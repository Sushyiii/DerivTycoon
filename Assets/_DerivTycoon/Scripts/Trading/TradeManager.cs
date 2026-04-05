using System.Collections.Generic;
using DerivTycoon.Core;
using UnityEngine;

namespace DerivTycoon.Trading
{
    public class TradeManager : MonoBehaviour
    {
        public static TradeManager Instance { get; private set; }

        private readonly Dictionary<string, Trade> _activeTrades = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnTradeOpened += OnTradeOpened;
            EventBus.OnTradeClosed += OnTradeClosed;
        }

        private void OnDisable()
        {
            EventBus.OnTradeOpened -= OnTradeOpened;
            EventBus.OnTradeClosed -= OnTradeClosed;
        }

        private void OnTradeOpened(Trade trade)
        {
            _activeTrades[trade.Id] = trade;
            Debug.Log($"[TradeManager] Opened: {trade.CommodityName} entry={trade.EntryPrice:F5} stake=${trade.Stake}");
        }

        private void OnTradeClosed(Trade trade)
        {
            if (!_activeTrades.ContainsKey(trade.Id)) return;
            _activeTrades.Remove(trade.Id);

            float pnl = trade.PnL;
            // Return stake + profit (or stake - loss)
            GameManager.Instance?.AddBalance(trade.Stake + pnl);

            string sign = pnl >= 0 ? "+" : "";
            Debug.Log($"[TradeManager] Closed: {trade.CommodityName} P&L={sign}{pnl:F2}");
            EventBus.ToastMessage($"{trade.CommodityName}: {sign}{pnl:F2} USD");
        }

        public void CloseTrade(string tradeId)
        {
            if (_activeTrades.TryGetValue(tradeId, out Trade trade))
            {
                trade.IsActive = false;
                EventBus.TradeClosed(trade);
            }
        }

        public int ActiveTradeCount => _activeTrades.Count;

        public IEnumerable<Trade> GetActiveTrades() => _activeTrades.Values;
    }
}
