using DerivTycoon.API;
using DerivTycoon.API.Models;
using DerivTycoon.Buildings;
using DerivTycoon.Core;
using DerivTycoon.Trading;
using UnityEngine;

namespace DerivTycoon.City
{
    public class BuildingPlacer : MonoBehaviour
    {
        public static BuildingPlacer Instance { get; private set; }

        [Header("Trade Settings")]
        public float defaultStake = 100f;
        public int defaultMultiplier = 100;

        private GridCell _hoveredCell;
        private int _hoveredX = -1;
        private int _hoveredZ = -1;
        private string _pendingSymbol;
        private bool _isPlacementMode;
        private bool _waitingForBuy;
        private int _nextReqId;
        private GridCell _pendingCell;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnGameStateChanged += OnGameStateChanged;
            EventBus.OnCommoditySelected += OnCommoditySelected;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= OnGameStateChanged;
            EventBus.OnCommoditySelected -= OnCommoditySelected;
        }

        private void OnGameStateChanged(GameState state)
        {
            _isPlacementMode = state == GameState.Placement;
            if (!_isPlacementMode) ClearHighlight();
        }

        private void OnCommoditySelected(string symbol)
        {
            _pendingSymbol = symbol;
        }

        private void Update()
        {
            if (!_isPlacementMode || CityGrid.Instance == null) return;

            RaycastToGrid();

            if (Input.GetMouseButtonDown(0) && _hoveredCell != null && !_hoveredCell.IsOccupied && !_waitingForBuy)
                PlaceBuilding();
        }

        private void RaycastToGrid()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var cell = CityGrid.Instance.GetCellAtWorldPos(hit.point);
                if (cell != null)
                {
                    if (cell.X != _hoveredX || cell.Y != _hoveredZ)
                    {
                        ClearHighlight();
                        _hoveredX = cell.X;
                        _hoveredZ = cell.Y;
                        _hoveredCell = cell;
                        CityGrid.Instance.HighlightCell(_hoveredX, _hoveredZ, true);
                    }
                    return;
                }
            }

            ClearHighlight();
        }

        private void ClearHighlight()
        {
            if (_hoveredX >= 0 && _hoveredZ >= 0)
                CityGrid.Instance?.HighlightCell(_hoveredX, _hoveredZ, false);

            _hoveredCell = null;
            _hoveredX = -1;
            _hoveredZ = -1;
        }

        private void PlaceBuilding()
        {
            if (string.IsNullOrEmpty(_pendingSymbol)) return;

            float entryPrice = MarketDataStore.Instance?.GetLatestPrice(_pendingSymbol) ?? 0f;
            if (entryPrice <= 0f)
            {
                EventBus.ToastMessage("No market data yet — try again in a moment.");
                GameManager.Instance.SetState(GameState.DemoPlaying);
                return;
            }

            if (!GameManager.Instance.SpendBalance(defaultStake))
            {
                EventBus.ToastMessage("Insufficient funds!");
                GameManager.Instance.SetState(GameState.DemoPlaying);
                return;
            }

            var trading = DerivTradingService.Instance;
            if (trading != null && trading.IsReady)
                PlaceBuildingLive(entryPrice);
            else
                PlaceBuildingDemo(entryPrice, null);
        }

        private void PlaceBuildingLive(float entryPrice)
        {
            _pendingCell = _hoveredCell;
            _waitingForBuy = true;
            int reqId = ++_nextReqId;

            var trading = DerivTradingService.Instance;

            void OnProposal(ProposalPayload proposal, int id)
            {
                if (id != reqId) return;
                trading.OnProposalReceived -= OnProposal;
                trading.OnTradingError -= OnError;
                trading.BuyProposal(proposal.id, proposal.ask_price, reqId);
                trading.ForgetProposal(proposal.id);
            }

            void OnBuy(BuyPayload buy, int id)
            {
                if (id != reqId) return;
                trading.OnBuyConfirmed -= OnBuy;
                trading.OnTradingError -= OnError;
                GameManager.Instance.SyncBalance(buy.balance_after);
                PlaceBuildingDemo(entryPrice, buy.contract_id.ToString());
                _waitingForBuy = false;
            }

            void OnError(string message, int id)
            {
                if (id != reqId) return;
                trading.OnProposalReceived -= OnProposal;
                trading.OnBuyConfirmed -= OnBuy;
                trading.OnTradingError -= OnError;
                GameManager.Instance.AddBalance(defaultStake); // refund
                _waitingForBuy = false;
                _pendingCell = null;
                EventBus.ToastMessage($"Trade failed: {message}");
                GameManager.Instance.SetState(GameState.LivePlaying);
                Debug.LogWarning($"[BuildingPlacer] Trade error: {message}");
            }

            trading.OnProposalReceived += OnProposal;
            trading.OnBuyConfirmed += OnBuy;
            trading.OnTradingError += OnError;
            trading.RequestMultiplierProposal(_pendingSymbol, defaultStake, defaultMultiplier, reqId);
        }

        private void PlaceBuildingDemo(float entryPrice, string derivContractId)
        {
            var cell = _pendingCell ?? _hoveredCell;
            int cellX = _pendingCell != null ? _pendingCell.X : _hoveredX;
            int cellZ = _pendingCell != null ? _pendingCell.Y : _hoveredZ;
            _pendingCell = null;

            var building = BuildingFactory.Create(_pendingSymbol, cell.WorldPosition);
            if (CityGrid.Instance.PlaceBuilding(cellX, cellZ, building))
            {
                var cfg = BuildingFactory.GetConfig(_pendingSymbol);
                var trade = new Trade
                {
                    Id = System.Guid.NewGuid().ToString(),
                    Symbol = _pendingSymbol,
                    CommodityName = GameManager.Instance.GetCommodityName(_pendingSymbol),
                    ContractType = "CALL",
                    Stake = defaultStake,
                    Multiplier = defaultMultiplier,
                    EntryPrice = entryPrice,
                    CurrentPrice = entryPrice,
                    StartTime = Time.time,
                    IsActive = true,
                    GridX = cellX,
                    GridY = cellZ,
                    DerivContractId = derivContractId,
                    ProductionCycleDuration = cfg.CycleDuration,
                    ProductionBarrierOffset = cfg.BarrierOffset,
                    ProductionStake = 1f,
                    ProductionEnabled = false,
                    VaultBalance = 0f,
                    WinStreak = 0,
                    TotalCyclesRun = 0
                };

                var controller = building.GetComponent<BuildingController>();
                controller?.SetTrade(trade);

                EventBus.TradeOpened(trade);
                GameManager.Instance.SetState(GameState.DemoPlaying);

                string mode = derivContractId != null ? $"LIVE contract={derivContractId}" : "DEMO";
                Debug.Log($"[BuildingPlacer] Placed {trade.CommodityName} @ {entryPrice:F5} stake=${defaultStake} [{mode}]");
            }
            else
            {
                Object.Destroy(building);
                GameManager.Instance.AddBalance(defaultStake);
                _waitingForBuy = false;
            }
        }

        public void BeginPlacement(string symbol)
        {
            _pendingSymbol = symbol;
            EventBus.CommoditySelected(symbol);
            GameManager.Instance.SetState(GameState.Placement);
        }
    }
}
