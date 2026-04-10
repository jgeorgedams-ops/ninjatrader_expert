# BUILD INDICATOR PROMPT
# Use after START_SESSION. Fill in the [...] fields then paste.

---

Build the following NinjaScript indicator as a complete, production-quality .cs file.
Do not give me fragments. Give me the full file, ready to copy to the Indicators\ folder.

Indicator name: [e.g. FDAX_NewIndicator]
Purpose: [one sentence description]
Primary signal it detects: [e.g. liquidity sweeps, regime changes, VWAP bands]
Plots required: [list each plot — name, colour, style, what value it shows]
Parameters required: [list configurable inputs with default values]

Enforce all of the following without me asking:

STRUCTURE
- Inherits from Indicator
- Calculate = Calculate.OnBarClose (unless tick precision explicitly required)
- BarsRequiredToPlot set in State.DataLoaded, not SetDefaults
- All Series<T> objects initialised in State.DataLoaded
- All plots declared with AddPlot() in State.SetDefaults

SIGNAL EXPOSURE — CRITICAL
- Every signal the strategy layer needs must be written to a Values[n][0] plot
  as a numeric Series<double>, not just drawn as a chart object
- If the signal is directional, use: 1.0 = bullish, -1.0 = bearish, 0.0 = none
- If the signal is a score, write the raw score value to Values[n][0]
- Draw.Arrow / Draw.Text can be added for visual display but are secondary to Values[]

FDAX-SPECIFIC
- Never use TickSize for FDAX stop/target calculations inside indicators
- If drawing entry levels, use whole point offsets from price

CODE QUALITY
- if (CurrentBar < BarsRequiredToPlot) return; — always first line of OnBarUpdate
- try/catch around all core logic, Print() in catch with indicator name prefix
- _debugMode bool with verbose Print() output
- Clean up any event subscriptions in State.Terminated
- XML summary comment on the class

OUTPUT
- Complete .cs file
- Deploy path: C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\Indicators\
- After creating: tell me the copy path, F5 to compile, what to check in Log tab
- List every Values[n] index and what it contains so strategy code can reference it
