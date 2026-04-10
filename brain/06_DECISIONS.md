# 06 — Architecture decisions

Every significant decision is recorded here with reasoning.
If you are about to do something that conflicts with a decision below, stop and flag it.

---

## Strategy architecture

### Single strategy class, not three separate strategies
**Decision:** One NinjaScript strategy file with internal state machine handling
all three signal types (sweep fade, reversal, trend pullback).
**Reason:** Position sizing, daily loss limits, session filtering, and the daily
trade counter must all be enforced in one place. Three independent strategies
would fight over orders, double-up on risk, and make P&L attribution impossible.

### State machine pattern inside strategy
**Decision:** Flat → WaitingForSignal → SignalConfirmed → InTrade → CoolingDown
**Reason:** Explicit states prevent impossible conditions (entering while in a trade,
applying cooldown incorrectly). Each state has exactly one set of valid transitions.

---

## Calculation and performance

### Calculate.OnBarClose always
**Decision:** Never use OnEachTick for FDAX strategies unless explicitly required.
**Reason:** OnEachTick fires hundreds of times per bar. OnBarClose fires once.
5-minute bar strategies have no need for tick-level calculation.

### Managed orders only
**Decision:** Never switch to unmanaged mode without explicit instruction and clear reason.
**Reason:** NT8 managed mode handles order lifecycle, partial fills, and bracket
management automatically. Unmanaged mode requires manual state tracking that
introduces bugs without meaningful benefit for this strategy style.

---

## Instrument-specific

### FDAX — points only, never ticks
**Decision:** All stop and target logic uses points and CalculationMode.Price.
CalculationMode.Ticks is never used for FDAX.
**Reason:** FDAX point value is €25. Tick-based math produces wrong euro values
and is a recurring source of bugs. The rule is simple: 15-point stop,
5-point T1, 10-point T2. In code: entryPrice ± N (whole number).

### 6E (Euro FX) — skipped
**Decision:** 6E is not added to the portfolio.
**Reason:** Euro FX is driven by the same ECB policy and EUR/USD flows as DAX.
Adding it creates hidden correlation with FDAX, not diversification.

---

## Build order

### Sweep fade first
**Decision:** FDAX_SweepFadeStrategy is built before reversal or trend pullback.
**Reason:** The LiquiditySweep signal is binary (fired / not fired). No threshold
to tune. This proves the execution framework (managed orders, session filter,
daily loss halt, cooldown, state machine) is correct before adding signal complexity.

### Asian session after FDAX proven
**Decision:** Asian session instrument research does not begin until FDAX sweep
fade has 4+ weeks of live paper trading data.
**Reason:** Parallel strategy development prevents clean attribution. If two
strategies are built simultaneously and both underperform, we cannot diagnose why.

### No parallel strategy development
**Decision:** One strategy built, proven, stabilised before the next begins.
**Reason:** See above. Also: debugging two simultaneously failing strategies
across different sessions is unsustainable for a solo operator.

---

## Infrastructure

### IB Gateway over TWS
**Decision:** Migrate IBKR connection from TWS to IB Gateway for permanent deployment.
**Reason:** TWS has a known layout bug causing API settings (port 7497) to reset
on restart. IB Gateway is headless, designed for programmatic use, stable across
restarts. Should have been the choice from the beginning.

### OneDrive path — always use it
**Decision:** All NT8 Custom files are at `C:\Users\jgdam\OneDrive\Documents\NinjaTrader 8\`
not the standard `Documents\NinjaTrader 8\` path.
**Reason:** OneDrive sync moves the Documents folder. Standard path fails silently.
This has caused wasted time. Always use the full OneDrive path.

### Indicator Values[] requirement
**Decision:** All indicators feeding the strategy layer must expose signals as
Series<double> plots readable via Values[n][0]. Indicators that only draw chart
objects (arrows, lines, text) without writing to Values[] must be modified before
strategy development begins.
**Reason:** NinjaScript strategies cannot read draw objects programmatically.
Only Values[] and typed public properties are accessible from the strategy class.

---

## Risk and prop firm

### Hardcode safety rails, parameterise tuning levers
**Decision:** Stop (15pts), targets (5/10pts), max contracts (1), cooldown (6 bars),
weekend flat time — all hardcoded as private const. Daily loss limit, reversal
score threshold, session enable toggles — configurable as NinjaScriptProperty.
**Reason:** A misconfigured UI parameter should never be able to violate a
safety rule. If it is not a tuning lever, it is not a parameter.

### Consistency rule — design for it from day one
**Decision:** Strategies are designed to produce consistent moderate returns,
not to maximise single-day P&L.
**Reason:** Prop firm consistency rules (typically no single day > 40% of total
profit) disqualify strategies with occasional blowout days even when overall
profitable. The partial exit structure (T1 at 5pts, T2 at 10pts) is intentional —
it locks in consistent smaller wins rather than running for maximum.
