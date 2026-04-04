using System;

namespace DerivTycoon.API.Models
{
    [Serializable]
    public class TickData
    {
        public string symbol;
        public float quote;
        public float bid;
        public float ask;
        public long epoch;
        public string subscriptionId;
    }

    // JSON wrappers matching Deriv API response format

    [Serializable]
    public class TickResponse
    {
        public string msg_type;
        public TickPayload tick;
        public SubscriptionInfo subscription;
        public ErrorPayload error;
    }

    [Serializable]
    public class TickPayload
    {
        public string symbol;
        public float quote;
        public float bid;
        public float ask;
        public long epoch;
    }

    [Serializable]
    public class SubscriptionInfo
    {
        public string id;
    }

    [Serializable]
    public class ErrorPayload
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class ProposalRequest
    {
        public int proposal = 1;
        public int subscribe = 1;
        public string contract_type;
        public string currency = "USD";
        public string symbol;
        public int duration;
        public string duration_unit = "m";
        public float amount;
        public string basis = "stake";
    }

    [Serializable]
    public class ProposalResponse
    {
        public string msg_type;
        public ProposalPayload proposal;
        public SubscriptionInfo subscription;
        public ErrorPayload error;
    }

    [Serializable]
    public class ProposalPayload
    {
        public float ask_price;
        public string display_value;
        public string id;
        public string longcode;
        public float payout;
        public float spot;
        public long spot_time;
    }

    [Serializable]
    public class BuyRequest
    {
        public string buy;
        public float price;
    }

    [Serializable]
    public class BuyResponse
    {
        public string msg_type;
        public BuyPayload buy;
        public ErrorPayload error;
    }

    [Serializable]
    public class BuyPayload
    {
        public float balance_after;
        public float buy_price;
        public long contract_id;
        public float payout;
        public long purchase_time;
        public string transaction_id;
    }

    [Serializable]
    public class ActiveSymbolsRequest
    {
        public string active_symbols = "brief";
    }

    [Serializable]
    public class ActiveSymbolsResponse
    {
        public string msg_type;
        public ActiveSymbol[] active_symbols;
        public ErrorPayload error;
    }

    [Serializable]
    public class ActiveSymbol
    {
        public string symbol;
        public string display_name;
        public string market;
        public string market_display_name;
        public string submarket;
        public string submarket_display_name;
        public int is_trading_suspended;
        public int exchange_is_open;
    }

    // Generic message type checker
    [Serializable]
    public class MessageType
    {
        public string msg_type;
    }
}
