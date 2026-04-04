using System.Collections.Generic;
using DerivTycoon.API.Models;
using DerivTycoon.Core;
using UnityEngine;

namespace DerivTycoon.API
{
    public class MarketDataStore : MonoBehaviour
    {
        public static MarketDataStore Instance { get; private set; }

        private const int MaxTicksPerSymbol = 100;

        private readonly Dictionary<string, List<TickData>> _tickHistory = new();
        private readonly Dictionary<string, TickData> _latestTicks = new();
        private readonly Dictionary<string, ActiveSymbol> _activeSymbols = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnTickReceived += StoreTick;
        }

        private void OnDisable()
        {
            EventBus.OnTickReceived -= StoreTick;
        }

        private void StoreTick(TickData tick)
        {
            if (tick == null || string.IsNullOrEmpty(tick.symbol)) return;

            _latestTicks[tick.symbol] = tick;

            if (!_tickHistory.ContainsKey(tick.symbol))
                _tickHistory[tick.symbol] = new List<TickData>();

            var history = _tickHistory[tick.symbol];
            history.Add(tick);

            if (history.Count > MaxTicksPerSymbol)
                history.RemoveAt(0);
        }

        public TickData GetLatestTick(string symbol)
        {
            return _latestTicks.TryGetValue(symbol, out var tick) ? tick : null;
        }

        public List<TickData> GetTickHistory(string symbol)
        {
            return _tickHistory.TryGetValue(symbol, out var history) ? history : new List<TickData>();
        }

        public float GetLatestPrice(string symbol)
        {
            var tick = GetLatestTick(symbol);
            return tick?.quote ?? 0f;
        }

        public float GetPriceChange(string symbol)
        {
            if (!_tickHistory.TryGetValue(symbol, out var history) || history.Count < 2)
                return 0f;

            float current = history[^1].quote;
            float previous = history[^2].quote;
            return previous > 0 ? (current - previous) / previous * 100f : 0f;
        }

        public void RegisterActiveSymbol(ActiveSymbol symbol)
        {
            if (symbol != null)
                _activeSymbols[symbol.symbol] = symbol;
        }

        public ActiveSymbol GetSymbolInfo(string symbol)
        {
            return _activeSymbols.TryGetValue(symbol, out var info) ? info : null;
        }

        public bool IsMarketOpen(string symbol)
        {
            var info = GetSymbolInfo(symbol);
            return info != null && info.exchange_is_open == 1 && info.is_trading_suspended == 0;
        }
    }
}
