using DerivTycoon.API;
using UnityEngine;

namespace DerivTycoon.Core
{
    /// <summary>
    /// Temporary debug script to discover available commodity symbols.
    /// Attach to GameManager or any GO. Remove after symbol discovery.
    /// </summary>
    public class SymbolDebugger : MonoBehaviour
    {
        private DerivWebSocket _ws;
        private bool _connected;

        private void Start()
        {
            var go = new GameObject("DebugWS");
            go.transform.SetParent(transform);
            _ws = go.AddComponent<DerivWebSocket>();

            _ws.OnConnected += () =>
            {
                _connected = true;
                Debug.Log("[SymbolDebug] Connected. Requesting active symbols...");
                // Try both V3 format and the standard format
                _ws.Send("{\"active_symbols\":\"brief\",\"product_type\":\"basic\"}");
            };

            _ws.OnMessageReceived += (json) =>
            {
                if (json.Contains("\"msg_type\":\"active_symbols\""))
                {
                    // Extract commodity/metal symbols by chunking the JSON
                    // Look for market=commodity or submarket containing metal/energy
                    string[] marketFilters = { "\"market\":\"commodity\"", "\"submarket\":\"metals\"", "\"submarket\":\"energy\"", "Gold", "Silver", "Palladium", "Platinum", "Copper", "Zinc" };

                    // Split into individual symbol blocks
                    string[] blocks = json.Split(new[] { "},{" }, System.StringSplitOptions.None);

                    foreach (var block in blocks)
                    {
                        bool isRelevant = false;
                        foreach (var filter in marketFilters)
                        {
                            if (block.Contains(filter))
                            {
                                isRelevant = true;
                                break;
                            }
                        }

                        if (isRelevant)
                        {
                            Debug.Log($"[SymbolDebug] COMMODITY: {block.Substring(0, Mathf.Min(block.Length, 500))}");
                        }
                    }

                    // Also log all unique markets and submarkets
                    var markets = new System.Collections.Generic.HashSet<string>();
                    int idx = 0;
                    while ((idx = json.IndexOf("\"market\":\"", idx)) >= 0)
                    {
                        int start = idx + 10;
                        int end = json.IndexOf("\"", start);
                        if (end > start) markets.Add(json.Substring(start, end - start));
                        idx = end + 1;
                    }
                    Debug.Log($"[SymbolDebug] ALL MARKETS: {string.Join(", ", markets)}");

                    var submarkets = new System.Collections.Generic.HashSet<string>();
                    idx = 0;
                    while ((idx = json.IndexOf("\"submarket\":\"", idx)) >= 0)
                    {
                        int start = idx + 13;
                        int end = json.IndexOf("\"", start);
                        if (end > start) submarkets.Add(json.Substring(start, end - start));
                        idx = end + 1;
                    }
                    Debug.Log($"[SymbolDebug] ALL SUBMARKETS: {string.Join(", ", submarkets)}");
                }
                else if (json.Contains("\"error\""))
                {
                    Debug.LogError("[SymbolDebug] Error: " + json);
                }
            };

            _ws.OnError += (err) => Debug.LogError("[SymbolDebug] WS Error: " + err);

            _ws.Connect("wss://ws.derivws.com/websockets/v3?app_id=1089");
        }
    }
}
