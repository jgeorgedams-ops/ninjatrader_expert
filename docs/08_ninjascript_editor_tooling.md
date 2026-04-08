# 08 — NinjaScript Editor & Tooling

**Editor overview:** https://developer.ninjatrader.com/docs/desktop/ninjascript_editor_overview  
**Legacy editor guide:** https://ninjatrader.com/support/helpguides/nt8/editor.htm

---

## The NinjaScript Editor

The NinjaScript Editor is the built-in IDE for writing and compiling NinjaScript. It is a basic C# code editor with NT8-specific integration.

**Open it:** Control Center → New → NinjaScript Editor, or **Ctrl+Alt+E**

### What It Does
- Syntax highlighting for C# / NinjaScript
- **Syntax and semantic checking** — errors listed at bottom of window
- **F5** compiles all scripts in the Custom folder
- Immediate effect: compiled scripts are live in NT8 without restart
- Access all built-in indicator source code for reading/learning

### What It Doesn't Do Well
- No IntelliSense/autocomplete (use Visual Studio for this)
- No breakpoint debugging (use `Print()` statements)
- No code navigation (no "Go to definition")
- Limited refactoring support

---

## Editor Layout

```
┌─────────────────────────────────────────────┐
│  File tree (left)    │  Code editor (right)  │
│  ├─ Indicators       │                       │
│  │   ├─ SampleSMA    │  [your code here]     │
│  │   └─ MyIndicator  │                       │
│  ├─ Strategies       │                       │
│  │   └─ MyStrategy   │                       │
│  └─ AddOns           │                       │
│                      │                       │
├──────────────────────┴───────────────────────┤
│  Error/Output panel (bottom)                  │
│  [Compiler errors and Print() output]         │
└───────────────────────────────────────────────┘
```

---

## Key Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| `F5` | Compile all scripts |
| `Ctrl+S` | Save current file |
| `Ctrl+Z` | Undo |
| `Ctrl+F` | Find in file |
| `Ctrl+H` | Find and replace |
| `Ctrl+Alt+E` | Open NinjaScript Editor |
| `Ctrl+Alt+O` | Open Output window (see Print() output) |

---

## Viewing Built-in Indicator Source Code

All NT8 built-in indicators ship with full source code — this is the most valuable learning resource available.

1. Open NinjaScript Editor
2. In the file tree, expand **Indicators**
3. Double-click any indicator (e.g. `SampleMACrossover`, `EMA`, `RSI`, `BollingerBands`)
4. Read and learn from production-quality NT8 code

Key files to study:
- `SampleMACrossover` — simplest complete strategy example (moving average crossover)
- `SampleSuperTrend` — more complete strategy with trailing stop and ATR
- `SampleAddOn` — basic add-on structure
- `EMA` — clean indicator implementation
- `BollingerBands` — multi-plot indicator with upper/lower bands

---

## Using Visual Studio for Development

Visual Studio provides IntelliSense, breakpoint debugging, refactoring, and proper code navigation. This is the recommended workflow for any serious NT8 development.

### Setup
1. Install Visual Studio (Community edition is free)
2. Create a new **Class Library (.NET Framework)** project
3. Add references to NT8 assemblies:
   ```
   C:\Program Files\NinjaTrader 8\bin\NinjaTrader.Client.dll
   C:\Program Files\NinjaTrader 8\bin\NinjaTrader.Core.dll
   C:\Program Files\NinjaTrader 8\bin\NinjaTrader.Custom.dll
   C:\Program Files\NinjaTrader 8\bin\NinjaTrader.Gui.dll
   ```
4. Set project output path to `Documents\NinjaTrader 8\bin\Custom\`
5. Build in VS → auto-deploys to NT8's Custom folder
6. Compile in NT8 editor (F5) to activate

### IntelliSense in VS
With NT8 assemblies referenced, you get:
- Full autocomplete for all NT8 methods and properties
- Parameter hints (method signatures)
- "Go to definition" for NT8 API methods
- Error squiggles as you type

### Debugging with Visual Studio
NT8 doesn't support attaching VS debugger in the normal way, but you can:
1. Use `Print()` statements (output to NT8 Output window)
2. Use the `NinjaTrader.UnitTest` framework (GitHub: samuelcaldas/NinjaTrader.UnitTest)
3. Write logic into testable pure C# methods, unit-test those separately

---

## Debugging Techniques

### Print() Debugging (Primary Method)
```csharp
// Basic print
Print("OnBarUpdate called for bar: " + CurrentBar);

// Formatted output with bar data
Print(string.Format("[{0}] Bar={1} O={2:F2} H={3:F2} L={4:F2} C={5:F2} V={6}",
    Name, CurrentBar, Open[0], High[0], Low[0], Close[0], Volume[0]));

// Print only in a time window (reduce noise)
if (CurrentBar > 100 && CurrentBar < 120)
    Print("Debug: EMA = " + EMA(Close, 20)[0]);

// Gate behind a debug flag
if (DebugMode) Print("Signal triggered at " + Time[0]);
```

**View output:** Control Center → **Log tab**, or **Ctrl+Alt+O** for the dedicated Output window.

### Structured Debugging Pattern
```csharp
private bool _debugMode = false;  // Set to true during dev, false for production

protected override void OnBarUpdate()
{
    try
    {
        if (CurrentBar < BarsRequiredToPlot) return;
        
        // Core logic
        double ema = EMA(Close, 20)[0];
        bool signal = Close[0] > ema && Close[1] <= EMA(Close, 20)[1];
        
        if (_debugMode)
        {
            Print(string.Format("Bar {0} | Close={1:F2} | EMA={2:F2} | Signal={3}",
                CurrentBar, Close[0], ema, signal));
        }
        
        if (signal)
            EnterLong(1, "LongEntry");
    }
    catch (Exception ex)
    {
        Print("EXCEPTION in " + Name + " at bar " + CurrentBar + ": " + ex.Message);
        Print("Stack: " + ex.StackTrace);
    }
}
```

### Common Errors and Fixes

| Error | Cause | Fix |
|---|---|---|
| `Index was outside the bounds` | Accessing `Close[N]` where N > CurrentBar | Guard with `if (CurrentBar < N) return;` |
| `Object reference not set` | Using a Series or object before initialising | Initialise in `State.DataLoaded`, not `SetDefaults` |
| `NaN` values | Indicator not warmed up yet | Check `BarsRequiredToPlot`, add `if (double.IsNaN(value)) return;` |
| Script compiles but does nothing | `OnBarUpdate` guard returning too early | Check `BarsRequiredToPlot` vs actual data length |
| Memory leak / ghost behaviour | Events not unsubscribed | Always unsubscribe in `State.Terminated` |
| Orders not filling in backtest | `BarsRequiredToTrade` too high | Check `State.DataLoaded` → `BarsRequiredToTrade` |

---

## Script Creation Wizard

For new files, use the wizard rather than writing from scratch:

1. NinjaScript Editor → **+** button → New Indicator (or Strategy, etc.)
2. Name your script, click Next/Generate
3. NT8 generates a boilerplate `.cs` with the correct class structure
4. Edit the generated file

---

## Compile Modes

Scripts can be in two states after compile:

| State | Meaning |
|---|---|
| ✅ Compiled successfully | Script is ready to use in NT8 |
| ❌ Compile errors | Script cannot run — fix errors in editor, recompile |

After fixing errors: **F5** to recompile. If errors persist in the panel but you've fixed the code, try **Edit → Reload** or close/reopen the editor.

---

## Editor Reference Links

| Resource | URL |
|---|---|
| NinjaScript editor overview | https://developer.ninjatrader.com/docs/desktop/ninjascript_editor_overview |
| Editor help guide (legacy) | https://ninjatrader.com/support/helpguides/nt8/editor.htm |
| NinjaScript best practices | https://developer.ninjatrader.com/docs/desktop/ninjascript_best_practices |
| Unit testing framework (GitHub) | https://github.com/samuelcaldas/NinjaTrader.UnitTest |
