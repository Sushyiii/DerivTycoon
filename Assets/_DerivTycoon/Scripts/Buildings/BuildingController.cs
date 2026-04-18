using DerivTycoon.API;
using DerivTycoon.API.Models;
using DerivTycoon.City;
using DerivTycoon.Core;
using DerivTycoon.Trading;
using DerivTycoon.UI;
using UnityEngine;

namespace DerivTycoon.Buildings
{
    public class BuildingController : MonoBehaviour
    {
        // ±20% P&L (after multiplier) = full red→green visual response
        private const float PnLRangePercent = 20f;
        private const float ProductionWinPayout = 1.80f; // vault receives this on a win

        private string _symbol;
        private BuildingConfig _config;
        private Trade _trade;

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private Vector3 _baseScale;

        // Production cycle state
        private float _cycleStartTime;
        private float _cycleStartPrice;
        private bool _cycleRunning;
        private int _cycleReqId;

        // Public accessors
        public Trade Trade => _trade;
        public string CommodityName => GameManager.Instance?.GetCommodityName(_symbol) ?? _symbol;
        public bool IsCycleRunning => _cycleRunning;
        public float CycleCountdownSeconds => (_cycleRunning && _trade != null)
            ? Mathf.Max(0f, _trade.ProductionCycleDuration - (Time.time - _cycleStartTime))
            : 0f;

        public void Initialize(string symbol, BuildingConfig config)
        {
            _symbol = symbol;
            _config = config;
            _baseScale = transform.localScale;

            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _baseColors[i] = _renderers[i].material.color;

            var col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.2f, 0);
            col.size   = new Vector3(2f, 2.4f, 2f);
        }

        public void SetTrade(Trade trade)
        {
            _trade = trade;
        }

        private void OnMouseDown()
        {
            BuildingInfoUI.Instance?.Show(this);
        }

        private void OnEnable()
        {
            EventBus.OnTickReceived += OnTickReceived;
            EventBus.OnTradeClosed  += OnTradeClosedEvent;
        }

        private void OnDisable()
        {
            EventBus.OnTickReceived -= OnTickReceived;
            EventBus.OnTradeClosed  -= OnTradeClosedEvent;
        }

        private void Update()
        {
            if (_trade == null || !_trade.ProductionEnabled || !_cycleRunning) return;
            // Live contract in progress — wait for API expiry event, don't use demo timer
            if (!string.IsNullOrEmpty(_trade.ActiveCycleContractId)) return;

            if (Time.time - _cycleStartTime >= _trade.ProductionCycleDuration)
                EvaluateCycle();
        }

        // ==================== Ownership Visuals ====================

        private void OnTickReceived(API.Models.TickData tick)
        {
            if (_trade == null || tick.symbol != _symbol) return;

            _trade.CurrentPrice = tick.quote;
            EventBus.TradeUpdated(_trade);
            UpdateVisuals(_trade.PnLPercent);
        }

        private void UpdateVisuals(float pnlPercent)
        {
            if (_renderers == null) return;

            float t = Mathf.InverseLerp(-PnLRangePercent, PnLRangePercent, pnlPercent);

            float heightScale = Mathf.Lerp(0.85f, 1.18f, t);
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * heightScale, _baseScale.z);

            Color tintLoss    = new Color(1.0f, 0.30f, 0.20f);
            Color tintNeutral = Color.white;
            Color tintProfit  = new Color(0.50f, 1.00f, 0.55f);

            Color tint = t < 0.5f
                ? Color.Lerp(tintLoss,    tintNeutral, t * 2f)
                : Color.Lerp(tintNeutral, tintProfit,  (t - 0.5f) * 2f);

            ApplyTint(tint);
        }

        private void ApplyTint(Color tint)
        {
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].material.color = _baseColors[i] * tint;
        }

        // ==================== Production Cycles ====================

        public void ToggleProduction()
        {
            if (_trade == null) return;

            if (_trade.ProductionEnabled)
                StopProduction();
            else
                StartProduction();
        }

        private void StartProduction()
        {
            if (_trade == null) return;
            if (_cycleRunning) return; // Already in a cycle, don't double-start

            float currentPrice = MarketDataStore.Instance?.GetLatestPrice(_symbol) ?? 0f;
            if (currentPrice <= 0f)
            {
                EventBus.ToastMessage("No market data — try again.");
                return;
            }

            if (!GameManager.Instance.SpendBalance(_trade.ProductionStake))
            {
                EventBus.ToastMessage("Insufficient funds for production!");
                return;
            }

            _trade.ProductionEnabled = true;
            _cycleStartPrice = currentPrice;
            _cycleStartTime  = Time.time;
            _cycleRunning    = true;
            EventBus.TradeUpdated(_trade);

            var trading = DerivTradingService.Instance;
            if (trading != null && trading.IsReady)
                StartCycleLive(trading);

            Debug.Log($"[Production] Started cycle for {_symbol} @ {currentPrice:F5}");
        }

        private void StartCycleLive(DerivTradingService trading)
        {
            int reqId = ++_cycleReqId;
            int durationSecs = (int)_trade.ProductionCycleDuration;

            void OnProposal(ProposalPayload proposal, int id)
            {
                if (id != reqId) return;
                trading.OnProposalReceived -= OnProposal;
                trading.OnTradingError -= OnCycleError;
                trading.BuyProposal(proposal.id, proposal.ask_price, reqId);
                trading.ForgetProposal(proposal.id);
            }

            void OnBuy(BuyPayload buy, int id)
            {
                if (id != reqId) return;
                trading.OnBuyConfirmed -= OnBuy;
                trading.OnTradingError -= OnCycleError;
                _trade.ActiveCycleContractId = buy.contract_id.ToString();
                trading.SubscribeContractUpdates(buy.contract_id);
                trading.OnContractUpdated += OnContractUpdate;
                Debug.Log($"[Production] CALL contract {buy.contract_id} bought for {_symbol}");
            }

            void OnCycleError(string message, int id)
            {
                if (id != reqId) return;
                trading.OnProposalReceived -= OnProposal;
                trading.OnBuyConfirmed -= OnBuy;
                trading.OnTradingError -= OnCycleError;
                // Refund stake and stop production on cycle error
                GameManager.Instance?.AddBalance(_trade.ProductionStake);
                _trade.ProductionEnabled = false;
                _cycleRunning = false;
                EventBus.ToastMessage($"Production failed: {message}");
                EventBus.TradeUpdated(_trade);
                Debug.LogWarning($"[Production] Cycle error for {_symbol}: {message}");
            }

            trading.OnProposalReceived += OnProposal;
            trading.OnBuyConfirmed += OnBuy;
            trading.OnTradingError += OnCycleError;
            trading.RequestCallProposal(
                _symbol,
                _trade.ProductionStake,
                durationSecs,
                _trade.ProductionBarrierOffset,
                reqId
            );
        }

        private void OnContractUpdate(ProposalOpenContractPayload payload)
        {
            if (_trade == null || payload == null) return;
            if (payload.contract_id.ToString() != _trade.ActiveCycleContractId) return;

            // Cache subscription ID on first update
            if (string.IsNullOrEmpty(_trade.ActiveCycleSubId))
            {
                string subId = DerivTradingService.Instance?.GetContractSubId(payload.contract_id);
                if (!string.IsNullOrEmpty(subId))
                    _trade.ActiveCycleSubId = subId;
            }

            bool expired = payload.is_expired == 1
                || payload.status == "won"
                || payload.status == "lost";

            if (!expired) return;

            DerivTradingService.Instance.OnContractUpdated -= OnContractUpdate;
            DerivTradingService.Instance.ForgetContractUpdates(_trade.ActiveCycleSubId);
            EvaluateCycleLive(payload);
        }

        private void EvaluateCycleLive(ProposalOpenContractPayload payload)
        {
            _trade.TotalCyclesRun++;
            bool win = payload.profit > 0;

            if (win)
            {
                _trade.VaultBalance += payload.payout;
                _trade.WinStreak++;
                string streakMsg = _trade.WinStreak >= 4 ? $" Mine on fire! {_trade.WinStreak} in a row!" : "";
                EventBus.ToastMessage($"{CommodityName}: Production won! +${payload.payout - _trade.ProductionStake:F2} to vault{streakMsg}");
            }
            else
            {
                _trade.WinStreak = 0;
                EventBus.ToastMessage($"{CommodityName}: Cycle lost. -${_trade.ProductionStake:F2}");
            }

            _trade.ActiveCycleContractId = null;
            _trade.ActiveCycleSubId = null;
            _cycleRunning = false;
            EventBus.TradeUpdated(_trade);

            Debug.Log($"[Production] LIVE {_symbol} cycle {_trade.TotalCyclesRun}: {(win ? "WIN" : "LOSS")} | vault=${_trade.VaultBalance:F2}");

            // Auto-start next cycle
            if (_trade.ProductionEnabled)
            {
                if (!GameManager.Instance.SpendBalance(_trade.ProductionStake))
                {
                    _trade.ProductionEnabled = false;
                    EventBus.ToastMessage("Not enough balance — production stopped.");
                    EventBus.TradeUpdated(_trade);
                    return;
                }
                _cycleStartPrice = MarketDataStore.Instance?.GetLatestPrice(_symbol) ?? _cycleStartPrice;
                _cycleStartTime  = Time.time;
                _cycleRunning    = true;
                StartCycleLive(DerivTradingService.Instance);
            }
        }

        private void StopProduction()
        {
            if (_trade == null) return;

            _trade.ProductionEnabled = false;

            if (_cycleRunning)
            {
                // Current cycle is in progress — let it complete, then stop
                // No refund: the cycle plays out and settles normally
                Debug.Log($"[Production] Stopping after current cycle completes for {_symbol}");
                EventBus.ToastMessage($"{CommodityName}: Production will stop after the current cycle completes.");
            }

            EventBus.TradeUpdated(_trade);
        }

        private void EvaluateCycle()
        {
            float currentPrice = MarketDataStore.Instance?.GetLatestPrice(_symbol) ?? 0f;
            if (currentPrice <= 0f)
            {
                // No data — restart cycle without result
                _cycleStartTime = Time.time;
                return;
            }

            _trade.TotalCyclesRun++;

            // Win condition: for ATM (metals) currentPrice > startPrice
            //                for non-ATM (Vol100) currentPrice > startPrice + barrierOffset
            float barrier = _cycleStartPrice + _trade.ProductionBarrierOffset;
            bool win = currentPrice > barrier;

            if (win)
            {
                _trade.VaultBalance += ProductionWinPayout;
                _trade.WinStreak++;

                string streakMsg = _trade.WinStreak >= 4
                    ? $" Mine on fire! {_trade.WinStreak} in a row!"
                    : "";
                EventBus.ToastMessage($"{CommodityName}: Production won! +${ProductionWinPayout - _trade.ProductionStake:F2} to vault{streakMsg}");
            }
            else
            {
                _trade.WinStreak = 0;
                EventBus.ToastMessage($"{CommodityName}: Cycle lost. -${_trade.ProductionStake:F2}");
                // Stake was already spent at cycle start; nothing extra to deduct
            }

            EventBus.TradeUpdated(_trade);
            Debug.Log($"[Production] {_symbol} cycle {_trade.TotalCyclesRun}: {(win ? "WIN" : "LOSS")} | vault=${_trade.VaultBalance:F2} | streak={_trade.WinStreak}");

            // Stop here if production was disabled during this cycle
            if (!_trade.ProductionEnabled)
            {
                _cycleRunning = false;
                EventBus.TradeUpdated(_trade);
                return;
            }

            // Auto-start next cycle: deduct stake and reset timer
            if (!GameManager.Instance.SpendBalance(_trade.ProductionStake))
            {
                _trade.ProductionEnabled = false;
                _cycleRunning = false;
                EventBus.ToastMessage("Not enough balance — production stopped.");
                EventBus.TradeUpdated(_trade);
                return;
            }

            _cycleStartPrice = currentPrice;
            _cycleStartTime  = Time.time;
        }

        // ==================== Trade Close ====================

        // Called by BuildingInfoUI instead of TradeManager.CloseTrade directly.
        // Ensures any active cycle contract is closed first before selling the mine.
        public void TrySellMine()
        {
            if (_trade == null) return;

            var trading = DerivTradingService.Instance;
            bool hasCycle = !string.IsNullOrEmpty(_trade.ActiveCycleContractId) &&
                            long.TryParse(_trade.ActiveCycleContractId, out long cycleId);

            if (hasCycle && trading != null && trading.IsReady)
            {
                long.TryParse(_trade.ActiveCycleContractId, out cycleId);

                void OnCycleSellOk(SellPayload sold)
                {
                    trading.OnSellConfirmed -= OnCycleSellOk;
                    trading.OnTradingError  -= OnCycleSellFail;
                    GameManager.Instance?.SyncBalance(sold.balance_after);
                    Debug.Log($"[Production] Cycle contract {cycleId} closed — proceeding to sell mine");
                    TradeManager.Instance?.CloseTrade(_trade.Id);
                }

                void OnCycleSellFail(string msg, int id)
                {
                    trading.OnSellConfirmed -= OnCycleSellOk;
                    trading.OnTradingError  -= OnCycleSellFail;
                    Debug.LogWarning($"[Production] Cannot close cycle contract yet: {msg}");
                    EventBus.ToastMessage("Please wait for the current production cycle to complete before selling.");
                }

                trading.OnSellConfirmed += OnCycleSellOk;
                trading.OnTradingError  += OnCycleSellFail;
                trading.SellContract(cycleId);
            }
            else
            {
                // No active cycle or demo mode — sell directly
                TradeManager.Instance?.CloseTrade(_trade.Id);
            }
        }

        private void OnTradeClosedEvent(Trade trade)
        {
            if (_trade == null || trade.Id != _trade.Id) return;

            EventBus.OnTickReceived -= OnTickReceived;
            EventBus.OnTradeClosed  -= OnTradeClosedEvent;

            // Stop production and clean up subscriptions
            _trade.ProductionEnabled = false;
            _cycleRunning = false;

            var trading = DerivTradingService.Instance;
            if (trading != null)
            {
                trading.OnContractUpdated -= OnContractUpdate;
                trading.ForgetContractUpdates(_trade.ActiveCycleSubId);
            }
            else if (_cycleRunning)
            {
                // Demo mode: refund stake for interrupted cycle
                GameManager.Instance?.AddBalance(_trade.ProductionStake);
            }

            bool isWin = trade.PnL >= 0f;

            if (_renderers != null)
            {
                if (isWin)
                {
                    transform.localScale = new Vector3(_baseScale.x, _baseScale.y * 1.25f, _baseScale.z);
                    ApplyTint(new Color(1.0f, 0.88f, 0.30f));
                }
                else
                {
                    transform.localScale = new Vector3(_baseScale.x * 0.90f, _baseScale.y * 0.75f, _baseScale.z * 0.90f);
                    ApplyTint(new Color(0.55f, 0.55f, 0.55f));
                }
            }

            CityGrid.Instance?.RemoveBuilding(_trade.GridX, _trade.GridY);
            Destroy(gameObject, 1.5f);
        }

        public void ResetVisuals()
        {
            transform.localScale = _baseScale;
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].material.color = _baseColors[i];
        }
    }
}
