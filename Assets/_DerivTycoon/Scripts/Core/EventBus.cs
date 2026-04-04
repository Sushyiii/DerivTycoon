using System;
using DerivTycoon.API.Models;

namespace DerivTycoon.Core
{
    public static class EventBus
    {
        // Game state
        public static event Action<GameState> OnGameStateChanged;
        public static void GameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        // Market data
        public static event Action<TickData> OnTickReceived;
        public static void TickReceived(TickData tick) => OnTickReceived?.Invoke(tick);

        // Trading
        public static event Action<Trade> OnTradeOpened;
        public static void TradeOpened(Trade trade) => OnTradeOpened?.Invoke(trade);

        public static event Action<Trade> OnTradeUpdated;
        public static void TradeUpdated(Trade trade) => OnTradeUpdated?.Invoke(trade);

        public static event Action<Trade> OnTradeClosed;
        public static void TradeClosed(Trade trade) => OnTradeClosed?.Invoke(trade);

        // Balance
        public static event Action<float> OnBalanceChanged;
        public static void BalanceChanged(float balance) => OnBalanceChanged?.Invoke(balance);

        // WebSocket
        public static event Action OnWebSocketConnected;
        public static void WebSocketConnected() => OnWebSocketConnected?.Invoke();

        public static event Action<string> OnWebSocketError;
        public static void WebSocketError(string error) => OnWebSocketError?.Invoke(error);

        // Building
        public static event Action<string> OnCommoditySelected;
        public static void CommoditySelected(string symbol) => OnCommoditySelected?.Invoke(symbol);

        // UI
        public static event Action<string> OnToastMessage;
        public static void ToastMessage(string message) => OnToastMessage?.Invoke(message);
    }

    // Forward declaration for Trade used in events (full impl in Trading/)
    public class Trade
    {
        public string Id;
        public string Symbol;
        public string CommodityName;
        public string ContractType; // CALL or PUT (Rise or Fall)
        public float Stake;
        public float EntryPrice;
        public float CurrentPrice;
        public float Duration;
        public float StartTime;
        public bool IsActive;
        public int GridX;
        public int GridY;

        public float PnL => ContractType == "CALL"
            ? (CurrentPrice - EntryPrice) / EntryPrice * Stake
            : (EntryPrice - CurrentPrice) / EntryPrice * Stake;

        public float PnLPercent => EntryPrice > 0
            ? (ContractType == "CALL"
                ? (CurrentPrice - EntryPrice) / EntryPrice * 100f
                : (EntryPrice - CurrentPrice) / EntryPrice * 100f)
            : 0f;
    }
}
