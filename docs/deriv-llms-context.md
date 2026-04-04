# Deriv API Context (sourced from https://developers.deriv.com/llms.txt)

## Overview

The Deriv API enables developers to build trading applications with real-time market data and programmatic trade execution. It provides both WebSocket and REST interfaces for accessing trading functionality.

## Key Components

**Authentication:** OAuth 2.0 with PKCE flow. Users authenticate through Deriv's authorization server, and developers exchange authorization codes for access tokens used in subsequent API calls.

**WebSocket Base URL:** `wss://api.derivws.com/trading/v1/options/ws/{endpoint}` - requires OTP obtained via REST API for authenticated connections

**REST Base URL:** `https://api.derivws.com`

## Main API Categories

**Account Management:** Balance queries, portfolio review, profit/loss summaries, statement retrieval, and transaction monitoring via WebSocket subscriptions.

**Market Data:** Active symbol listings, available contract types, tick streams, and historical price data accessible without authentication.

**Trading Operations:** Price proposals, contract purchases, early exits, position management with stop-loss/take-profit orders, and contract cancellation.

**System Functions:** Server health checks, time synchronization, and trading schedule information.

## Authentication Flow

1. User redirects to `https://auth.deriv.com/oauth2/auth` with PKCE parameters
2. After user authorization, receive code at redirect URI
3. Backend exchanges code for access token at `https://auth.deriv.com/oauth2/token`
4. Use token to call REST endpoint `/trading/v1/options/accounts/{accountId}/otp`
5. Connect authenticated WebSocket using returned OTP URL

## Contract Types

The platform supports diverse trading instruments including binary options (CALL, PUT), multipliers (MULTUP, MULTDOWN), accumulators (ACCU), and vanilla options. The `contracts_list` endpoint provides current available types.

## Rate Limits

WebSocket: 100 requests/second per connection; REST: 60 requests/minute per token; maximum 100 active subscriptions per connection.

---

# Additional Context Discovered During Development

## Working Connection (Confirmed April 4, 2026)

The V3 WebSocket endpoint works best for broad symbol access:
```
wss://ws.derivws.com/websockets/v3?app_id=1089
```

## Available Commodity Symbols (from active_symbols API)

| Symbol | Display Name | Market | Submarket |
|--------|-------------|--------|-----------|
| frxXAUUSD | Gold/USD | commodities | metals |
| frxXAGUSD | Silver/USD | commodities | metals |
| frxXPTUSD | Platinum/USD | commodities | metals |
| frxXPDUSD | Palladium/USD | commodities | metals |

**Synthetic indices (24/7, always live):**
| Symbol | Display Name | Market | Submarket |
|--------|-------------|--------|-----------|
| 1HZ100V | Volatility 100 Index | synthetic_index | random_index |

## All Available Markets
`synthetic_index`, `forex`, `indices`, `cryptocurrency`, `commodities`

## All Available Submarkets
`forex_basket`, `minor_pairs`, `major_pairs`, `asia_oceania_OTC`, `non_stable_coin`, `random_daily`, `crash_index`, `europe_OTC`, `commodity_basket`, `metals`, `jump_index`, `range_index`, `step_index`, `americas_OTC`, `random_index`

## Full API Docs Navigation
- `/docs/` - Introduction
- `/docs/intro/api-overview/` - API Overview
- `/docs/intro/authentication/` - Authentication
- `/docs/intro/oauth/` - OAuth 2.0
- `/docs/data/active-symbols/` - Active Symbols
- `/docs/data/contracts-for/` - Contracts For
- `/docs/data/contracts-list/` - Contracts List
- `/docs/data/ticks/` - Ticks
- `/docs/data/ticks-history/` - Ticks History
- `/docs/trading/proposal/` - Proposal
- `/docs/trading/buy/` - Buy
- `/docs/trading/sell/` - Sell
- `/docs/trading/cancel/` - Cancel
- `/docs/account/balance/` - Balance
- `/docs/account/portfolio/` - Portfolio
- `/docs/account/profit-table/` - Profit Table
- `/docs/subscription/` - Subscription Management
- `/docs/system/` - System
- `/docs/workflows/` - Workflows
