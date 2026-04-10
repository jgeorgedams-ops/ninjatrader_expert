# RESEARCH INSTRUMENT PROMPT
# Use after START_SESSION. Fill in the instrument and paste.

---

Research the following futures instrument for potential addition to my
automated trading system. Give me a structured evaluation and a clear
recommendation: add it, defer it, or skip it.

Instrument to research: [e.g. Nikkei 225 micro — SGX / CME]
Context: I am an Australian retail trader (AEST timezone) looking to cover
the Asian session window: 09:00–12:00 AEST (23:00–02:00 UTC).

Evaluate on these criteria:

1. LIQUIDITY AND SPREAD
   - Is it liquid during 09:00–12:00 AEST?
   - Typical bid-ask spread in points? Acceptable for 5-point T1?

2. VOLATILITY PROFILE
   - Average daily range? Typical 09:00–12:00 AEST session range?
   - Is a 15-point stop reasonable given this range?
   - Too volatile (stops hit randomly) or too flat (targets never reached)?

3. NT8 DATA AND BROKER ACCESS
   - Is a real-time data feed available in NT8 for this instrument?
   - Which data providers support it?
   - Is it accessible through IBKR from Australia?

4. STRATEGY FIT
   - Does it exhibit range-bound / mean-reverting behaviour during Asian hours?
   - Would VWAP mean reversion logic apply?
   - Would liquidity sweep detection be meaningful (enough swing structure)?
   - How does it behave around Tokyo/SGX session open vs mid-session?

5. CONTRACT SPECIFICATIONS
   - Point value in local currency
   - Tick size
   - Typical margin requirement
   - Currency risk (AUD/USD/JPY/HKD hedging needed?)

6. CORRELATION
   - Correlation to FDAX and NQ? (Want low correlation for diversification)
   - What drives this instrument — domestic macro, global risk, commodities?

7. GAP RISK
   - Does it gap overnight between sessions?
   - Any regular scheduled events that cause violent moves? (Data releases, BOJ, etc.)

8. PROP FIRM COMPATIBILITY
   - Is it on TopStep / Apex approved instrument lists?
   - Any restrictions on overnight holding?

Give me your recommendation with a confidence rating (high / medium / low).
If you recommend adding it, suggest what the initial strategy logic should be.
