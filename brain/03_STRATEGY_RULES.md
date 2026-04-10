# 03 — Strategy rules

All strategy logic is documented here. This is the source of truth.
If code conflicts with this document, the code is wrong.

---

## Universal rules — apply to every strategy

- Managed orders only
- Calculate.OnBarClose only
- Max 1 concurrent position per instrument
- Session filter enforced via ToTime() — no trades outside session window
- Daily loss halt: if cumulative daily loss >= DailyLossLimitEuro, halt all new entries
- Cooldown: 6 bars (30 minutes on 5m chart) after any stopped-out trade
- Weekend flat: force close all positions Friday 15:45 UTC (hardcoded)
- Single strategy class handles all signal types — not three separate strategies

---

## Strategy 1 — FDAX Sweep Fade (BUILD FIRST)

### Concept
A liquidity sweep is a false breakout above or below a significant swing
high/low that immediately reverses. The move clears resting stop orders,
then price snaps back. We fade the sweep at the close of the sweep candle.

### Why this is built first
The signal is binary: FDAX_LiquiditySweep fired Good or Strong, or it did not.
No score threshold to calibrate. Cleanest way to prove the execution plumbing
before adding signal complexity.

### Entry conditions — all must be true on bar close
1. FDAX_LiquiditySweep signal = Good or Strong (not Weak)
2. FDAX_RegimeDetector = Auction (0) preferred
   — allowed in TrendUp/TrendDown only if sweep is counter to dominant trend
3. ToTime(Time[0]) between 70000 and 173000 (07:00–17:30 CET)
4. Not within 15 minutes of session open or close
5. _haltedForDay == false
6. _cooldownBarsRemaining == 0
7. Position.MarketPosition == MarketPosition.Flat

### Entry execution
- Direction: Long if bull sweep (swept below swing low and recovered)
             Short if bear sweep (swept above swing high and reversed)
- Order type: Limit at close of sweep candle
- Quantity: 1 contract

### Exit rules
- Stop loss: 15 points from entry price (CalculationMode.Price)
- Target 1: 5 points — exit 50% of position
- Target 2: 10 points — exit remaining 50%
- No trailing stop — fixed targets only on this strategy

### State machine
```
Flat → WaitingForSignal → SignalConfirmed → InTrade → CoolingDown → Flat
```

---

## Strategy 2 — FDAX Reversal at Key Levels (BUILD SECOND)

### Concept
Enter at key level rejections where price shows clear failure. Key levels
include session pivot points, previous session high/low, significant round
numbers, VWAP standard deviation bands. FDAX_ReversalScorer quantifies
the probability of reversal at each bar.

### Entry conditions — all must be true on bar close
1. FDAX_ReversalScorer >= MinReversalScore (default 65, configurable)
2. Price within 3 points of a key level
3. Bar shows rejection candle (pin bar, engulfing, or outside bar reversal)
4. FDAX_RegimeDetector: any regime, Auction preferred
5. Session filter, halt filter, cooldown filter — same as Strategy 1

### Entry execution
- Direction: determined by rejection direction
- Order type: Limit at close of rejection candle
- Quantity: 1 contract

### Exit rules
- Stop: 15 points | Target 1: 5 points (50%) | Target 2: 10 points (50%)

---

## Strategy 3 — FDAX Trend Pullback to VWAP (BUILD THIRD)

### Concept
In a confirmed trending regime, price pulling back to the session VWAP
represents high-probability trend continuation entries. Enter only in the
direction of the regime trend.

### Entry conditions — all must be true on bar close
1. FDAX_RegimeDetector = TrendUp (1) or TrendDown (2)
2. Price has pulled back to touch or cross VWAP from the trend side
3. Bar closes rejecting VWAP back in trend direction
4. ATR(14) confirms active volatility (not dead/flat market)
5. Session filter, halt filter, cooldown filter — same as Strategy 1

### Entry execution
- Direction: Long in TrendUp, Short in TrendDown only
- Order type: Limit at VWAP level
- Quantity: 1 contract

### Exit rules
- Stop: 15 points | Target 1: 5 points (50%) | Target 2: 10 points (50%)
- Phase 2 enhancement: consider trailing stop to VWAP after T1 hit

---

## Asian session strategy — TBD

Instrument not yet chosen. Logic placeholder:
- VWAP mean reversion during Tokyo session (09:00–11:30 AEST)
- Low volatility range-bound conditions
- Fade extremes at VWAP SD bands
- Rules to be defined once instrument research complete

---

## What does NOT get automated (yet)

- News event blackout calendar — manual avoidance in Phase 1, automated Phase 2
- FESX correlation filter — insufficient data until Phase 2
- Multi-contract scaling — not until 50+ trade sample proven on 1 contract
