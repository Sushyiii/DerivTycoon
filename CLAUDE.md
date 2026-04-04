# DerivTycoon

## Project Overview
A tycoon-style Unity WebGL game for the **Deriv API Grand Prix 2026** competition (deadline: April 17, 2026). Commodity trades are visualized as buildings in a city grid. Buildings grow when trades profit and deteriorate when losing. Solo developer project.

## Competition Details
- **Event**: Deriv API Grand Prix 2026 (internal Deriv company competition)
- **Build Period**: April 6-17, 2026
- **Deliverables**: Live web app, GitHub repo, vibe-coding documentation
- **Judging**: Innovation, UX, Technical Execution
- **Inspiration**: Gaming apps built with Deriv API V2 (e.g., Deriv Accumulators)
- **Prize**: Winning app showcased to global clients

## Tech Stack
- **Engine**: Unity 6 (6000.0.29f1) with URP (Universal Render Pipeline)
- **Build Target**: WebGL
- **Hosting**: Vercel
- **API**: Deriv API via WebSocket V3
- **Backend** (stretch): Vercel serverless functions for OAuth

## Game Concept
- Player opens trades on commodities/metals/synthetic indices
- Each trade spawns a thematic building on a city grid
- Buildings visually react to live P&L from real market data
- City skyline = portfolio visualization

## Commodity -> Building Mapping

| Commodity | Symbol (Confirmed) | Building Type | Profit Visual | Loss Visual |
|-----------|-------------------|---------------|---------------|-------------|
| Gold | frxXAUUSD | Gold Mine | Shaft glows, gold nuggets float | Abandoned, dim |
| Silver | frxXAGUSD | Silver Mint | Columns grow, silver sheen | Tarnished, idle |
| Platinum | frxXPTUSD | Platinum Forge | Furnace blazing, ingots stacking | Cooled, cracked |
| Palladium | frxXPDUSD | Palladium Refinery | Pipes flowing, sparks flying | Pipes rusted, silent |
| Volatility Index | 1HZ100V | Trading Tower | Screens flashing green, tower grows | Screens red, tower shrinks |

**Important symbol notes:**
- Symbols confirmed from the `active_symbols` API endpoint (April 4, 2026)
- Metals markets (`frxX*`) are **weekday only** (Mon-Fri). They return "market is presently closed" on weekends.
- `1HZ100V` (Volatility 100 Index) is **24/7** — always has live ticks. This is essential for demo/judging.
- Oil, Natural Gas, Sugar are **NOT available** on this API. Only metals exist under `commodities` market.
- Available markets: `synthetic_index`, `forex`, `indices`, `cryptocurrency`, `commodities`
- Available submarkets include: `metals`, `commodity_basket`, `random_index`, `random_daily`, `crash_index`, `jump_index`, etc.

## Deriv API Reference

### Connection (CONFIRMED WORKING)
- **WebSocket URL**: `wss://ws.derivws.com/websockets/v3?app_id=1089`
- This is the V3 API endpoint. The V2 REST/WS endpoints in Deriv docs (`api.derivws.com/trading/v1/...`) are for the newer API but we use V3 for broader symbol support.
- App ID `1089` is a public/demo app ID.

### Rate Limits
- WebSocket: 100 requests/second
- REST: 60 requests/minute
- Max concurrent subscriptions: 100

### Key WebSocket Messages

**Subscribe to ticks (no auth):**
```json
{"ticks": "frxXAUUSD", "subscribe": 1}
```

**Tick response:**
```json
{
  "tick": {"bid": 1950.50, "ask": 1950.75, "epoch": 1704067200, "quote": 1950.625, "symbol": "frxXAUUSD"},
  "subscription": {"id": "sub_123"},
  "msg_type": "tick"
}
```

**Get active symbols:**
```json
{"active_symbols": "brief", "product_type": "basic"}
```

**Request proposal:**
```json
{
  "proposal": 1, "subscribe": 1, "contract_type": "HIGHER",
  "currency": "USD", "symbol": "frxXAUUSD",
  "duration": 5, "duration_unit": "m", "amount": 100, "basis": "stake"
}
```

**Buy contract:**
```json
{"buy": "proposal_id_here", "price": 95.50}
```

**Sell contract:**
```json
{"sell": 12345678, "price": 150.00}
```

**Unsubscribe:**
```json
{"forget": "subscription_id"}
```

**Keep-alive ping (send every 25s):**
```json
{"ping": 1}
```

### Authentication Flow (OAuth2 + PKCE) — Stretch Goal
1. Generate `code_verifier` (43-128 char random string) and `code_challenge` (BASE64URL(SHA256(verifier)))
2. Redirect to `https://auth.deriv.com/oauth2/auth?response_type=code&client_id={APP_ID}&redirect_uri={URL}&code_challenge={challenge}&code_challenge_method=S256&state={state}`
3. User authorizes, Deriv redirects with `?code=...`
4. **Server-side**: Exchange code for token via `POST https://auth.deriv.com/oauth2/token` (with code_verifier)
5. Get OTP: `POST https://api.derivws.com/trading/v1/options/accounts/{accountId}/otp` with Bearer token
6. Connect authenticated WS: `wss://api.derivws.com/trading/v1/options/ws?otp={otp}`

### API Categories (28 endpoints total)
- **Account Management** (5): balance, portfolio, profit_table, statement, transaction
- **Market Data** (5): active_symbols, contracts_for, contracts_list, ticks, ticks_history
- **Trading Operations** (7): proposal, buy, sell, proposal_open_contract, contract_update, contract_update_history, cancel
- **Subscription** (2): forget, forget_all
- **System** (4): ping, time, trading_times, health_check
- **REST** (5): get accounts, create account, reset demo balance, OTP, WebSocket setup

## Architecture

### WebSocket in WebGL — Critical Design
Unity WebGL **cannot** use `System.Net.Sockets` or `System.Net.WebSockets.ClientWebSocket`. We use a dual-mode approach:

**In WebGL builds:**
- `DerivWebSocket.jslib` — JavaScript plugin that calls `new WebSocket()` in the browser
- Uses Unity's `SendMessage()` to call back into C# (prefixes connection ID to messages with `|` separator)
- C# side uses `[DllImport("__Internal")]` to call jslib functions: `WS_Create`, `WS_Send`, `WS_Close`, `WS_GetState`

**In Editor (for development):**
- Uses `System.Net.WebSockets.ClientWebSocket` natively
- Async receive loop with `StringBuilder` for message assembly
- Guarded by `#if !UNITY_WEBGL || UNITY_EDITOR`

**Thread safety:**
- Both modes enqueue callbacks into `ConcurrentQueue<Action>` on `DerivWebSocket`
- `Update()` drains the queue each frame to marshal onto Unity's main thread

### Project Structure
```
Assets/
├── _DerivTycoon/                    # All game code lives here
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs       # Singleton, state machine, balance, initialization
│   │   │   ├── GameState.cs         # Enum: Boot, MainMenu, DemoPlaying, LivePlaying, Trading, Placement, Paused
│   │   │   ├── EventBus.cs          # Static event system (decoupled communication)
│   │   │   └── SymbolDebugger.cs    # TEMPORARY — remove after symbol discovery (can be deleted)
│   │   ├── API/
│   │   │   ├── DerivWebSocket.cs    # Dual-mode WebSocket wrapper (jslib + native)
│   │   │   ├── DerivAPIService.cs   # High-level API: subscribe ticks, request proposals, buy/sell
│   │   │   ├── MarketDataStore.cs   # Tick cache (last 100 ticks per symbol), latest prices
│   │   │   └── Models/
│   │   │       └── TickData.cs      # All data models: TickData, ProposalData, BuyData, ActiveSymbols, etc.
│   │   ├── Trading/                 # NOT YET IMPLEMENTED
│   │   │   ├── TradeManager.cs
│   │   │   └── Trade.cs             # (Trade class currently defined in EventBus.cs — move here later)
│   │   ├── City/                    # NOT YET IMPLEMENTED
│   │   │   ├── CityGrid.cs
│   │   │   ├── GridCell.cs
│   │   │   ├── CityCamera.cs
│   │   │   └── BuildingPlacer.cs
│   │   ├── Buildings/               # NOT YET IMPLEMENTED
│   │   │   ├── BuildingFactory.cs
│   │   │   ├── BuildingController.cs
│   │   │   ├── BuildingVisuals.cs
│   │   │   └── BuildingConfig.cs
│   │   └── UI/                      # NOT YET IMPLEMENTED
│   │       ├── HUD.cs
│   │       ├── TradePanelUI.cs
│   │       ├── BuildingInfoUI.cs
│   │       ├── PortfolioPanelUI.cs
│   │       ├── MarketTickerUI.cs
│   │       └── TutorialUI.cs
│   ├── Plugins/WebGL/
│   │   └── DerivWebSocket.jslib     # IMPLEMENTED — JS WebSocket bridge
│   ├── Prefabs/Buildings/           # Empty — needs building prefabs
│   ├── Prefabs/UI/                  # Empty
│   ├── Prefabs/Effects/             # Empty
│   ├── ScriptableObjects/           # Empty — needs commodity configs
│   ├── Materials/Buildings/         # Empty — needs per-commodity materials
│   ├── Materials/Grid/              # Empty
│   ├── Scenes/                      # Empty — using root SampleScene for now
│   ├── Art/Textures/                # Empty
│   ├── Art/Sprites/                 # Empty
│   └── WebGLTemplates/DerivTycoon/  # Empty — needs custom template
├── _Archive/                        # Archived test assets (old pixel building scripts)
├── Materials/                       # Test materials from pixel building experiments (can be cleaned up)
├── Prefabs/                         # SmallBuilding.prefab test prefab (can be cleaned up)
├── Scenes/
│   └── SampleScene.unity            # ACTIVE SCENE — has GameManager GO with GameManager component
└── Settings/                        # URP render pipeline settings (PC + Mobile profiles)
```

### Game States
```
BOOT → Connect public WS → Subscribe to 5 commodity ticks
  → MAIN_MENU → "Play Demo" → CITY_VIEW
  → CITY_VIEW → "New Trade" → TRADE_PANEL → Select commodity, Rise/Fall, stake, duration
  → PLACEMENT_MODE → Click grid cell → Building spawns, trade opens → CITY_VIEW
  → Click building → BUILDING_INFO (P&L, sell early)
  → "Portfolio" → PORTFOLIO_VIEW
```

### Demo Mode (MVP)
- No authentication required
- Public WS for real tick data
- Virtual $10,000 balance
- Simulated trades using real price movements to determine win/loss
- Judges can play immediately without an account

## Building Visual States
- **Construction** (first 3s): rises from ground, dust particles
- **Strong Profit (>5%)**: 1.3x scale, warm golden tint, particles, flag on top
- **Mild Profit (0-5%)**: 1.0-1.3x, colors trending warm
- **Breakeven (~0%)**: Base scale, neutral idle
- **Mild Loss (0 to -5%)**: 0.7-1.0x, desaturated colors
- **Heavy Loss (<-5%)**: 0.7x, grey tint, crack particles
- **Closed (profit)**: Stays at final size, celebratory burst, becomes landmark
- **Closed (loss)**: Crumbles to ruin, can be demolished

## UI Layout
- **Top bar**: Balance, active trade count, portfolio button, settings
- **Center**: 3D isometric city view with pan/zoom
- **Bottom ticker**: Scrolling live commodity prices
- **Trade panel**: Slides up from bottom as overlay
- **Building info**: Floating panel on click

## Development Conventions
- All game code under `Assets/_DerivTycoon/`
- Use `JsonUtility` for JSON parsing (no external dependencies)
- Use URP Lit shader for all materials
- ScriptableObjects for configuration (per-commodity, game settings)
- Event-based communication via static `EventBus` (no tight coupling between systems)
- Preprocessor guards for WebGL vs Editor: `#if UNITY_WEBGL && !UNITY_EDITOR`
- Namespaces: `DerivTycoon.Core`, `DerivTycoon.API`, `DerivTycoon.API.Models`, `DerivTycoon.Trading`, `DerivTycoon.City`, `DerivTycoon.Buildings`, `DerivTycoon.UI`

## Known Issues & Gotchas

### JsonUtility and error fields
`JsonUtility.FromJson` always instantiates nested class fields (like `ErrorPayload error`) even if the JSON doesn't contain them. This means `response.error != null` is ALWAYS true. **Use `HasError()` helper** which checks `!string.IsNullOrEmpty(error.code)` instead. This pattern is already implemented in `DerivAPIService.cs`.

### Unity serialization overrides code defaults
If you change default values for serialized fields in code (like `commoditySymbols` array in `GameManager`), existing scene instances keep their OLD serialized values. You must **delete and recreate** the GameObject in the scene, or manually update via Inspector. The MCP `set_property` tool does NOT support array types.

### Scene setup
The active scene is `Assets/SampleScene.unity` (originally `Assets/Scenes/SampleScene.unity` but Unity moved it). It contains:
- Main Camera
- Directional Light
- Global Volume (URP post-processing)
- **GameManager** (empty GO at origin) with `DerivTycoon.Core.GameManager` component

### CoPlay MCP
The project has `com.coplaydev.coplay` package installed for Unity MCP integration. This allows Claude to interact directly with the Unity Editor (create GameObjects, add components, play/stop, read logs, etc.). The MCP server is configured as: `uvx --python ">=3.11" coplay-mcp-server@latest`

## Implementation Progress

### Phase 1: Foundation — COMPLETED
- [x] Project folder structure created under `Assets/_DerivTycoon/`
- [x] Test pixel buildings archived to `Assets/_Archive/`
- [x] `DerivWebSocket.jslib` — JS WebSocket bridge for WebGL
- [x] `DerivWebSocket.cs` — Dual-mode C# wrapper (native for Editor, jslib for WebGL)
- [x] `DerivAPIService.cs` — High-level API (tick subscriptions, proposals, buy/sell, auto-reconnect, 25s keep-alive ping)
- [x] `MarketDataStore.cs` — Tick cache with circular buffer (100 ticks per symbol)
- [x] `GameManager.cs` — Singleton with state machine, balance management, commodity config
- [x] `EventBus.cs` — Decoupled static event system
- [x] `GameState.cs` — State enum
- [x] `TickData.cs` — All API data models (Tick, Proposal, Buy, ActiveSymbols, etc.)
- [x] **VERIFIED**: WebSocket connects, live tick data flows for `1HZ100V` (~1 tick/sec)
- [x] **VERIFIED**: Metals symbols are correct but market-hours dependent

### Phase 2: City + Grid — NOT STARTED
- [ ] `CityGrid.cs` — 8x8 grid, cell occupancy management
- [ ] `GridCell.cs` — Individual cell state
- [ ] `CityCamera.cs` — Isometric camera with pan/zoom
- [ ] `BuildingPlacer.cs` — Raycast to grid, ghost preview, click to place
- [ ] `BuildingFactory.cs` — Creates building per commodity type
- [ ] 5 building prefabs (cube-based for MVP)

### Phase 3: Trading + Visuals — NOT STARTED
- [ ] `TradeManager.cs` — Manage active trades, P&L tracking
- [ ] `Trade.cs` — Trade model (move from EventBus.cs)
- [ ] Demo mode logic (virtual balance, simulated contracts)
- [ ] `BuildingVisuals.cs` — P&L-driven scale, color, particles
- [ ] `TradePanelUI.cs`, `HUD.cs`, `MarketTickerUI.cs`

### Phase 4: Polish — NOT STARTED
- [ ] `PortfolioPanelUI.cs`, `BuildingInfoUI.cs`, `ToastNotification.cs`
- [ ] `MainMenuUI.cs`, `TutorialUI.cs`
- [ ] Scene transitions, full state machine
- [ ] Stretch: OAuth backend + live trading

### Phase 5: Ship — NOT STARTED
- [ ] Visual polish, post-processing, lighting
- [ ] WebGL optimization (<30MB gzipped target)
- [ ] Custom WebGL template
- [ ] Deploy to Vercel
- [ ] Vibe-coding documentation
- [ ] Cross-browser testing

## MVP vs Stretch

**MVP (Must ship):**
- Public WS connection with live commodity/synthetic ticks
- Demo mode ($10K virtual balance, real prices)
- 5 building types with P&L-driven visuals
- Grid city, placement, trade panel, portfolio
- Tutorial overlay
- Deployed on Vercel

**Stretch:**
- OAuth login for real Deriv demo account trading
- Sound effects
- Post-processing bloom on profitable buildings
- Building upgrade system
- Day/night cycle

## Complete File Reference (API Surfaces)

This section documents every implemented script's public interface so a new developer/AI session can understand the codebase without reading every file.

### DerivWebSocket.cs (`DerivTycoon.API`) — 271 lines
Dual-mode WebSocket wrapper. Platform-agnostic interface for WebSocket communication.

**Public API:**
```csharp
// Events
event Action OnConnected;
event Action<string> OnMessageReceived;   // json payload (WebGL: stripped of "id|" prefix)
event Action<string> OnError;
event Action<int, string> OnDisconnected; // (closeCode, reason)

// Properties
bool IsConnected { get; }

// Methods
void Connect(string url);
void Send(string message);
void Close();
```

**WebGL Callbacks** (called by jslib via `SendMessage`, must remain public):
```csharp
void OnWsOpen(string idStr);       // "connectionId"
void OnWsMessage(string data);     // "connectionId|jsonPayload"
void OnWsError(string data);       // "connectionId|errorMessage"
void OnWsClose(string data);       // "connectionId|code|reason"
```

**Native (Editor) internals:**
- `ClientWebSocket` with async connect/receive/send
- `ReceiveLoop()` — 8192-byte buffer, `StringBuilder` for multi-frame messages
- `CancellationTokenSource` for clean shutdown
- All callbacks go through `ConcurrentQueue<Action>` → drained in `Update()`

**Preprocessor guards:**
- `#if UNITY_WEBGL && !UNITY_EDITOR` — jslib `[DllImport("__Internal")]` for `WS_Create`, `WS_Send`, `WS_Close`, `WS_GetState`
- `#if !UNITY_WEBGL || UNITY_EDITOR` — native `ClientWebSocket` implementation

---

### DerivWebSocket.jslib — 75 lines
JavaScript WebSocket bridge for Unity WebGL. Located at `Assets/_DerivTycoon/Plugins/WebGL/`.

**Global state:**
- `$webSockets` — object mapping `{id: {ws: WebSocket, goName: string}}`
- `$nextId` — auto-incrementing connection counter

**Exported functions:**
```javascript
WS_Create(urlPtr, gameObjectNamePtr) → int   // Returns connection ID or -1 on failure
WS_Send(id, msgPtr) → int                    // Returns 1 on success, 0 if not OPEN
WS_Close(id, code) → void                    // Closes with code (default 1000)
WS_GetState(id) → int                        // WebSocket.readyState (0-3), 3 if not found
```

**Callback message format** (sent to C# via `SendMessage`):
- `OnWsOpen`: `"connectionId"`
- `OnWsMessage`: `"connectionId|jsonPayload"`
- `OnWsError`: `"connectionId|WebSocket error"`
- `OnWsClose`: `"connectionId|closeCode|reason"`

**Dependencies:** `autoAddDeps` for `$webSockets` and `$nextId`, merged into `LibraryManager.library`.

---

### DerivAPIService.cs (`DerivTycoon.API`) — 265 lines
High-level API service. Singleton MonoBehaviour.

**Public API:**
```csharp
// Singleton
static DerivAPIService Instance { get; }

// Events
event Action<ProposalPayload> OnProposalReceived;
event Action<BuyPayload> OnBuyConfirmed;
event Action<ActiveSymbol[]> OnActiveSymbolsReceived;

// Connection
string publicWsUrl = "wss://ws.derivws.com/websockets/v3?app_id=1089"; // [SerializeField]
void ConnectPublic();
void Disconnect();

// Market Data
void SubscribeToTicks(string symbol);      // Sends {"ticks":"symbol","subscribe":1}
void UnsubscribeFromTicks(string symbol);  // Sends {"forget":"subscriptionId"}
void RequestActiveSymbols();               // Sends {"active_symbols":"brief"}

// Trading
void RequestProposal(string symbol, string contractType, float amount, int duration, string durationUnit = "m");
void BuyContract(string proposalId, float price);
```

**Internal behavior:**
- `_subscriptionIds` dictionary maps `symbol → subscriptionId` for unsubscribe
- 25-second ping keep-alive in `Update()`
- Auto-reconnect via `Invoke(nameof(ReconnectPublic), 3f)` on disconnect
- Message routing by `msg_type`: tick, proposal, buy, active_symbols, ping
- `HasError()` — static helper checking `!string.IsNullOrEmpty(error.code)` (JsonUtility workaround)
- Tick messages fire `EventBus.TickReceived(tickData)`
- Active symbols registered to `MarketDataStore.Instance`

---

### MarketDataStore.cs (`DerivTycoon.API`) — 93 lines
Tick data cache. Singleton MonoBehaviour.

**Public API:**
```csharp
static MarketDataStore Instance { get; }

TickData GetLatestTick(string symbol);
List<TickData> GetTickHistory(string symbol);  // Up to 100 most recent
float GetLatestPrice(string symbol);           // Returns quote, 0 if no data
float GetPriceChange(string symbol);           // % change between last 2 ticks

void RegisterActiveSymbol(ActiveSymbol symbol);
ActiveSymbol GetSymbolInfo(string symbol);
bool IsMarketOpen(string symbol);              // exchange_is_open==1 && is_trading_suspended==0
```

**Storage:**
- `Dictionary<string, List<TickData>> _tickHistory` — circular buffer, max 100 per symbol (removes index 0 when full)
- `Dictionary<string, TickData> _latestTicks` — most recent tick per symbol
- `Dictionary<string, ActiveSymbol> _activeSymbols` — symbol metadata

**Auto-caching:** Subscribes to `EventBus.OnTickReceived` in `OnEnable()`.

---

### GameManager.cs (`DerivTycoon.Core`) — 132 lines
Central game controller. Singleton with `DontDestroyOnLoad`.

**Public API:**
```csharp
static GameManager Instance { get; }

// Properties
GameState CurrentState { get; }
float Balance { get; }
bool IsDemoMode { get; }

// Settings (serialized, editable in Inspector)
float startingBalance = 10000f;
string[] commoditySymbols = {"frxXAUUSD", "frxXAGUSD", "frxXPTUSD", "frxXPDUSD", "1HZ100V"};

// Methods
void StartDemoMode();           // Sets IsDemoMode=true, resets balance, state→DemoPlaying
void SetState(GameState state); // Fires EventBus.GameStateChanged
bool SpendBalance(float amt);   // Returns false if insufficient funds
void AddBalance(float amt);
string GetCommodityName(string symbol); // e.g., "frxXAUUSD" → "Gold"
```

**Initialization flow (`Start` → `Initialize`):**
1. Sets balance to `startingBalance`
2. Sets state to `Boot`
3. Ensures `MarketDataStore` and `DerivAPIService` exist (creates if missing)
4. Subscribes to `EventBus.OnWebSocketConnected`
5. Calls `DerivAPIService.Instance.ConnectPublic()`

**On WebSocket connected (`OnConnected`):**
1. Requests active symbols
2. Subscribes to all 5 commodity tick streams
3. Sets state to `MainMenu`

---

### EventBus.cs (`DerivTycoon.Core`) — 72 lines
Static event system for decoupled communication.

**Events (all static):**
| Event | Signature | Fired By |
|-------|-----------|----------|
| `OnGameStateChanged` | `Action<GameState>` | `GameManager.SetState()` |
| `OnTickReceived` | `Action<TickData>` | `DerivAPIService.HandleTickMessage()` |
| `OnTradeOpened` | `Action<Trade>` | (future: TradeManager) |
| `OnTradeUpdated` | `Action<Trade>` | (future: TradeManager) |
| `OnTradeClosed` | `Action<Trade>` | (future: TradeManager) |
| `OnBalanceChanged` | `Action<float>` | `GameManager.SpendBalance/AddBalance()` |
| `OnWebSocketConnected` | `Action` | `DerivAPIService.HandlePublicConnected()` |
| `OnWebSocketError` | `Action<string>` | `DerivAPIService.HandlePublicError()` |
| `OnCommoditySelected` | `Action<string>` | (future: TradePanelUI) |
| `OnToastMessage` | `Action<string>` | (future: various UI) |

**Trade class** (nested in EventBus.cs, to be moved to `Trading/Trade.cs`):
```csharp
class Trade {
    string Id, Symbol, CommodityName, ContractType; // "CALL" or "PUT"
    float Stake, EntryPrice, CurrentPrice, Duration, StartTime;
    bool IsActive;
    int GridX, GridY;

    float PnL { get; }         // Currency P&L based on contract direction
    float PnLPercent { get; }  // Percentage P&L
}
```
P&L formula: CALL → `(current - entry) / entry * stake`; PUT → `(entry - current) / entry * stake`

---

### GameState.cs (`DerivTycoon.Core`) — 13 lines
```csharp
enum GameState { Boot, MainMenu, DemoPlaying, LivePlaying, Trading, Placement, Paused }
```

---

### TickData.cs (`DerivTycoon.API.Models`) — 144 lines
All JSON-serializable data models for Deriv API. Every class is `[Serializable]` for `JsonUtility`.

**Data transfer objects:**
| Class | Fields | Used For |
|-------|--------|----------|
| `TickData` | symbol, quote, bid, ask, epoch, subscriptionId | Internal tick representation |
| `TickResponse` | msg_type, tick(TickPayload), subscription(SubscriptionInfo), error(ErrorPayload) | Parsing `{"msg_type":"tick",...}` |
| `TickPayload` | symbol, quote, bid, ask, epoch | Raw tick data from API |
| `SubscriptionInfo` | id | Subscription ID for forget/unsubscribe |
| `ErrorPayload` | code, message | API error details |
| `ProposalRequest` | proposal=1, subscribe=1, contract_type, currency="USD", symbol, duration, duration_unit="m", amount, basis="stake" | Sending proposal requests |
| `ProposalResponse` | msg_type, proposal(ProposalPayload), subscription, error | Parsing proposal responses |
| `ProposalPayload` | ask_price, display_value, id, longcode, payout, spot, spot_time | Proposal pricing data |
| `BuyRequest` | buy(proposalId), price | Sending buy requests |
| `BuyResponse` | msg_type, buy(BuyPayload), error | Parsing buy confirmations |
| `BuyPayload` | balance_after, buy_price, contract_id, payout, purchase_time, transaction_id | Purchase confirmation data |
| `ActiveSymbolsRequest` | active_symbols="brief" | (unused — constructed as string in DerivAPIService) |
| `ActiveSymbolsResponse` | msg_type, active_symbols[], error | Parsing symbol list responses |
| `ActiveSymbol` | symbol, display_name, market, market_display_name, submarket, submarket_display_name, is_trading_suspended, exchange_is_open | Symbol metadata |
| `MessageType` | msg_type | Generic message router (first-pass parse) |

---

### SymbolDebugger.cs (`DerivTycoon.Core`) — 92 lines
**Status: TEMPORARY — safe to delete.**
Debug utility that opens a separate WebSocket, requests `active_symbols`, and logs commodity/metal symbols plus all unique markets and submarkets. Used during Phase 1 to discover correct symbol names. Not attached to any scene objects.

---

## Data Model Reference (JSON ↔ C# Mapping)

### Tick Subscription Flow
```
C# sends:    {"ticks":"frxXAUUSD","subscribe":1}
API returns: {"msg_type":"tick","tick":{"symbol":"frxXAUUSD","quote":1950.625,"bid":1950.50,"ask":1950.75,"epoch":1704067200},"subscription":{"id":"abc123"}}

DerivAPIService parses → TickResponse → creates TickData → fires EventBus.TickReceived
MarketDataStore catches event → stores in _tickHistory + _latestTicks
```

### Proposal Flow
```
C# sends:    {"proposal":1,"subscribe":1,"contract_type":"HIGHER","currency":"USD","symbol":"frxXAUUSD","duration":5,"duration_unit":"m","amount":100,"basis":"stake"}
API returns: {"msg_type":"proposal","proposal":{"ask_price":95.50,"id":"prop_123","payout":195.00,...},"subscription":{"id":"sub_456"}}

DerivAPIService parses → ProposalResponse → fires OnProposalReceived(ProposalPayload)
```

### Buy Flow
```
C# sends:    {"buy":"prop_123","price":95.50}
API returns: {"msg_type":"buy","buy":{"balance_after":9904.50,"buy_price":95.50,"contract_id":12345,"payout":195.00,...}}

DerivAPIService parses → BuyResponse → fires OnBuyConfirmed(BuyPayload)
```

## Existing Assets Inventory

### Materials (20 files in `Assets/Materials/`)
Test materials from pixel building experiments. All use URP Lit shader:
- **Basic set**: WallMat (beige), RoofMat (dark grey), DoorMat (brown), WindowMat (light blue)
- **Pixel Set 1**: PixelBrick, PixelRoof, PixelDoor, PixelTrim, PixelWindow, PixelPorch, PixelChimney, PixelGround
- **Pixel Set 2**: PixelBrick2, PixelRoof2, PixelDoor2
- **Pixel Set 3**: PixelBrick3, PixelRoof3, PixelTrim3, PixelDoor3, PixelAwning3
- **Status**: Can be cleaned up or repurposed. No commodity-themed materials exist yet.

### Prefabs (1 file in `Assets/Prefabs/`)
- `SmallBuilding.prefab` — 5-component test building (Base, Roof, Door, Window1, Window2). Generated by archived `BuildSmallBuilding.cs`. Not integrated with game systems.

### URP Settings (7 files in `Assets/Settings/`)
- DefaultVolumeProfile, SampleSceneProfile — post-processing volumes
- PC_RPAsset + PC_Renderer — desktop render pipeline config
- Mobile_RPAsset + Mobile_Renderer — mobile render pipeline config
- UniversalRenderPipelineGlobalSettings — global URP config
- **Status**: Default URP setup, no custom changes yet.

### Input System
- `Assets/InputSystem_Actions.inputactions` (41KB) — Unity New Input System asset. Exists but not yet wired to any game actions.

### Archive (`Assets/_Archive/`)
- `BuildSmallBuilding.cs` — Editor script that generates SmallBuilding prefab (still referenced)
- `BuildPixelBuilding3.cs` — Editor script for complex pixel shop building (~25 child objects)
- `TutorialInfo/` — Standard Unity README template (Readme.cs, ReadmeEditor.cs, icons, layout)
- **Why archived**: Experimental/template code, not part of game systems

## Dependency Chain

```
GameManager (root orchestrator)
  ├── creates → MarketDataStore (singleton)
  ├── creates → DerivAPIService (singleton)
  │                └── creates → DerivWebSocket (per-connection)
  │                                  └── calls → DerivWebSocket.jslib (WebGL only)
  ├── uses → EventBus (static, no instance)
  └── references → GameState (enum)

MarketDataStore
  └── listens → EventBus.OnTickReceived

DerivAPIService
  ├── uses → DerivWebSocket (composition)
  ├── fires → EventBus.WebSocketConnected/Error, EventBus.TickReceived
  ├── fires → OnProposalReceived, OnBuyConfirmed, OnActiveSymbolsReceived (own events)
  └── calls → MarketDataStore.Instance.RegisterActiveSymbol()

EventBus (static hub)
  └── Trade class (nested, to be moved)
```

**No circular dependencies.** All communication between systems goes through EventBus or direct singleton access.

## Vercel Deployment Notes (for Phase 5)

Unity WebGL builds produce:
- `Build/` folder with `.data`, `.framework.js`, `.loader.js`, `.wasm` files
- `index.html` — entry point
- `TemplateData/` — loading screen assets

For Vercel:
- Deploy the entire `Build/` output as a static site
- Custom WebGL template goes in `Assets/_DerivTycoon/WebGLTemplates/DerivTycoon/`
- Must include `index.html` template with loading bar and `{{{ SCRIPT }}}` placeholder
- Target: <30MB gzipped total build size
- Enable Brotli compression in Player Settings for smallest builds

## Resources
- **Deriv API Docs**: https://developers.deriv.com/docs/
- **Deriv AI Hub**: https://developers.deriv.com/ai-hub/
- **Deriv llms.txt**: https://developers.deriv.com/llms.txt
- **Deriv API Context (local)**: `docs/deriv-llms-context.md` — saved API context with confirmed symbols, markets, submarkets
- **Deriv MCP Server**: Available for advanced AI integration
