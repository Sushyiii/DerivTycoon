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
        // At 100x multiplier this corresponds to a ±0.2% price move
        private const float PnLRangePercent = 20f;

        private string _symbol;
        private BuildingConfig _config;
        private Trade _trade;

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private Vector3 _baseScale;

        // Public accessors for BuildingInfoUI
        public Trade Trade => _trade;
        public string CommodityName => GameManager.Instance?.GetCommodityName(_symbol) ?? _symbol;

        public void Initialize(string symbol, BuildingConfig config)
        {
            _symbol = symbol;
            _config = config;
            _baseScale = transform.localScale;

            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _baseColors[i] = _renderers[i].material.color;

            // Add a box collider on the root for click detection
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
            EventBus.OnTickReceived  += OnTickReceived;
            EventBus.OnTradeClosed   += OnTradeClosedEvent;
        }

        private void OnDisable()
        {
            EventBus.OnTickReceived  -= OnTickReceived;
            EventBus.OnTradeClosed   -= OnTradeClosedEvent;
        }

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

        private void OnTradeClosedEvent(Trade trade)
        {
            if (_trade == null || trade.Id != _trade.Id) return;

            // Stop reacting to ticks
            EventBus.OnTickReceived -= OnTickReceived;
            EventBus.OnTradeClosed  -= OnTradeClosedEvent;

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

            // Free the grid cell and destroy the building after a short visual pause
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
