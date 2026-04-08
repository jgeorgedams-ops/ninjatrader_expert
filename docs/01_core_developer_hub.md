# 01 — Core Developer Hub

**Official source:** https://developer.ninjatrader.com/docs/desktop  
**Getting started guide:** https://ninjatrader.com/support/helpguides/nt8/getting_started_operations.htm  
**Best practices:** https://developer.ninjatrader.com/docs/desktop/ninjascript_best_practices

---

## What NinjaScript Is

NinjaScript is NinjaTrader's scripting framework — a strict superset of C# built on .NET. Every script is a C# class that inherits from an NT8 base class. You get full access to the .NET standard library plus NT8's trading API.

The four main script types:

| Type | Base Class | Purpose |
|---|---|---|
| Indicator | `Indicator` | Custom chart calculations and plots |
| Strategy | `Strategy` | Automated order execution |
| Add-on | `AddOnBase` | Platform extensions, custom windows |
| Drawing Tool | `DrawingTool` | Custom chart drawings |

Secondary types: `BarsType`, `ChartStyle`, `MarketAnalyzerColumn`, `Optimizer`, `OptimizationFitness`, `PerformanceMetrics`, `SuperDOMColumn`, `ShareService`.

---

## Architecture Overview

```
NinjaTrader 8 Platform
│
├── NinjaScript Engine (.NET runtime)
│   ├── Your indicators (.cs files)
│   ├── Your strategies (.cs files)
│   └── Your add-ons (.cs files)
│
├── Data Layer
│   ├── Real-time market data (broker connection)
│   ├── Historical data (downloaded, stored locally)
│   └── Playback / Replay connection
│
├── Execution Layer
│   ├── Managed orders (automatic lifecycle)
│   ├── Unmanaged orders (manual lifecycle)
│   └── ATI (external automation)
│
└── UI Layer
    ├── Charts (SharpDX rendering)
    ├── Strategy Analyzer (backtesting)
    ├── SuperDOM (order ladder)
    └── Market Analyzer (scanner)
```

---

## The State Machine — Core Concept

Every NinjaScript object progresses through a fixed sequence of states. Understanding this is essential:

```
SetDefaults → Configure → DataLoaded → Historical → Realtime → Terminated
```

| State | When | What to do here |
|---|---|---|
| `SetDefaults` | First call, before any user sees it | Set `Name`, `Description`, default parameter values, `Calculate` mode |
| `Configure` | After defaults, before data loads | Add extra data series (`AddDataSeries`), set `BarsRequiredToPlot` |
| `DataLoaded` | Historical data available | Initialise lists, collections, any objects needing bar data |
| `Historical` | Processing historical bars | Runs `OnBarUpdate` — backtest period |
| `Realtime` | Connected, live data flowing | Runs `OnBarUpdate` — live execution |
| `Terminated` | Script stopped/removed | **Unsubscribe all events. Dispose timers. Release resources.** |

```csharp
protected override void OnStateChange()
{
    if (State == State.SetDefaults)
    {
        Name        = "MyIndicator";
        Description = "What this does";
        Calculate   = Calculate.OnBarClose;
        IsOverlay   = false;           // false = subgraph, true = price pane
        AddPlot(Brushes.DodgerBlue, "Signal");
    }
    else if (State == State.Configure)
    {
        // Optional: add a secondary data series
        // AddDataSeries(Data.BarsPeriodType.Minute, 5);
    }
    else if (State == State.DataLoaded)
    {
        BarsRequiredToPlot = 20;
    }
    else if (State == State.Terminated)
    {
        // Clean up — very important for add-ons and event subscribers
    }
}
```

---

## Calculate Modes

Controls when `OnBarUpdate` is called:

| Mode | Fires | Use When |
|---|---|---|
| `Calculate.OnBarClose` | Once per completed bar | Most indicators and strategies — best performance |
| `Calculate.OnEachTick` | Every tick (every price update) | Tick-based logic, precise entry/exit |
| `Calculate.OnPriceChange` | Only when price changes | Between tick and bar close — reduced overhead vs OnEachTick |

Set in `State.SetDefaults`:
```csharp
Calculate = Calculate.OnBarClose;
```

---

## NinjaScript Best Practices

Direct from NT8 official documentation:

### Performance
- Prefer `Calculate.OnBarClose` — `OnEachTick` can fire hundreds of times per second
- Cache indicator values in local variables rather than re-calling `EMA(Close, 20)[0]` multiple times per bar
- Avoid LINQ inside `OnBarUpdate` — allocates heap memory on every call
- Never use `Thread.Sleep()` or blocking I/O inside `OnBarUpdate`

### Reliability
- Always guard with `if (CurrentBar < BarsRequiredToPlot) return;`
- Wrap core logic in `try/catch` with `Print()` in the catch block
- Always clean up in `State.Terminated` — event handlers and timers that aren't unsubscribed will cause memory leaks and ghost behaviour

### Code Quality
- Use `private` fields for state; use `[NinjaScriptProperty]` attribute for user-configurable parameters
- Name entry/exit signals consistently — the name string is used to match orders
- Use `Print()` extensively during development; remove or gate behind a `DebugMode` bool for production

### Debugging Pattern
```csharp
private bool _debugMode = true;

protected override void OnBarUpdate()
{
    try
    {
        if (CurrentBar < BarsRequiredToPlot) return;

        // your logic

        if (_debugMode)
            Print(string.Format("[{0}] Bar={1} Close={2:F2}", Name, CurrentBar, Close[0]));
    }
    catch (Exception ex)
    {
        Print("ERROR in " + Name + ": " + ex.Message);
    }
}
```

---

## File Locations

| Purpose | Path |
|---|---|
| Indicators source | `Documents\NinjaTrader 8\bin\Custom\Indicators\` |
| Strategies source | `Documents\NinjaTrader 8\bin\Custom\Strategies\` |
| Add-ons source | `Documents\NinjaTrader 8\bin\Custom\AddOns\` |
| Built-in samples | `Documents\NinjaTrader 8\bin\Custom\Samples\` |
| Compiled output | `Documents\NinjaTrader 8\bin\Custom\` (auto) |
| Market data | `Documents\NinjaTrader 8\db\` |
| Workspace files | `Documents\NinjaTrader 8\workspaces\` |

---

## Compile & Deploy Workflow

1. Write or edit `.cs` file in the correct Custom subfolder
2. Open NT8 → NinjaScript Editor (Ctrl+Alt+E or New → NinjaScript Editor)
3. Press **F5** to compile
4. Check Control Center → **Log tab** for compiler errors
5. Errors also appear in NinjaScript Editor bottom panel
6. For indicators: add to chart via right-click → Indicators
7. For strategies: add to chart via right-click → Strategies, or run from Strategy Analyzer

---

## All Official Documentation Sections

| Section | URL |
|---|---|
| Desktop SDK index | https://developer.ninjatrader.com/docs/desktop |
| Best practices | https://developer.ninjatrader.com/docs/desktop/ninjascript_best_practices |
| Educational resources (all tutorials) | https://developer.ninjatrader.com/docs/desktop/educational_resources |
| Distribution guide | https://developer.ninjatrader.com/docs/desktop/distribution |
| User-based vendor licensing | https://developer.ninjatrader.com/docs/desktop/user_based_licensing |
| Developer community home | https://developer.ninjatrader.com/ |
| Getting started (help guide) | https://ninjatrader.com/support/helpguides/nt8/getting_started_operations.htm |
