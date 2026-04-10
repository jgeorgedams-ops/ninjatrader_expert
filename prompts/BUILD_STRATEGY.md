# BUILD STRATEGY PROMPT
# Use after START_SESSION. Fill in the [...] fields then paste.

---

Build the following NinjaScript strategy as a complete, production-quality .cs file.
Do not give me fragments. Give me the full file, ready to copy to the Strategies\ folder.

Strategy name: [e.g. FDAX_SweepFadeStrategy]
Signal source: [e.g. FDAX_LiquiditySweep Good/Strong + FDAX_RegimeDetector Auction]
Entry direction logic: [e.g. Long on bull sweep, Short on bear sweep]
Entry order type: [Limit at close of signal candle / Market / StopLimit]

Enforce all of the following without me asking — these are non-negotiable:

STRUCTURE
- Single class inheriting from Strategy
- State machine enum: StrategyState { Flat, WaitingForSignal, SignalConfirmed, InTrade, CoolingDown }
- All indicators declared at class level, instantiated in State.DataLoaded only
- Calculate = Calculate.OnBarClose
- Managed orders only

FDAX RISK (hardcoded consts — never parameters)
- StopPoints = 15.0 — CalculationMode.Price, not ticks
- Target1Points = 5.0 — exit 50% of position
- Target2Points = 10.0 — exit remaining 50%
- MaxContracts = 1
- CooldownBars = 6

FILTERS (all must be checked at top of OnBarUpdate before any signal logic)
- if (CurrentBar < BarsRequiredToTrade) return;
- Session filter: ToTime(Time[0]) between 70000 and 173000 (07:00–17:30 CET)
- if (_haltedForDay) return;
- if (_dailyTradeCount >= MaxDailyTrades) return;
- if (_cooldownBarsRemaining > 0) { _cooldownBarsRemaining--; return; }
- Weekend flat: Friday >= 154500 UTC — force ExitLong/ExitShort if in position

DAILY TRACKING
- _dailyRealizedPnL reset on Bars.IsFirstBarOfSession
- _haltedForDay reset on Bars.IsFirstBarOfSession
- _dailyTradeCount reset on Bars.IsFirstBarOfSession
- Halt triggered when _dailyRealizedPnL <= -DailyLossLimitEuro

CONFIGURABLE PARAMETERS ([NinjaScriptProperty])
- DailyLossLimitEuro = 750.0
- MaxDailyTrades = 6
- EnableStrategy = true (master on/off switch)
- [Add any strategy-specific params here]

CODE QUALITY
- try/catch around all core OnBarUpdate logic, Print() in catch
- _debugMode bool — verbose Print() output when true, silent when false
- Clean up all indicator references in State.Terminated
- XML summary comment on the class describing the strategy in two sentences

OUTPUT
- Complete .cs file
- Deploy path: C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\Strategies\
- After creating the file, tell me exactly what to do: copy path, F5 to compile,
  what to check in the Log tab
