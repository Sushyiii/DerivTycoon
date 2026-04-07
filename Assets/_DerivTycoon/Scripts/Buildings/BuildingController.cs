using DerivTycoon.API;
using DerivTycoon.City;
using DerivTycoon.Core;
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

        // Public accessors
        public Trade Trade => _trade;
        public string CommodityName => GameManager.Instance?.GetCommodityName(_symbol) ?? _symbol;
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
            Debug.Log($"[Production] Started cycle for {_symbol} @ {currentPrice:F5}");
        }

        private void StopProduction()
        {
            if (_trade == null) return;

            if (_cycleRunning)
            {
                // Refund the stake for the abandoned cycle
                GameManager.Instance?.AddBalance(_trade.ProductionStake);
                Debug.Log($"[Production] Cycle abandoned, refunded ${_trade.ProductionStake:F2}");
            }

            _trade.ProductionEnabled = false;
            _cycleRunning = false;

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

            // Auto-start next cycle: deduct stake and reset timer
            if (!GameManager.Instance.SpendBalance(_trade.ProductionStake))
            {
                // Can't afford next cycle — stop production
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

        private void OnTradeClosedEvent(Trade trade)
        {
            if (_trade == null || trade.Id != _trade.Id) return;

            EventBus.OnTickReceived -= OnTickReceived;
            EventBus.OnTradeClosed  -= OnTradeClosedEvent;

            // Stop production cleanly (no refund on close — mine is selling)
            _trade.ProductionEnabled = false;
            _cycleRunning = false;

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
