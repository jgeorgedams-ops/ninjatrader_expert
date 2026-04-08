# ninjatrader_expert

Personal NinjaTrader 8 indicator and strategy development repository.

**Instruments:** FDAX primary (DAX Futures, 25 euros per point, 15 point fixed stop) and FESX confirmation (Euro Stoxx 50).

**Workflow:** Spec indicator in Claude.ai, scaffold .cs in Claude Code, compile in NT8 with F5, test via Market Replay, commit to this repo.

**FDAX key parameters:** Point value 25 euros. Fixed stop 15 points equals 375 euros per contract. T1 is 5 points. T2 is 10 points. Session 07:00 to 17:30 CET. Primary chart 5-minute bars. Refinement chart 1-minute bars.
