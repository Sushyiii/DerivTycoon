using System;
using System.Collections.Generic;
using DerivTycoon.API.Models;
using DerivTycoon.Core;
using UnityEngine;

namespace DerivTycoon.API
{
    public class DerivAPIService : MonoBehaviour
    {
        public static DerivAPIService Instance { get; private set; }

        [Header("Connection Settings")]
        public string publicWsUrl = "wss://ws.derivws.com/websockets/v3?app_id=1089";

        private DerivWebSocket _publicSocket;
        private readonly Dictionary<string, string> _subscriptionIds = new();
        private float _pingInterval = 25f;
        private float _pingTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ConnectPublic()
        {
            if (_publicSocket == null)
            {
                var go = new GameObject("DerivPublicWS");
                go.transform.SetParent(transform);
                _publicSocket = go.AddComponent<DerivWebSocket>();
            }

            _publicSocket.OnConnected += HandlePublicConnected;
            _publicSocket.OnMessageReceived += HandlePublicMessage;
            _publicSocket.OnError += HandlePublicError;
            _publicSocket.OnDisconnected += HandlePublicDisconnected;

            _publicSocket.Connect(publicWsUrl);
        }

        private void Update()
        {
            if (_publicSocket != null && _publicSocket.IsConnected)
            {
                _pingTimer += Time.deltaTime;
                if (_pingTimer >= _pingInterval)
                {
                    _pingTimer = 0f;
                    _publicSocket.Send("{\"ping\":1}");
                }
            }
        }

        private void HandlePublicConnected()
        {
            Debug.Log("[DerivAPI] Public WebSocket connected");
            EventBus.WebSocketConnected();
        }

        private void HandlePublicMessage(string json)
        {
            try
            {
                var msgType = JsonUtility.FromJson<MessageType>(json);

                switch (msgType.msg_type)
                {
                    case "tick":
                        HandleTickMessage(json);
                        break;
                    case "proposal":
                        HandleProposalMessage(json);
                        break;
                    case "buy":
                        HandleBuyMessage(json);
                        break;
                    case "active_symbols":
                        HandleActiveSymbolsMessage(json);
                        break;
                    case "ping":
                        break; // pong received, connection alive
                    default:
                        Debug.Log($"[DerivAPI] Unhandled message type: {msgType.msg_type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DerivAPI] Failed to parse message: {ex.Message}\n{json}");
            }
        }

        private static bool HasError(ErrorPayload error)
        {
            return error != null && !string.IsNullOrEmpty(error.code);
        }

        private void HandleTickMessage(string json)
        {
            // Quick check: if JSON contains "error", parse for error details
            if (json.Contains("\"error\""))
            {
                var response = JsonUtility.FromJson<TickResponse>(json);
                if (HasError(response.error))
                {
                    Debug.LogError($"[DerivAPI] Tick error: {response.error.message}");
                    return;
                }
            }

            var tickResp = JsonUtility.FromJson<TickResponse>(json);
            if (tickResp.tick == null) return;

            var tickData = new TickData
            {
                symbol = tickResp.tick.symbol,
                quote = tickResp.tick.quote,
                bid = tickResp.tick.bid,
                ask = tickResp.tick.ask,
                epoch = tickResp.tick.epoch,
                subscriptionId = tickResp.subscription?.id
            };

            if (tickResp.subscription != null)
                _subscriptionIds[tickResp.tick.symbol] = tickResp.subscription.id;

            EventBus.TickReceived(tickData);
        }

        private void HandleProposalMessage(string json)
        {
            var response = JsonUtility.FromJson<ProposalResponse>(json);
            if (HasError(response.error))
            {
                Debug.LogError($"[DerivAPI] Proposal error: {response.error.message}");
                return;
            }

            OnProposalReceived?.Invoke(response.proposal);
        }

        private void HandleBuyMessage(string json)
        {
            var response = JsonUtility.FromJson<BuyResponse>(json);
            if (HasError(response.error))
            {
                Debug.LogError($"[DerivAPI] Buy error: {response.error.message}");
                return;
            }

            OnBuyConfirmed?.Invoke(response.buy);
        }

        private void HandleActiveSymbolsMessage(string json)
        {
            var response = JsonUtility.FromJson<ActiveSymbolsResponse>(json);
            if (HasError(response.error))
            {
                Debug.LogError($"[DerivAPI] Active symbols error: {response.error.message}");
                return;
            }

            if (response.active_symbols != null)
            {
                foreach (var symbol in response.active_symbols)
                {
                    MarketDataStore.Instance?.RegisterActiveSymbol(symbol);
                }
                OnActiveSymbolsReceived?.Invoke(response.active_symbols);
            }
        }

        private void HandlePublicError(string error)
        {
            Debug.LogError($"[DerivAPI] Public WS error: {error}");
            EventBus.WebSocketError(error);
        }

        private void HandlePublicDisconnected(int code, string reason)
        {
            Debug.LogWarning($"[DerivAPI] Public WS disconnected ({code}): {reason}");
            // Auto-reconnect after delay
            Invoke(nameof(ReconnectPublic), 3f);
        }

        private void ReconnectPublic()
        {
            Debug.Log("[DerivAPI] Attempting reconnection...");
            ConnectPublic();
        }

        // ==================== Public API Methods ====================

        public event Action<ProposalPayload> OnProposalReceived;
        public event Action<BuyPayload> OnBuyConfirmed;
        public event Action<ActiveSymbol[]> OnActiveSymbolsReceived;

        public void SubscribeToTicks(string symbol)
        {
            if (_publicSocket == null || !_publicSocket.IsConnected)
            {
                Debug.LogWarning("[DerivAPI] Cannot subscribe, not connected");
                return;
            }

            string msg = $"{{\"ticks\":\"{symbol}\",\"subscribe\":1}}";
            _publicSocket.Send(msg);
            Debug.Log($"[DerivAPI] Subscribing to ticks: {symbol}");
        }

        public void UnsubscribeFromTicks(string symbol)
        {
            if (_subscriptionIds.TryGetValue(symbol, out string subId))
            {
                string msg = $"{{\"forget\":\"{subId}\"}}";
                _publicSocket.Send(msg);
                _subscriptionIds.Remove(symbol);
                Debug.Log($"[DerivAPI] Unsubscribed from: {symbol}");
            }
        }

        public void RequestActiveSymbols()
        {
            if (_publicSocket == null || !_publicSocket.IsConnected) return;
            _publicSocket.Send("{\"active_symbols\":\"brief\"}");
        }

        public void RequestProposal(string symbol, string contractType, float amount, int duration, string durationUnit = "m")
        {
            if (_publicSocket == null || !_publicSocket.IsConnected) return;

            var request = new ProposalRequest
            {
                symbol = symbol,
                contract_type = contractType,
                amount = amount,
                duration = duration,
                duration_unit = durationUnit
            };

            _publicSocket.Send(JsonUtility.ToJson(request));
        }

        public void BuyContract(string proposalId, float price)
        {
            if (_publicSocket == null || !_publicSocket.IsConnected) return;

            string msg = $"{{\"buy\":\"{proposalId}\",\"price\":{price}}}";
            _publicSocket.Send(msg);
        }

        public void Disconnect()
        {
            _publicSocket?.Close();
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}
