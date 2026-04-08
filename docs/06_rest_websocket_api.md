# 06 — REST API & WebSocket API

**Trader APIs overview:** https://developer.ninjatrader.com/products/api  
**WebSocket API docs:** https://developer.ninjatrader.com/docs/api/websocket/  
**Tradovate REST API docs:** https://developer.ninjatrader.com/docs/api  
**Ecosystem API (vendor licensing):** https://developer.ninjatrader.com/docs/ecosystem  
**Web SDK:** https://developer.ninjatrader.com/docs/web/

---

## Overview: Three API Layers

NinjaTrader exposes three distinct API surfaces:

| API | Purpose | Who Uses It |
|---|---|---|
| **Tradovate REST API** | Full brokerage API — accounts, orders, positions, market data | External apps, Python bots, cloud strategies |
| **WebSocket API** | Real-time streaming market data and account updates | Low-latency data consumers |
| **Ecosystem API** | Vendor product/license management | Third-party indicator/strategy vendors |

There is also the **CrossTrade REST API** (third-party, NT8 ecosystem) that wraps the ATI into a REST interface — see the third-party section below.

---

## Tradovate REST API

The Tradovate API is the primary programmatic interface for accounts, orders and market data when connected through NinjaTrader's Tradovate brokerage integration.

**Base URL:** `https://live.tradovateapi.com/v1/` (live) / `https://demo.tradovateapi.com/v1/` (demo)  
**Auth:** OAuth 2.0 — obtain an access token, pass as Bearer in all requests  
**Format:** JSON request/response  
**Swagger definition:** Available — generate client code in any language

### Authentication Flow
```python
import requests

# 1. Get access token
auth_url = "https://demo.tradovateapi.com/v1/auth/accesstokenrequest"
payload = {
    "name": "YOUR_USERNAME",
    "password": "YOUR_PASSWORD",
    "appId": "Sample App",
    "appVersion": "1.0",
    "cid": YOUR_CID,
    "sec": "YOUR_SECRET",
    "deviceId": "device-id-string"
}
resp = requests.post(auth_url, json=payload)
token = resp.json()["accessToken"]
headers = {"Authorization": f"Bearer {token}"}
```

### Key Endpoints

#### Account & Position
```python
base = "https://demo.tradovateapi.com/v1"

# Get all accounts
accounts = requests.get(f"{base}/account/list", headers=headers).json()

# Get positions
positions = requests.get(f"{base}/position/list", headers=headers).json()

# Get orders
orders = requests.get(f"{base}/order/list", headers=headers).json()

# Get executions (fills)
fills = requests.get(f"{base}/execution/list", headers=headers).json()
```

#### Place an Order
```python
order_payload = {
    "accountSpec": "YOUR_ACCOUNT_NAME",
    "accountId": 12345,
    "action": "Buy",               # or "Sell"
    "symbol": "MESU4",             # instrument symbol
    "orderQty": 1,
    "orderType": "Market",         # Market, Limit, Stop, StopLimit
    "isAutomated": True
}
order = requests.post(f"{base}/order/placeorder", json=order_payload, headers=headers).json()
```

#### Cancel an Order
```python
requests.post(f"{base}/order/cancelorder", json={"orderId": order_id}, headers=headers)
```

#### Market Data (Historical)
```python
# Get historical bars
bars_payload = {
    "symbol": "MESU4",
    "unitOfTime": "Minute",
    "unitNumber": 5,
    "startTime": "2024-01-02T09:30:00Z",
    "endTime": "2024-01-02T16:00:00Z",
    "useHighWaterMark": False
}
bars = requests.post(f"{base}/md/getChart", json=bars_payload, headers=headers).json()
```

### Contract / Instrument Lookup
```python
# Search for a contract
contracts = requests.get(f"{base}/contract/find?name=ES", headers=headers).json()

# Get full contract spec
spec = requests.get(f"{base}/contractMaturity/item?id={contract_id}", headers=headers).json()
```

---

## WebSocket API — Real-Time Streaming

The WebSocket API provides real-time streaming for market data, account updates, and order fills via high-performance persistent connections.

**Endpoint:** `wss://live.tradovateapi.com/v1/websocket` (live) / `wss://demo.tradovateapi.com/v1/websocket` (demo)

### Connection & Auth
```python
import websockets
import asyncio
import json

async def connect():
    uri = "wss://demo.tradovateapi.com/v1/websocket"
    
    async with websockets.connect(uri) as ws:
        # Authenticate immediately after connect
        auth_msg = {
            "url": "auth/accesstokenrequest",
            "body": {
                "name": "YOUR_USERNAME",
                "password": "YOUR_PASSWORD",
                "appId": "Sample App",
                "appVersion": "1.0",
                "cid": YOUR_CID,
                "sec": "YOUR_SECRET"
            }
        }
        await ws.send(json.dumps(auth_msg))
        
        # Listen for messages
        while True:
            msg = await ws.recv()
            data = json.loads(msg)
            print(data)

asyncio.run(connect())
```

### Subscribe to Market Data
```python
# After auth, subscribe to real-time quotes
subscribe_msg = {
    "url": "md/subscribeQuote",
    "body": {"symbol": "MESU4"}
}
await ws.send(json.dumps(subscribe_msg))

# Subscribe to order book (DOM)
dom_msg = {
    "url": "md/subscribeDOM",
    "body": {"symbol": "MESU4"}
}
await ws.send(json.dumps(dom_msg))
```

### Subscribe to Account Updates
```python
# Subscribe to order and position updates
await ws.send(json.dumps({
    "url": "account/subscribeUpdates",
    "body": {"accountId": 12345}
}))
```

### Message Types Received
- `{"e": "md", ...}` — market data update (quote, trade)
- `{"e": "dom", ...}` — order book update
- `{"e": "order", ...}` — order state change
- `{"e": "fill", ...}` — execution/fill
- `{"e": "position", ...}` — position update
- `{"e": "cashBalance", ...}` — account balance update

---

## Web SDK

The Web SDK is for building **web-based** tools and applications that connect to NinjaTrader data and trading infrastructure.

**Documentation:** https://developer.ninjatrader.com/docs/web/

Primarily relevant for building browser-based dashboards, web analytics tools, or custom trading UIs that connect to NT8 data.

---

## Ecosystem API (Vendor Licensing)

For third-party vendors who sell NinjaScript indicators or strategies. Manages product licensing, user accounts, and activation.

**Documentation:** https://developer.ninjatrader.com/docs/ecosystem

**Auth:** Bearer token (expires every 90 minutes)  
**Get token:** Login to NT8 Ecosystem vendor portal → F12 → Network tab → find `/me` endpoint → copy Authorization header value

Not needed unless you're selling commercial NinjaScript products.

---

## CrossTrade REST API (Third-Party, Recommended for Python)

CrossTrade is an NT8 ecosystem vendor that wraps NT8's desktop platform into a REST API accessible from any language.

**Docs:** https://docs.crosstrade.io/api/overview  
**Overview:** https://crosstrade.io/crosstrade-api

This is **significantly easier** than the ATI DLL interface for Python-based algo trading. Install the CrossTrade NT8 Add-On; while it's connected, your NT8 desktop becomes an HTTP-accessible trading engine.

### What It Provides
- Place, cancel, modify orders via HTTP POST
- Query accounts, positions, orders, executions, P&L
- Real-time WebSocket streaming for positions and market state
- Works from Python, JavaScript, Go, or anything with HTTP

### Example (Python)
```python
import requests

BASE = "https://api.crosstrade.io"
API_KEY = "your-api-key"
headers = {"Authorization": f"Bearer {API_KEY}"}

# Get account snapshot
account = requests.get(f"{BASE}/account", headers=headers).json()

# Get open positions
positions = requests.get(f"{BASE}/positions", headers=headers).json()

# Place market order
order = requests.post(f"{BASE}/orders", headers=headers, json={
    "instrument": "MES 03-25",
    "action": "BUY",
    "quantity": 1,
    "orderType": "MARKET"
}).json()

# Get recent executions
fills = requests.get(f"{BASE}/executions", headers=headers).json()
```

---

## API Reference Links

| Resource | URL |
|---|---|
| Trader APIs overview | https://developer.ninjatrader.com/products/api |
| WebSocket API docs | https://developer.ninjatrader.com/docs/api/websocket/ |
| Tradovate REST API docs | https://developer.ninjatrader.com/docs/api |
| Ecosystem API | https://developer.ninjatrader.com/docs/ecosystem |
| Web SDK | https://developer.ninjatrader.com/docs/web/ |
| CrossTrade API docs | https://docs.crosstrade.io/api/overview |
