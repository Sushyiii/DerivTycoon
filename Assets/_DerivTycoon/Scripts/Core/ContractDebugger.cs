using System.Collections;
using System.Collections.Generic;
using DerivTycoon.API;
using DerivTycoon.API.Models;
using UnityEngine;

namespace DerivTycoon.Core
{
    /// <summary>
    /// TEMPORARY debug utility. Queries contracts_for on all game symbols
    /// and logs available contract types with durations.
    /// Attach to any GameObject in the scene, then enter Play mode.
    /// </summary>
    public class ContractDebugger : MonoBehaviour
    {
        private readonly Queue<string> _pendingSymbols = new();
        private string _currentSymbol;
        private readonly Dictionary<string, List<AvailableContract>> _results = new();

        private void OnEnable()
        {
            EventBus.OnWebSocketConnected += OnConnected;
        }

        private void OnDisable()
        {
            EventBus.OnWebSocketConnected -= OnConnected;
        }

        private void OnConnected()
        {
            StartCoroutine(QueryAllSymbols());
        }

        private IEnumerator QueryAllSymbols()
        {
            // Wait a moment for connection to stabilize
            yield return new WaitForSeconds(2f);

            var api = DerivAPIService.Instance;
            if (api == null) yield break;

            api.OnContractsForReceived += OnContractsReceived;

            string[] symbols = { "frxXAUUSD", "frxXAGUSD", "frxXPTUSD", "frxXPDUSD", "1HZ100V" };

            foreach (var symbol in symbols)
            {
                _currentSymbol = symbol;
                _pendingSymbols.Enqueue(symbol);
                api.RequestContractsFor(symbol);
                // Wait between requests to avoid rate limits and to correlate responses
                yield return new WaitForSeconds(1.5f);
            }

            // Wait for last response
            yield return new WaitForSeconds(2f);

            api.OnContractsForReceived -= OnContractsReceived;

            LogSummary();
        }

        private void OnContractsReceived(string _, AvailableContract[] contracts)
        {
            string symbol = _pendingSymbols.Count > 0 ? _pendingSymbols.Dequeue() : "unknown";

            _results[symbol] = new List<AvailableContract>(contracts);

            Debug.Log($"[ContractDebugger] Received {contracts.Length} contract types for {symbol}");

            // Log each contract type immediately
            foreach (var c in contracts)
            {
                Debug.Log($"  [{symbol}] {c.contract_type} ({c.contract_display}) | " +
                          $"category={c.contract_category_display} | " +
                          $"duration={c.min_contract_duration}..{c.max_contract_duration} | " +
                          $"expiry={c.expiry_type} | sentiment={c.sentiment} | " +
                          $"barrier={c.barrier_category} | start={c.start_type}");
            }
        }

        private void LogSummary()
        {
            Debug.Log("=== CONTRACT AVAILABILITY SUMMARY ===");

            foreach (var kvp in _results)
            {
                string symbol = kvp.Key;
                var contracts = kvp.Value;

                // Group by category
                var categories = new Dictionary<string, List<string>>();
                foreach (var c in contracts)
                {
                    string cat = c.contract_category_display ?? c.contract_category ?? "unknown";
                    if (!categories.ContainsKey(cat))
                        categories[cat] = new List<string>();
                    categories[cat].Add($"{c.contract_type}({c.min_contract_duration}..{c.max_contract_duration})");
                }

                Debug.Log($"\n--- {symbol} ---");
                foreach (var cat in categories)
                {
                    Debug.Log($"  {cat.Key}: {string.Join(", ", cat.Value)}");
                }
            }

            Debug.Log("=== END SUMMARY ===");
        }
    }
}
