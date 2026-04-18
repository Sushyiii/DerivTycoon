using DerivTycoon.API;
using DerivTycoon.API.Models;
using DerivTycoon.Trading;
using UnityEngine;

namespace DerivTycoon.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        public float startingBalance = 10000f;

        [Header("Commodity Symbols")]
        public string[] commoditySymbols = new string[]
        {
            "frxXAUUSD",   // Gold
            "frxXAGUSD",   // Silver
            "frxXPTUSD",   // Platinum
            "frxXPDUSD",   // Palladium
            "1HZ100V"      // Volatility 100 Index (24/7 synthetic - always live)
        };

        public GameState CurrentState { get; private set; } = GameState.Boot;
        public float Balance { get; private set; }
        public bool IsDemoMode { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Balance = startingBalance;
            Initialize();
        }

        private void Initialize()
        {
            SetState(GameState.Boot);

            // Ensure required services exist
            EnsureService<MarketDataStore>("MarketDataStore");
            EnsureService<DerivAPIService>("DerivAPIService");
            EnsureService<TradeManager>("TradeManager");

            // Wire up trading auth if service is present on this GO
            var authService = GetComponent<API.DerivAuthService>();
            if (authService != null)
                authService.OnTradingWsReady += OnTradingWsReady;

            // Connect to public WebSocket for tick data
            EventBus.OnWebSocketConnected += OnConnected;
            DerivAPIService.Instance.ConnectPublic();
        }

        private void OnTradingWsReady(string wsUrl)
        {
            EnsureService<API.DerivTradingService>("DerivTradingService");
            var trading = API.DerivTradingService.Instance;

            // Unsubscribe first to avoid duplicate handlers on reconnect
            trading.OnBalanceUpdated -= SyncBalance;
            trading.OnTradingDisconnected -= OnTradingDisconnected;

            trading.OnBalanceUpdated += SyncBalance;
            trading.OnTradingDisconnected += OnTradingDisconnected;
            trading.Connect(wsUrl);
            IsDemoMode = false;
            Debug.Log("[GameManager] Live trading mode active");
            if (CurrentState == GameState.MainMenu || CurrentState == GameState.DemoPlaying)
                SetState(GameState.LivePlaying);
        }

        private void OnTradingDisconnected()
        {
            Debug.LogWarning("[GameManager] Trading WS dropped — reconnecting in 3s...");
            Invoke(nameof(ReconnectTrading), 3f);
        }

        private void ReconnectTrading()
        {
            API.DerivAuthService.Instance?.Reconnect();
        }

        private void OnConnected()
        {
            Debug.Log("[GameManager] Connected to Deriv API, subscribing to commodities...");

            // Request active symbols first to verify our symbol names
            DerivAPIService.Instance.RequestActiveSymbols();

            // Subscribe to all commodity tick streams
            foreach (var symbol in commoditySymbols)
            {
                DerivAPIService.Instance.SubscribeToTicks(symbol);
            }

            // Move to main menu once connected
            SetState(GameState.MainMenu);
        }

        public void StartDemoMode()
        {
            IsDemoMode = true;
            Balance = startingBalance;
            EventBus.BalanceChanged(Balance);
            SetState(GameState.DemoPlaying);
            Debug.Log("[GameManager] Demo mode started");
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            Debug.Log($"[GameManager] State: {CurrentState} -> {newState}");
            CurrentState = newState;
            EventBus.GameStateChanged(newState);
        }

        public bool SpendBalance(float amount)
        {
            if (amount > Balance) return false;

            Balance -= amount;
            EventBus.BalanceChanged(Balance);
            return true;
        }

        public void AddBalance(float amount)
        {
            Balance += amount;
            EventBus.BalanceChanged(Balance);
        }

        public void SyncBalance(float amount)
        {
            Debug.Log($"[GameManager] SyncBalance: {amount}");
            Balance = amount;
            EventBus.BalanceChanged(Balance);
        }

        public string GetCommodityName(string symbol)
        {
            return symbol switch
            {
                "frxXAUUSD" => "Gold",
                "frxXAGUSD" => "Silver",
                "frxXPTUSD" => "Platinum",
                "frxXPDUSD" => "Palladium",
                "1HZ100V"   => "Volatility Index",
                _ => symbol
            };
        }

        private void EnsureService<T>(string name) where T : MonoBehaviour
        {
            if (FindAnyObjectByType<T>() == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(transform);
                go.AddComponent<T>();
            }
        }

        private void OnDestroy()
        {
            EventBus.OnWebSocketConnected -= OnConnected;
            var trading = API.DerivTradingService.Instance;
            if (trading != null)
                trading.OnBalanceUpdated -= SyncBalance;
        }
    }
}
