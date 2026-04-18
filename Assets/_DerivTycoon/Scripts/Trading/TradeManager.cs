using System.Collections.Generic;
using DerivTycoon.API;
using DerivTycoon.API.Models;
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
            // Only add balance locally in demo mode — live mode syncs from real API sell response
            if (GameManager.Instance?.IsDemoMode == true)
                GameManager.Instance?.AddBalance(trade.Stake + pnl);

            string sign = pnl >= 0 ? "+" : "";
            Debug.Log($"[TradeManager] Closed: {trade.CommodityName} P&L={sign}{pnl:F2}");
            EventBus.ToastMessage($"{trade.CommodityName}: {sign}{pnl:F2} USD");
        }

        public void CloseTrade(string tradeId)
        {
            if (!_activeTrades.TryGetValue(tradeId, out Trade trade)) return;

            var trading = DerivTradingService.Instance;
            if (trading != null && trading.IsReady && !string.IsNullOrEmpty(trade.DerivContractId))
            {
                // Live mode: sell via real API, settlement happens in OnSellConfirmed
                trading.OnSellConfirmed += OnSellConfirmed;
                trading.SellContract(long.Parse(trade.DerivContractId));
            }
            else
            {
                // Demo mode: immediate local settlement
                trade.IsActive = false;
                EventBus.TradeClosed(trade);
            }
        }

        private void OnSellConfirmed(SellPayload sold)
        {
            if (DerivTradingService.Instance != null)
                DerivTradingService.Instance.OnSellConfirmed -= OnSellConfirmed;

            // Find the trade by contract ID
            Trade closedTrade = null;
            foreach (var t in _activeTrades.Values)
            {
                if (t.DerivContractId == sold.contract_id.ToString())
                {
                    closedTrade = t;
                    break;
                }
            }

            if (closedTrade == null)
            {
                Debug.LogWarning($"[TradeManager] Sell confirmed but no matching trade for contract {sold.contract_id}");
                return;
            }

            GameManager.Instance?.SyncBalance(sold.balance_after);
            closedTrade.IsActive = false;
            EventBus.TradeClosed(closedTrade);

            float sign = sold.sold_for >= 0 ? sold.sold_for : 0;
            Debug.Log($"[TradeManager] Sold contract {sold.contract_id} for ${sold.sold_for:F2}, balance: ${sold.balance_after:F2}");
        }

        public int ActiveTradeCount => _activeTrades.Count;

        public IEnumerable<Trade> GetActiveTrades() => _activeTrades.Values;
    }
}
