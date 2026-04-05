using System.Collections.Generic;
using DerivTycoon.API;
using DerivTycoon.Core;
using UnityEngine;

namespace DerivTycoon.Buildings
{
    public class BuildingController : MonoBehaviour
    {
        // ??0.05% price swing produces full red???green response (for testing)
        private const float PnLRangePercent = 0.05f;

        private string _symbol;
        private BuildingConfig _config;
        private Trade _trade;

        // All renderers in the prefab (for tinting)
        private Renderer[] _renderers;
        // Original colors per renderer material (so we can lerp back to neutral)
        private Color[] _baseColors;

        // Baseline Y scale of the root (so we grow/shrink relative to prefab design)
        private Vector3 _baseScale;

        public void Initialize(string symbol, BuildingConfig config)
        {
            _symbol = symbol;
            _config = config;
            _baseScale = transform.localScale;

            // Collect all child renderers once
            _renderers = GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _baseColors[i] = _renderers[i].material.color;
        }

        public void SetTrade(Trade trade)
        {
            _trade = trade;
        }

        private void OnEnable()  => EventBus.OnTickReceived += OnTickReceived;
        private void OnDisable() => EventBus.OnTickReceived -= OnTickReceived;

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

            // t=0 ??? full loss, t=0.5 ??? breakeven, t=1 ??? full profit
            float t = Mathf.InverseLerp(-PnLRangePercent, PnLRangePercent, pnlPercent);

            // Overall vertical scale: 0.85x (loss) ??? 1.0x (neutral) ??? 1.18x (profit)
            float heightScale = Mathf.Lerp(0.85f, 1.18f, t);
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * heightScale, _baseScale.z);

            // Tint: red overlay on loss, green overlay on profit, neutral at breakeven
            Color tintLoss   = new Color(1.0f, 0.30f, 0.20f);  // warm red
            Color tintNeutral= Color.white;
            Color tintProfit = new Color(0.50f, 1.00f, 0.55f);  // fresh green

            Color tint = t < 0.5f
                ? Color.Lerp(tintLoss,    tintNeutral, t * 2f)
                : Color.Lerp(tintNeutral, tintProfit,  (t - 0.5f) * 2f);

            ApplyTint(tint);
        }

        private void ApplyTint(Color tint)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                // Multiply the tint against the original design colour so themed
                // colours aren't completely washed out
                _renderers[i].material.color = _baseColors[i] * tint;
            }
        }

        public void OnTradeClosed(bool isWin)
        {
            EventBus.OnTickReceived -= OnTickReceived;
            if (_renderers == null) return;

            if (isWin)
            {
                // Gold shimmer ??? scale up slightly, warm gold tint
                transform.localScale = new Vector3(_baseScale.x, _baseScale.y * 1.25f, _baseScale.z);
                ApplyTint(new Color(1.0f, 0.88f, 0.30f));
            }
            else
            {
                // Deflated and grey
                transform.localScale = new Vector3(_baseScale.x * 0.90f, _baseScale.y * 0.75f, _baseScale.z * 0.90f);
                ApplyTint(new Color(0.55f, 0.55f, 0.55f));
            }
        }

        /// <summary>Reset visuals back to prefab original (e.g. after watching without an active trade)</summary>
        public void ResetVisuals()
        {
            transform.localScale = _baseScale;
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].material.color = _baseColors[i];
        }
    }
}
