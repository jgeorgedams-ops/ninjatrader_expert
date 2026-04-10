# 01 — System map

## Repositories

| Repo | Local path | GitHub |
|------|-----------|--------|
| NT8 development | `C:\Users\jgdam\ninjatrader_expert\` | jgeorgedams-ops/ninjatrader_expert |
| Python system | `C:\Users\jgdam\ruflo-trading\` | — |

## NT8 custom files path (OneDrive — use this, not standard Documents)

```
C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\bin\Custom\
```

---

## NT8 indicators — all compiled and working

| File | Purpose | Exposes | Known issues |
|------|---------|---------|--------------|
| FDAX_RegimeDetector.cs | Classifies Auction / TrendUp / TrendDown via Efficiency Ratio, ATR ratio, directional consistency | CurrentRegime property (0=Auction, 1=TrendUp, 2=TrendDown) | None |
| FDAX_LiquiditySweep.cs | Fractal swing sweep detection, delta confirmation, Weak/Good/Strong scoring, green/red entry lines | Values[] — AUDIT REQUIRED | None |
| FDAX_SessionVWAP.cs | Session VWAP + SD bands, resets 07:00 CET | Values[] — AUDIT REQUIRED | None |
| FDAX_SignalOverlay.cs | Delta divergence BD/SD labels, sweep arrows | Values[] — AUDIT REQUIRED | May need parameter tuning |
| FDAX_ReversalScorer.cs | Reversal probability score 0–100 at key levels | Values[] — AUDIT REQUIRED | Directional bug fixed last session |

**Critical pre-build task:** Audit all indicators to confirm signals are exposed as
Series<double> plots readable via Values[n][0] by the strategy layer — not just as
drawn chart objects. If any indicator only draws and does not write to Values[], it
must be modified before strategy development begins.

---

## NT8 strategies — planned build queue

| File | Status | Session | Signals |
|------|--------|---------|---------|
| FDAX_SweepFadeStrategy.cs | NOT STARTED — build first | EU | FDAX_LiquiditySweep Good/Strong + RegimeDetector |
| FDAX_ReversalStrategy.cs | NOT STARTED — build second | EU | FDAX_ReversalScorer >= threshold + regime |
| FDAX_TrendPullbackStrategy.cs | NOT STARTED — build third | EU | FDAX_RegimeDetector Trend + FDAX_SessionVWAP |
| Asian_Strategy.cs | NOT STARTED — research phase | Asian | TBD — instrument not yet chosen |

---

## Python system — C:\Users\jgdam\ruflo-trading\

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| NQ DMAC VectorBT backtest | — | Working | 10/35 DMAC, empirically derived params |
| NQ RiskManager | — | Working | pytest coverage |
| Broker abstraction | trading_loop.py | Working — swap INCOMPLETE | MockBroker working; IBKRBroker stub exists but one-line swap not done |
| Streamlit dashboard | — | Working | AEST timestamps, emergency flatten |
| Morning brief | morning_brief.py | Working | Runs via Task Scheduler, HTTP port 8888 |
| DAX alerts | dax_alerts.py | Working | Proximity threshold, INSIDE zone priority, cooldown, alert_active.json |
| DAX MTF analysis | dax_mtf_analysis.py | Working | FVGs, OBs, BOS/CHOCH across 1D/4H/1H/5m |
| DAX probability engine | dax_probability.py | Working | Sample sizes on every stat |

---

## IBKR connection

| Item | Status |
|------|--------|
| Paper trading account | Connected |
| TWS port | 7497 |
| Settings persistence | BROKEN — resets on TWS restart (known IBKR Pro layout bug) |
| Fix | IB Gateway recommended — headless, stable, designed for algo use |
| IBKRBroker swap | NOT COMPLETED — outstanding task in trading_loop.py |

---

## Three-terminal operational workflow (Python system)

```
Terminal 1:  morning_brief.py     — run once at session start
Terminal 2:  dax_alerts.py        — run all session
Terminal 3:  trading_loop.py      — run all session
```

---

## Config files

| File | Purpose |
|------|---------|
| config/strategy.yaml | NQ DMAC parameters — all params stored here |
| alert_active.json | Live alert state written by dax_alerts.py |
