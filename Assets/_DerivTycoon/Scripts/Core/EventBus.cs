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
        public int Multiplier;      // e.g. 100 → 100x leverage
        public float EntryPrice;
        public float CurrentPrice;
        public float Duration;
        public float StartTime;
        public bool IsActive;
        public int GridX;
        public int GridY;

        // Real API contract IDs (null = demo mode)
        public string DerivContractId;           // ownership MULTUP contract ID
        public string ActiveCycleContractId;     // current production CALL contract ID
        public string ActiveCycleSubId;          // proposal_open_contract subscription ID

        // Production cycle fields
        public float ProductionCycleDuration;  // 300f metals, 60f Vol100
        public float ProductionBarrierOffset;  // 0f metals, -1.2f Vol100
        public float ProductionStake = 1f;
        public bool ProductionEnabled;
        public float VaultBalance;             // accumulated wins, never drains
        public int WinStreak;
        public int TotalCyclesRun;

        // P&L = price_change_% × multiplier × stake
        public float PnL => EntryPrice > 0
            ? (ContractType == "CALL"
                ? (CurrentPrice - EntryPrice) / EntryPrice * Multiplier * Stake
                : (EntryPrice - CurrentPrice) / EntryPrice * Multiplier * Stake)
            : 0f;

        // PnLPercent is the % return on the stake (after multiplier)
        public float PnLPercent => EntryPrice > 0
            ? (ContractType == "CALL"
                ? (CurrentPrice - EntryPrice) / EntryPrice * Multiplier * 100f
                : (EntryPrice - CurrentPrice) / EntryPrice * Multiplier * 100f)
            : 0f;
    }
}
