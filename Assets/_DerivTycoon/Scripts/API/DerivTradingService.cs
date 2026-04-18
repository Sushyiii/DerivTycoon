using System;
using System.Collections.Generic;
using System.Text;
using DerivTycoon.API.Models;
using UnityEngine;

namespace DerivTycoon.API
{
    public class DerivTradingService : MonoBehaviour
    {
        public static DerivTradingService Instance { get; private set; }

        public bool IsReady { get; private set; }

        public event Action<ProposalPayload, int> OnProposalReceived;
        public event Action<BuyPayload, int>      OnBuyConfirmed;
        public event Action<SellPayload>          OnSellConfirmed;
        public event Action<ProposalOpenContractPayload> OnContractUpdated;
        public event Action<float>                OnBalanceUpdated;
        public event Action<string, int>          OnTradingError; // (message, reqId)

        private DerivWebSocket _tradingSocket;

        // Caches subscription IDs for open contract subscriptions
        private readonly Dictionary<long, string> _contractSubIds = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Connect(string wsUrl)
        {
            if (_tradingSocket == null)
            {
                var go = new GameObject("DerivTradingWS");
                go.transform.SetParent(transform);
                _tradingSocket = go.AddComponent<DerivWebSocket>();
            }

            _tradingSocket.OnConnected -= OnConnected;
            _tradingSocket.OnMessageReceived -= OnMessage;
            _tradingSocket.OnError -= OnError;
            _tradingSocket.OnDisconnected -= OnDisconnected;

            _tradingSocket.OnConnected += OnConnected;
            _tradingSocket.OnMessageReceived += OnMessage;
            _tradingSocket.OnError += OnError;
            _tradingSocket.OnDisconnected += OnDisconnected;

            _tradingSocket.Connect(wsUrl);
        }

        private void OnConnected()
        {
            IsReady = true;
            Debug.Log("[DerivTrading] Connected to authenticated trading WS");
        }

        private void OnMessage(string json)
        {
            Debug.Log($"[DerivTrading] RX: {json}");

            try
            {
                var msgType = JsonUtility.FromJson<MessageType>(json);

                switch (msgType.msg_type)
                {
                    case "proposal":
                        HandleProposal(json);
                        break;
                    case "buy":
                        HandleBuy(json);
                        break;
                    case "sell":
                        HandleSell(json);
                        break;
                    case "proposal_open_contract":
                        HandleContractUpdate(json);
                        break;
                    case "balance":
                        HandleBalance(json);
                        break;
                    case "forget":
                    case "ping":
                        break;
                    default:
                        Debug.Log($"[DerivTrading] Unhandled msg_type: {msgType.msg_type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DerivTrading] Parse error: {ex.Message}\n{json}");
            }
        }

        private void HandleProposal(string json)
        {
            var response = JsonUtility.FromJson<ProposalResponse>(json);
            if (HasError(response.error))
            {
                Debug.LogWarning($"[DerivTrading] Proposal error: {response.error.message} (req={response.req_id})");
                OnTradingError?.Invoke(response.error.message, response.req_id);
                return;
            }
            OnProposalReceived?.Invoke(response.proposal, response.req_id);
        }

        private void HandleBuy(string json)
        {
            var response = JsonUtility.FromJson<BuyResponse>(json);
            if (HasError(response.error))
            {
                Debug.LogWarning($"[DerivTrading] Buy error: {response.error.message} (req={response.req_id})");
                OnTradingError?.Invoke(response.error.message, response.req_id);
                return;
            }
            OnBuyConfirmed?.Invoke(response.buy, response.req_id);
        }

        private void HandleSell(string json)
        {
            var response = JsonUtility.FromJson<SellResponse>(json);
            if (HasError(response.error)) return;
            OnSellConfirmed?.Invoke(response.sell);
        }

        private void HandleContractUpdate(string json)
        {
            var response = JsonUtility.FromJson<ProposalOpenContractResponse>(json);
            if (HasError(response.error)) return;

            var payload = response.proposal_open_contract;

            // Cache subscription ID on first message
            if (payload != null && response.subscription != null
                && !string.IsNullOrEmpty(response.subscription.id))
            {
                _contractSubIds[payload.contract_id] = response.subscription.id;
            }

            OnContractUpdated?.Invoke(payload);
        }

        private void HandleBalance(string json)
        {
            var response = JsonUtility.FromJson<BalanceResponse>(json);
            if (HasError(response.error)) return;
            OnBalanceUpdated?.Invoke(response.balance.balance);
        }

        private void OnError(string error)
        {
            Debug.LogError($"[DerivTrading] WS error: {error}");
        }

        private void OnDisconnected(int code, string reason)
        {
            IsReady = false;
            Debug.LogWarning($"[DerivTrading] Disconnected ({code}): {reason}");
        }

        private static bool HasError(ErrorPayload error)
            => error != null && !string.IsNullOrEmpty(error.code);

        // ==================== Public Trading Methods ====================

        public void RequestMultiplierProposal(string symbol, float stake, int multiplier, int reqId)
        {
            var req = new MultiplierProposalRequest
            {
                underlying_symbol = symbol,
                amount = stake,
                multiplier = multiplier,
                req_id = reqId
            };
            string json = JsonUtility.ToJson(req);
            Debug.Log($"[DerivTrading] TX proposal: {json}");
            _tradingSocket.Send(json);
        }

        public void RequestCallProposal(string symbol, float stake, int durationSecs, float barrierOffset, int reqId)
        {
            var sb = new StringBuilder();
            sb.Append("{\"proposal\":1,\"subscribe\":1,\"contract_type\":\"CALL\"");
            sb.Append($",\"underlying_symbol\":\"{symbol}\"");
            sb.Append($",\"amount\":{stake:F2}");
            sb.Append(",\"basis\":\"stake\"");
            sb.Append($",\"duration\":{durationSecs}");
            sb.Append(",\"duration_unit\":\"s\"");
            sb.Append(",\"currency\":\"USD\"");
            if (barrierOffset != 0f)
                sb.Append($",\"barrier\":\"{barrierOffset:+0.00;-0.00}\"");
            sb.Append($",\"req_id\":{reqId}}}");

            string json = sb.ToString();
            Debug.Log($"[DerivTrading] TX call proposal: {json}");
            _tradingSocket.Send(json);
        }

        public void BuyProposal(string proposalId, float price, int reqId)
        {
            string json = $"{{\"buy\":\"{proposalId}\",\"price\":{price:F2},\"req_id\":{reqId}}}";
            Debug.Log($"[DerivTrading] TX buy: {json}");
            _tradingSocket.Send(json);
        }

        public void ForgetProposal(string proposalId)
        {
            string json = $"{{\"forget\":\"{proposalId}\"}}";
            _tradingSocket.Send(json);
        }

        public void SellContract(long contractId)
        {
            string json = $"{{\"sell\":{contractId},\"price\":0}}";
            Debug.Log($"[DerivTrading] TX sell: {json}");
            _tradingSocket.Send(json);
        }

        public void SubscribeContractUpdates(long contractId)
        {
            var req = new ProposalOpenContractRequest { contract_id = contractId };
            string json = JsonUtility.ToJson(req);
            Debug.Log($"[DerivTrading] TX subscribe contract: {json}");
            _tradingSocket.Send(json);
        }

        public void ForgetContractUpdates(string subscriptionId)
        {
            if (string.IsNullOrEmpty(subscriptionId)) return;
            string json = $"{{\"forget\":\"{subscriptionId}\"}}";
            _tradingSocket.Send(json);
        }

        public string GetContractSubId(long contractId)
            => _contractSubIds.TryGetValue(contractId, out string subId) ? subId : null;

        public void SubscribeBalance()
        {
            _tradingSocket.Send("{\"balance\":1,\"subscribe\":1}");
        }

        private void OnDestroy()
        {
            _tradingSocket?.Close();
        }
    }
}
