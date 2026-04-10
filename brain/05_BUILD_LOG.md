# 05 — Build log

Update this file at the end of every session.
Tick off what is done. Add what broke. Move items to the queue.

---

## Completed and working

### NT8 Indicators
- [x] FDAX_RegimeDetector.cs — Auction/TrendUp/TrendDown, Efficiency Ratio, ATR ratio
- [x] FDAX_LiquiditySweep.cs — fractal swing detection, delta confirmation, Weak/Good/Strong
- [x] FDAX_SessionVWAP.cs — VWAP + SD bands, resets 07:00 CET
- [x] FDAX_SignalOverlay.cs — BD/SD delta divergence labels, sweep arrows
- [x] FDAX_ReversalScorer.cs — directional bug fixed, score 0–100

### Python system
- [x] NQ DMAC VectorBT backtest — 10/35 params, year-by-year validated
- [x] NQ RiskManager with pytest coverage
- [x] MockBroker / IBKRBroker abstraction — one-line swap pattern
- [x] Streamlit dashboard — AEST timestamps, emergency flatten button
- [x] config/strategy.yaml — all NQ params stored here
- [x] morning_brief.py — NQ + FDAX unified brief, Task Scheduler, port 8888
- [x] dax_alerts.py — proximity alerts, INSIDE zone priority, cooldown, alert_active.json
- [x] dax_mtf_analysis.py — FVGs, OBs, BOS/CHOCH across 1D/4H/1H/5m
- [x] dax_probability.py — probability engine with sample sizes

### Infrastructure
- [x] Brain files created (brain/ folder) — this session
- [x] Prompt library created (prompts/ folder) — this session
- [x] CLAUDE.md upgraded to point to brain files — this session
- [x] GitHub repo: jgeorgedams-ops/ninjatrader_expert

---

## Outstanding — must fix before strategy build

- [ ] **IBKRBroker one-line swap in trading_loop.py** — NOT COMPLETED
      The swap is one line. MockBroker → IBKRBroker. Do this first.

- [ ] **TWS API settings persistence** — resets on restart
      Fix: migrate from TWS to IB Gateway (headless, stable)

- [ ] **FDAX indicator Values[] audit** — CRITICAL prerequisite
      Confirm each of the 5 indicators writes signals to Values[n][0]
      not just to drawn chart objects. If any indicator only draws,
      it must be modified to also write a numeric Series<double> plot.
      Indicators to audit: LiquiditySweep, SessionVWAP, SignalOverlay, ReversalScorer

---

## Build queue — in strict order

| # | Task | Prerequisite | Notes |
|---|------|-------------|-------|
| 1 | Complete IBKRBroker swap | Nothing | One line in trading_loop.py |
| 2 | Values[] audit on all 4 indicators | Nothing | May require indicator edits |
| 3 | Build FDAX_SweepFadeStrategy.cs | Items 1 and 2 | Full skeleton — see 03_STRATEGY_RULES.md |
| 4 | Paper trade sweep fade | Item 3 | Minimum 4 weeks before drawing conclusions |
| 5 | Build FDAX_ReversalStrategy.cs | Item 4 stable | Add to same strategy class |
| 6 | Build FDAX_TrendPullbackStrategy.cs | Item 5 stable | Add to same strategy class |
| 7 | Asian session instrument research | Item 3 started | Use RESEARCH_INSTRUMENT prompt |
| 8 | Build Asian session strategy | Item 7 decision | New strategy class |
| 9 | Portfolio-level risk Python process | Items 3+8 live | Cross-system flatten + P&L |

---

## Known issues / watch list

| Issue | Status | Notes |
|-------|--------|-------|
| FDAX_SweepClassifier (original) | Retired | Replaced by FDAX_LiquiditySweep — do not use |
| FDAX_SignalOverlay sweep arrows | Monitor | May need parameter tuning as live data accumulates |
| True cumulative delta | Unavailable | Using proxy: (Close - Open) * Volume — acceptable for now |
| FESX data | Limited (~3 months) | Defer as confirmation instrument until Phase 2 |
| NQ DMAC bear market (2022) | Known weakness | Cooldown periods partially mitigate — documented in backtest |

---

## Session notes (append here after each session)

### [Date] — Brain system created
- Created brain/ and prompts/ folders
- Created all 7 brain files
- Created 6 prompt files
- Upgraded CLAUDE.md
- Next: IBKRBroker swap then Values[] audit
