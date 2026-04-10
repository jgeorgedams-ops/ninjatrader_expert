# DEBUG NINJASCRIPT PROMPT
# Use after START_SESSION. Paste error and code below the divider.

---

I have a NinjaScript compile error or runtime bug. Diagnose and fix it.

Rules for your response:
- Identify the exact cause first, in one sentence
- If the fix is under 10 lines, give me just the corrected lines with context
- If the fix touches a method, give me the complete corrected method — not a fragment
- If the fix requires understanding the full file, ask me to paste it
- Do not suggest workarounds that violate the coding rules in CLAUDE.md

Before suggesting anything, run through this checklist mentally:
- Index out of bounds → is CurrentBar < BarsRequiredToPlot guard missing or wrong?
- NaN values → is the indicator not warmed up? Is BarsRequiredToPlot too low?
- Object reference not set → was the object initialised in SetDefaults instead of DataLoaded?
- Memory leak / ghost behaviour → are events not unsubscribed in State.Terminated?
- FDAX stop/target wrong → is CalculationMode.Ticks being used instead of .Price?
- Strategy not entering → is BarsRequiredToTrade too high? Is session filter blocking?
- Orders not filling in backtest → check BarsRequiredToTrade vs data length
- Draw object tag collision → are tags unique per bar? Use tag + CurrentBar.

ERROR FROM NT8 LOG TAB:
[PASTE ERROR HERE]

RELEVANT CODE:
[PASTE THE METHOD OR BLOCK WHERE THE ERROR OCCURS]
