# 02 — Sessions and instruments

## Session coverage — current and target

| Session | AEST | UTC | Instrument | Status |
|---------|------|-----|-----------|--------|
| Asian open | 09:00–12:00 | 23:00–02:00 | TBD — research phase | Not built |
| EU open | 17:00–19:30 | 07:00–09:30 | FDAX | Building |
| Euro lunch fade | 21:00–22:30 | 11:00–12:30 | FDAX | Part of EU strategy |
| US premarket watch | 22:30–24:00 | 12:30–14:00 | NQ | DMAC running |
| US RTH | 23:30–06:00 | 13:30–20:00 | NQ | DMAC running |

---

## FDAX (DAX Futures) — primary instrument

| Spec | Value |
|------|-------|
| Exchange | Eurex |
| Point value | €25.00 per point |
| Tick size | 0.5 points |
| **Stop loss** | **15 points — HARDCODED, never configurable** |
| **Target 1** | **5 points (exit 50%)** |
| **Target 2** | **10 points (exit 50%)** |
| Session | 07:00–17:30 CET |
| Primary chart | 5-minute bars |
| Refinement chart | 1-minute bars |
| Confirmation | FESX (Euro Stoxx 50) — limited data, Phase 2 |

**FDAX rule that is never broken:** All stop and target logic uses points and
CalculationMode.Price. Never CalculationMode.Ticks. Tick references in code are bugs.

---

## NQ (Nasdaq E-mini) — US session

| Spec | Value |
|------|-------|
| Exchange | CME |
| Point value | $20.00 per point |
| Tick size | 0.25 points |
| Strategy | DMAC 10/35 — fully built in Python |
| Execution | MockBroker (testing) → IBKRBroker (live — swap not yet done) |
| Session | US RTH 09:30–16:00 ET (23:30–06:00 AEST) |

---

## Asian session — instrument under research (do not start until FDAX proven)

### Criteria for selection
1. Liquid during 09:00–12:00 AEST (Tokyo/SGX open)
2. Sufficient daily range for 15-point stop + 10-point target to be reasonable
3. NT8 data feed available and reliable
4. Accessible from Australian broker (IBKR preferred)
5. Fits reversal / sweep fade logic — range-bound morning behaviour preferred
6. On prop firm approved instrument list
7. Does not correlate heavily with FDAX or NQ (diversification benefit)

### Candidates to research

| Instrument | Exchange | Notes |
|-----------|---------|-------|
| Nikkei 225 micro (MNI) | SGX / CME | Strong candidate — active 09:00 AEST, good range |
| ASX SPI 200 | ASX | Domestic — easy broker access, familiar market |
| HSI mini | HKEX | Volatile, wide spreads, higher risk |
| Nifty 50 futures | NSE | Exotic — data and broker access harder |

**Decision:** Not starting research until FDAX_SweepFadeStrategy has 4+ weeks of live data.

---

## Expansion instruments — Phase 3 only

| Instrument | Verdict | Reason |
|-----------|---------|--------|
| ES (S&P 500 E-mini) | Add Phase 2 | Correlated to NQ but larger size. NQ DMAC logic ports directly. |
| GC (Gold) | Best overnight add | Trends well, low gap risk, VWAP pullback logic fits, ~24hr liquidity |
| CL (Crude oil) | Defer — high risk | Violent gaps on EIA inventory reports. Needs news blackout calendar first. |
| 6E (Euro FX) | Skip | Correlated to DAX — same ECB/EUR drivers. No diversification. |
