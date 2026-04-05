using DerivTycoon.API;
using DerivTycoon.Core;
using UnityEngine;

namespace DerivTycoon.Buildings
{
    public class BuildingController : MonoBehaviour
    {
        // Tighten for testing: ??0.05% swing = full red/green response
        private const float PnLRangePercent = 0.05f;

        private string _symbol;
        private BuildingConfig _config;
        private Trade _trade;
        private Transform _body;
        private Renderer _bodyRenderer;
        private float _baseHeight;

        public void Initialize(string symbol, BuildingConfig config)
        {
            _symbol = symbol;
            _config = config;
            _baseHeight = config.BaseHeight;

            _body = transform.Find("Body");
            if (_body != null)
                _bodyRenderer = _body.GetComponent<Renderer>();
        }

        public void SetTrade(Trade trade)
        {
            _trade = trade;
        }

        private void OnEnable()
        {
            EventBus.OnTickReceived += OnTickReceived;
        }

        private void OnDisable()
        {
            EventBus.OnTickReceived -= OnTickReceived;
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
            if (_body == null || _bodyRenderer == null) return;

            // t=0 ??? full loss, t=0.5 ??? breakeven, t=1 ??? full profit
            float t = Mathf.InverseLerp(-PnLRangePercent, PnLRangePercent, pnlPercent);

            // Height: 0.6x (loss) ??? 1.5x (profit) of base
            float heightScale = Mathf.Lerp(0.6f, 1.5f, t);
            float newHeight = _baseHeight * heightScale;
            _body.localScale = new Vector3(1.4f, newHeight, 1.4f);
            _body.localPosition = new Vector3(0, newHeight / 2f, 0);

            // Color: pure red ??? bright green (very obvious)
            Color lossColor  = new Color(0.9f, 0.1f, 0.1f);   // red
            Color evenColor  = new Color(1.0f, 0.8f, 0.1f);   // yellow
            Color profitColor = new Color(0.1f, 0.95f, 0.2f); // green

            Color col = t < 0.5f
                ? Color.Lerp(lossColor, evenColor, t * 2f)
                : Color.Lerp(evenColor, profitColor, (t - 0.5f) * 2f);

            _bodyRenderer.material.color = col;
        }

        public void OnTradeClosed(bool isWin)
        {
            EventBus.OnTickReceived -= OnTickReceived;

            if (_body == null || _bodyRenderer == null) return;

            if (isWin)
            {
                float h = _baseHeight * 1.6f;
                _body.localScale = new Vector3(1.4f, h, 1.4f);
                _body.localPosition = new Vector3(0, h / 2f, 0);
                _bodyRenderer.material.color = new Color(1f, 0.85f, 0.1f); // gold
            }
            else
            {
                float h = _baseHeight * 0.5f;
                _body.localScale = new Vector3(1.2f, h, 1.2f);
                _body.localPosition = new Vector3(0, h / 2f, 0);
                _bodyRenderer.material.color = Color.grey;
            }
        }
    }
}
