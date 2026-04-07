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

            if (Input.GetMouseButtonDown(0) && _hoveredCell != null && !_hoveredCell.IsOccupied)
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

            float entryPrice = API.MarketDataStore.Instance?.GetLatestPrice(_pendingSymbol) ?? 0f;
            if (entryPrice <= 0f)
            {
                EventBus.ToastMessage("No market data yet ??? try again in a moment.");
                GameManager.Instance.SetState(GameState.DemoPlaying);
                return;
            }

            if (!GameManager.Instance.SpendBalance(defaultStake))
            {
                EventBus.ToastMessage("Insufficient funds!");
                GameManager.Instance.SetState(GameState.DemoPlaying);
                return;
            }

            var building = BuildingFactory.Create(_pendingSymbol, _hoveredCell.WorldPosition);
            if (CityGrid.Instance.PlaceBuilding(_hoveredX, _hoveredZ, building))
            {
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
                    GridX = _hoveredX,
                    GridY = _hoveredZ
                };

                var controller = building.GetComponent<BuildingController>();
                controller?.SetTrade(trade);

                EventBus.TradeOpened(trade);
                GameManager.Instance.SetState(GameState.DemoPlaying);

                Debug.Log($"[BuildingPlacer] Placed {trade.CommodityName} @ {entryPrice:F5} stake=${defaultStake}");
            }
            else
            {
                Object.Destroy(building);
                GameManager.Instance.AddBalance(defaultStake);
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
