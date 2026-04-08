# 05 — Automated Trading Interface (ATI)

**ATI overview:** https://ninjatrader.com/support/helpguides/nt8/automated_trading_interface_at.htm  
**Automated trading overview:** https://ninjatrader.com/support/helpguides/nt8/automated_trading.htm  
**Commands and parameters:** https://ninjatrader.com/support/helpguides/nt8/commands_and_valid_parameters.htm  
**File interface:** https://ninjatrader.com/support/helpguides/nt8/file_interface.htm  
**DLL interface:** https://ninjatrader.com/support/helpguides/nt8/dll_interface.htm  
**DLL functions reference:** https://ninjatrader.com/support/helpguides/nt8/functions.htm  
**ATI initialisation:** https://ninjatrader.com/support/helpguides/nt8/initialization.htm  
**ATI settings:** https://ninjatrader.com/support/helpguides/nt8/options_ati.htm

---

## What the ATI Is

The Automated Trading Interface (ATI) lets **external applications** send orders to NinjaTrader without writing NinjaScript. This is the bridge if you want to:

- Drive NT8 orders from **Python**, R, Excel, or any other language
- Connect a third-party signal service to NT8 execution
- Build an external dashboard that controls live trading
- Integrate with Zorro, MetaTrader, or other platforms

There are two ATI interfaces:

| Interface | How It Works | Best For |
|---|---|---|
| **File Interface** | Write a text file to a watched folder; NT8 reads and executes commands | Simple integrations, any language that can write files |
| **DLL Interface** | Load `NTDirect.dll` and call functions directly | Low-latency C/C++, .NET, Python (via pythonnet) integrations |

---

## Enable the ATI

1. NT8 Control Center → Tools → Options
2. Select **Automated trading interface** category
3. Set **AT interface** to Enabled
4. Set **Default account** (the account ATI commands will use if no account specified)

---

## The Eight ATI Commands

These commands work on both File and DLL interfaces:

| Command | Purpose |
|---|---|
| `PLACE` | Place a new order |
| `CANCEL` | Cancel an existing order |
| `CHANGE` | Modify an existing order (price, quantity) |
| `CLOSEPOSITION` | Close/flatten a position |
| `REVERSEPOSITION` | Reverse position direction |
| `CLOSESTRATEGY` | Close/terminate a running ATM strategy |
| `FLATTEN` | Flatten all positions for an account |
| `FLATTENEVERYTHING` | Flatten all positions on all accounts |

---

## File Interface

NT8 watches a folder for `.txt` command files. When a file appears, NT8 reads it, executes the command, and deletes the file.

### Default Watch Folder
```
C:\Program Files\NinjaTrader 8\bin\Custom\
```
Configure in Options → ATI.

### Command File Format
Each line is one parameter: `key=value`

### PLACE Command Example
```
PLACE
account=Sim101
instrument=ES 03-25
action=BUY
quantity=1
ordertype=MARKET
tif=DAY
strategy=
```

### PLACE with ATM Strategy
```
PLACE
account=Sim101
instrument=MES 03-25
action=BUY
quantity=1
ordertype=MARKET
tif=DAY
strategy=MyATM
```

### CLOSEPOSITION Example
```
CLOSEPOSITION
account=Sim101
instrument=ES 03-25
```

### FLATTEN Example
```
FLATTEN
account=Sim101
```

### Valid Parameters for PLACE

| Parameter | Values | Notes |
|---|---|---|
| `account` | Account name | Leave blank for default |
| `instrument` | e.g. `ES 03-25` | Full instrument name |
| `action` | `BUY`, `SELL`, `BUY_TO_COVER`, `SELL_SHORT` | |
| `quantity` | Integer | Number of contracts |
| `ordertype` | `MARKET`, `LIMIT`, `STOP`, `STOPLIMIT` | |
| `limitprice` | Decimal | Required for LIMIT/STOPLIMIT |
| `stopprice` | Decimal | Required for STOP/STOPLIMIT |
| `tif` | `DAY`, `GTC`, `GTD` | Time in force |
| `strategy` | ATM strategy name | Optional — activates ATM brackets |
| `orderId` | String | Optional ID for tracking/cancelling |

---

## Python → File Interface Example

```python
import os, time

ATI_FOLDER = r"C:\Program Files\NinjaTrader 8\bin\Custom"

def place_order(account, instrument, action, quantity, order_type="MARKET"):
    command = f"""PLACE
account={account}
instrument={instrument}
action={action}
quantity={quantity}
ordertype={order_type}
tif=DAY
"""
    filepath = os.path.join(ATI_FOLDER, f"order_{int(time.time())}.txt")
    with open(filepath, "w") as f:
        f.write(command)
    print(f"Order file written: {filepath}")

# Example usage
place_order("Sim101", "MES 03-25", "BUY", 1)
time.sleep(1)  # Give NT8 time to process
place_order("Sim101", "MES 03-25", "SELL", 1)  # Close
```

---

## DLL Interface

Load `NTDirect.dll` (32-bit or 64-bit) from the NinjaTrader installation directory and call its functions.

**DLL Paths:**
```
32-bit: C:\Program Files (x86)\NinjaTrader 8\bin\NTDirect.dll  (if installed)
64-bit: C:\Program Files\NinjaTrader 8\bin\NTDirect.dll
```

### DLL Functions Reference

#### Connection
```
int Connected(int showMessage)
// Returns 0 if connected and ATI enabled, -1 if not
// Call first before any other function
```

#### Account & Position Queries
```
double AvgEntryPrice(string instrument, string account)
int    GetPosition(string instrument, string account)
       // Returns 0=flat, positive=long, negative=short
double GetRealizedPnL(string account)
```

#### Order Execution
```
int Command(string command, string account, string instrument, string action,
            int quantity, string orderType, double limitPrice, double stopPrice,
            string tif, string oCO, string orderId, string strategy,
            string strategyId)
// command: "PLACE", "CANCEL", "CHANGE", "CLOSEPOSITION", etc.
// Returns 0 on success, -1 on error
```

#### Market Data Injection (Playback Sync)
```
int Last(string instrument, double price, int size)
int Bid(string instrument, double price, int size)
int Ask(string instrument, double price, int size)
int BidPlayback(string instrument, double price, int size, string timestamp)
int AskPlayback(string instrument, double price, int size, string timestamp)
// timestamp format: "yyyyMMddHHmmss"
```

#### ATM Strategy Queries
```
string GetAtmStrategyIds(string account)
       // Returns strategy IDs separated by "|"
int    GetAtmStrategyPositionQuantity(string strategyId)
double GetAtmStrategyPositionAveragePrice(string strategyId)
int    GetAtmStrategyRealizedPnL(string strategyId)
int    CloseAtmStrategyPosition(string strategyId)
```

---

## Python → DLL Interface (via pythonnet)

```python
import clr
clr.AddReference(r"C:\Program Files\NinjaTrader 8\bin\NTDirect.dll")
from NinjaTrader.Ati import Command as NTCommand

# Check connection
# Note: pythonnet DLL loading has limitations — File Interface is generally
# more reliable from Python. CrossTrade REST API is the modern alternative.
```

> **Practical note for Python users:** The File Interface is more reliable from Python than DLL loading via pythonnet. For modern Python-to-NT8 integration, the **CrossTrade REST API** (`docs/06_rest_websocket_api.md`) is significantly easier and more capable.

---

## ATI Initialisation Notes

- ATI binds to the **first account name** used in the first calling function
- If account parameter is left blank, the **Default account** (set in Options → ATI) is used
- The connection is established automatically on the first DLL function call
- For File Interface: NT8 must be running and the ATI must be enabled

---

## ATI Reference Links

| Resource | URL |
|---|---|
| ATI overview | https://ninjatrader.com/support/helpguides/nt8/automated_trading_interface_at.htm |
| Automated trading overview | https://ninjatrader.com/support/helpguides/nt8/automated_trading.htm |
| Commands and parameters | https://ninjatrader.com/support/helpguides/nt8/commands_and_valid_parameters.htm |
| File interface | https://ninjatrader.com/support/helpguides/nt8/file_interface.htm |
| DLL interface | https://ninjatrader.com/support/helpguides/nt8/dll_interface.htm |
| DLL functions reference | https://ninjatrader.com/support/helpguides/nt8/functions.htm |
| ATI initialisation | https://ninjatrader.com/support/helpguides/nt8/initialization.htm |
| ATI settings/config | https://ninjatrader.com/support/helpguides/nt8/options_ati.htm |
