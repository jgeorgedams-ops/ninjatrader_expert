# CLAUDE.md — NinjaTrader 8 Development Brain

> This file is the primary context file for Claude Code. Read this first before touching any file in this project.

---

## READ THESE FIRST — every session, no exceptions

Before touching any file or writing any code, read all brain files in order:

```
brain/00_WHO_I_AM.md
brain/01_SYSTEM_MAP.md
brain/02_SESSIONS_AND_INSTRUMENTS.md
brain/03_STRATEGY_RULES.md
brain/04_RISK_RULES.md
brain/05_BUILD_LOG.md
brain/06_DECISIONS.md
```

These files contain the trader profile, full system inventory, strategy rules,
risk parameters, build queue, and all architecture decisions. They are the
source of truth. Everything below is NinjaScript technical reference.

---

## What This Project Is

This is a NinjaTrader 8 (NT8) algorithmic futures trading development workspace
for an Australian retail futures trader transitioning from discretionary to fully
automated trading. It contains custom NinjaScript indicators, automated strategies,
API reference documentation, code templates, test configurations, and the brain
files that give Claude Code full context every session.

**Platform:** NinjaTrader 8 (NT8)
**Language:** NinjaScript — C# extension built on .NET Framework
**Primary instruments:** FDAX (European session), NQ (US session), Asian TBD
**Regulatory context:** Australian retail trader — ASIC oversight, trading income

---

## Folder Map

```
/brain           → Context files — read every session before writing code
/prompts         → Pre-built prompts for Claude Code and Claude.ai sessions
/indicators      → Custom .cs indicator files
                   deploy to: C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\Indicators\
/strategies      → Automated strategy .cs files
                   deploy to: C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\Strategies\
/docs            → Reference documents — 01 through 09
/templates       → Boilerplate .cs starter files — copy and rename before editing
/tests           → Strategy Analyzer replay configs, backtest notes, walk-forward records
CLAUDE.md        → This file
README.md        → Human-readable project overview
```

---

## NinjaScript Fundamentals

### Language
- NinjaScript is C# with NT8-specific base classes. All standard C# and .NET libraries available.
- Scripts compile inside NT8's NinjaScript Editor (Ctrl+Alt+E) or Visual Studio.
- Every script inherits from a base class: `Indicator`, `Strategy`, `AddOnBase`, `DrawingTool`.

### The Two Core Methods

```csharp
protected override void OnStateChange()
{
    // Called ONCE per state: SetDefaults → Configure → DataLoaded → Historical → Realtime → Terminated
    if (State == State.SetDefaults) { Name = "MyScript"; }
    if (State == State.Configure)   { AddDataSeries(Data.BarsPeriodType.Minute, 5); }
    if (State == State.DataLoaded)  { /* initialise objects, set BarsRequiredToPlot */ }
    if (State == State.Terminated)  { /* unsubscribe events, dispose timers — critical */ }
}

protected override void OnBarUpdate()
{
    if (CurrentBar < BarsRequiredToPlot) return;  // ALWAYS guard first
    try
    {
        // all calculation and signal logic here
    }
    catch (Exception ex) { Print("ERROR: " + ex.Message); }
}
```

### Critical Index Convention
- `Close[0]` = current bar, `Close[1]` = 1 bar ago, `Close[2]` = 2 bars ago
- `High[0]`, `Low[0]`, `Open[0]`, `Volume[0]` — same convention
- Never access `Close[-1]` — future bar, will throw

### State Machine
```
SetDefaults → Configure → DataLoaded → Historical → Realtime → Terminated
```

### Managed vs Unmanaged Orders
| Mode | Use When |
|---|---|
| Managed (default) | Standard strategies — NT8 handles lifecycle automatically |
| Unmanaged | Only if managed mode genuinely cannot achieve the goal |

---

## FDAX-Specific Rules — Never Violate These

```
Point value:  €25.00 per point
Tick size:    0.5 points — but ALL stop/target logic uses POINTS, never ticks
Stop loss:    15 points (€375/contract) — hardcoded, never a parameter
Target 1:     5 points  (€125/contract)
Target 2:     10 points (€250/contract)
Max contracts: 1 — until 50+ trade sample is proven
Session:      07:00–17:30 CET — enforced via ToTime() filter
```

**If you ever see CalculationMode.Ticks used for FDAX stop/target — it is wrong. Fix it.**

---

## Indicator-to-Strategy Reading Pattern

Indicators must be declared at class level and instantiated in State.DataLoaded.
Never instantiate inside OnBarUpdate. Never call the indicator constructor repeatedly.

```csharp
// Class level
private FDAX_RegimeDetector   _regime;
private FDAX_LiquiditySweep   _sweep;
private FDAX_SessionVWAP      _vwap;
private FDAX_ReversalScorer   _reversal;

// State.DataLoaded
_regime   = FDAX_RegimeDetector(/* params */);
_sweep    = FDAX_LiquiditySweep(/* params */);
_vwap     = FDAX_SessionVWAP(/* params */);
_reversal = FDAX_ReversalScorer(/* params */);

// OnBarUpdate — read once into locals
int    regime        = (int)_regime.Values[0][0];
double sweepScore    = _sweep.Values[0][0];
double sweepDir      = _sweep.Values[1][0];
double vwap          = _vwap.Values[0][0];
double reversalScore = _reversal.Values[0][0];
```

---

## Key NT8 APIs — Quick Reference

### Built-in Indicators
```csharp
EMA(Close, 20)[0]
SMA(Close, 50)[0]
RSI(Close, 14, 3)[0]
ATR(14)[0]
MACD(Close,12,26,9).Value[0]
BollingerBands(Close,14,2).Upper[0]
VOL()[0]
```

### Order Entry (managed mode)
```csharp
EnterLong(quantity, "EntrySignalName");
EnterShort(quantity, "EntrySignalName");
ExitLong("ExitSignalName", "EntrySignalName");
ExitShort("ExitSignalName", "EntrySignalName");
EnterLongLimit(quantity, limitPrice, "LimitEntry");
EnterShortLimit(quantity, limitPrice, "LimitEntry");

// FDAX — use CalculationMode.Currency or .Price, never .Ticks
SetStopLoss("EntryName", CalculationMode.Price, entryPrice - 15.0, false);
SetProfitTarget("EntryName", CalculationMode.Price, entryPrice + 5.0);
```

### Drawing on Charts
```csharp
Draw.Line(this, "tag", false, 10, High[10], 0, High[0], Brushes.Blue, DashStyleHelper.Solid, 2);
Draw.ArrowUp(this, "arrow"+CurrentBar, true, 0, Low[0]-2*TickSize, Brushes.Green);
Draw.Text(this, "lbl", "Signal", 0, High[0]+2*TickSize);
Draw.HorizontalLine(this, "hline", Close[0], Brushes.Gray);
```

### Time Filtering
```csharp
int now = ToTime(Time[0]);
// Session filter: 07:00–17:30 CET = approximately 06:00–16:30 UTC
// Use ToTime(Time[0]) against integer times: 70000 = 07:00:00
if (now < 70000 || now > 173000) return;
```

### Printing / Debugging
```csharp
Print("Value: " + Close[0]);
Print(string.Format("Bar {0}: O={1:F1} H={2:F1} L={3:F1} C={4:F1}",
    CurrentBar, Open[0], High[0], Low[0], Close[0]));
// Output: NinjaScript Output window (Ctrl+Alt+O)
```

---

## Coding Rules — Enforce Without Being Asked

1. Always guard `OnBarUpdate` with `if (CurrentBar < BarsRequiredToPlot) return;`
2. Always wrap core logic in `try/catch` with `Print()` in catch
3. Never use blocking calls inside `OnBarUpdate` — no Thread.Sleep, no synchronous HTTP
4. Always clean up in `State.Terminated` — unsubscribe events, dispose timers
5. Managed mode only — never switch to unmanaged without explicit instruction
6. Calculate.OnBarClose always — never OnEachTick unless explicitly required
7. FDAX stops and targets in points (CalculationMode.Price), never ticks
8. Indicators declared at class level, instantiated in State.DataLoaded only
9. Multi-timeframe data: add series in State.Configure, access via BarsArray[n]
10. All configurable params as [NinjaScriptProperty], all safety rails as private const

---

## Deploy Paths (Windows — OneDrive path)

```
Indicators:  C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\Indicators\
Strategies:  C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\Strategies\
Add-ons:     C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\AddOns\
```

After copying: NinjaScript Editor → F5 to compile. Check Control Center → Log tab for errors.

---

## Documentation Index

| File | Topic |
|---|---|
| `docs/01_core_developer_hub.md` | Platform overview, architecture, state machine |
| `docs/02_indicator_development.md` | Custom indicators, plots, MTF, drawing |
| `docs/03_strategy_development_backtesting.md` | Strategies, orders, Strategy Analyzer |
| `docs/04_ninjascript_language_reference.md` | C#/NinjaScript language reference |
| `docs/05_automated_trading_interface.md` | ATI — external order automation |
| `docs/06_rest_websocket_api.md` | REST API, WebSocket, Tradovate |
| `docs/07_addon_platform_extension.md` | Add-ons, custom windows, distribution |
| `docs/08_ninjascript_editor_tooling.md` | Editor, debugging, Visual Studio |
| `docs/09_community_support_appshare.md` | Forums, app share, GitHub resources |

---

## Australian Context

- Tax: futures gains taxed as ordinary income for active traders. Keep full trade logs.
- Brokers (NT8-compatible, AU-accessible): Rithmic via TopStep/Apex, IBKR, Tradovate
- NT8 custom files are on OneDrive path — always use the OneDrive path above, not standard Documents
- PowerShell: use semicolons (`;`) not `&&` for command chaining
- AEST = UTC+10 (UTC+11 during daylight saving)
