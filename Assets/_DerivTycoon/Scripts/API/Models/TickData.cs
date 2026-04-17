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
        public int req_id;
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
        public int req_id;
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

    // ==================== Contracts For ====================

    [Serializable]
    public class ContractsForResponse
    {
        public string msg_type;
        public ContractsForPayload contracts_for;
        public ErrorPayload error;
    }

    [Serializable]
    public class ContractsForPayload
    {
        public AvailableContract[] available;
    }

    [Serializable]
    public class AvailableContract
    {
        public string contract_type;
        public string contract_display;
        public string contract_category;
        public string contract_category_display;
        public string min_contract_duration;
        public string max_contract_duration;
        public string expiry_type;
        public string sentiment;
        public string barrier_category;
        public string start_type;
    }

    // Generic message type checker
    [Serializable]
    public class MessageType
    {
        public string msg_type;
    }

    // ==================== New API Trading Models ====================

    // MULTUP proposal — no duration, uses underlying_symbol
    [Serializable]
    public class MultiplierProposalRequest
    {
        public int proposal = 1;
        public int subscribe = 1;
        public string contract_type = "MULTUP";
        public string underlying_symbol;
        public float amount;
        public string basis = "stake";
        public int multiplier;
        public string currency = "USD";
        public int req_id;
    }

    // Sell WS response — outer key is "sell" (not "sold")
    [Serializable]
    public class SellResponse
    {
        public string msg_type;
        public SellPayload sell;
        public ErrorPayload error;
        public int req_id;
    }

    [Serializable]
    public class SellPayload
    {
        public float balance_after;
        public float sold_for;
        public long contract_id;
        public long transaction_id;
    }

    // Balance WS response
    [Serializable]
    public class BalanceResponse
    {
        public string msg_type;
        public BalancePayload balance;
        public SubscriptionInfo subscription;
        public ErrorPayload error;
    }

    [Serializable]
    public class BalancePayload
    {
        public float balance;
        public string currency;
        public string loginid;
    }

    // proposal_open_contract WS request + response
    [Serializable]
    public class ProposalOpenContractRequest
    {
        public int proposal_open_contract = 1;
        public int subscribe = 1;
        public long contract_id;
    }

    [Serializable]
    public class ProposalOpenContractResponse
    {
        public string msg_type;
        public ProposalOpenContractPayload proposal_open_contract;
        public SubscriptionInfo subscription;
        public ErrorPayload error;
    }

    [Serializable]
    public class ProposalOpenContractPayload
    {
        public long contract_id;
        public string status;
        public int is_expired;
        public float profit;
        public float payout;
        public float bid_price;
        public float buy_price;
    }

    // REST: accounts response — wrapped in {"data": [...], "meta": {...}}
    [Serializable]
    public class AccountsRestResponse
    {
        public AccountRestData[] data;
    }

    [Serializable]
    public class AccountRestData
    {
        public string account_id;
        public string account_type;
        public string currency;
        public float balance;
    }

    // REST: OTP response — response.data.url is the WS URL
    [Serializable]
    public class OtpRestResponse
    {
        public OtpData data;
    }

    [Serializable]
    public class OtpData
    {
        public string url;
    }
}
