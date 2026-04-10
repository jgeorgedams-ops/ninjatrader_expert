# 00 — Who I am

## Trader profile

- Australian retail futures trader, The Entrance North NSW (AEST timezone, UTC+10/+11)
- Experienced discretionary trader — years of screen time on FDAX and NQ
- Transitioning from discretionary to fully automated algorithmic trading
- Three core discretionary strategies being systematised:
  1. Reversal trades at key levels (pivots, fibs, liquidity sweeps)
  2. Trend and efficiency trades (regime-filtered pullbacks)
  3. Liquidity sweep fades (stop-hunt reversals)
- Coding level: intermediate-beginner. Can run, iterate, debug with guidance.
  Cannot architect complex systems from scratch independently.
- Primary execution platform: NinjaTrader 8 (NinjaScript C#)
- Research and signal platform: Python 3.14 (VectorBT, yfinance, Streamlit)

---

## My trading day — AEST windows I can monitor

| Window | AEST | UTC | What I want here |
|--------|------|-----|-----------------|
| Asian session | 09:00–12:00 | 23:00–02:00 | Automated — instrument TBD |
| Gap | 12:00–17:00 | 02:00–07:00 | Sleep — nothing running |
| EU open | 17:00–19:30 | 07:00–09:30 | FDAX — prime session |
| Euro lunch fade | 21:00–22:30 | 11:00–12:30 | FDAX — fade exhaustion |
| US premarket | 22:30–24:00 | 12:30–14:00 | NQ premarket watch |

---

## Rules Claude must enforce — always, without being asked

- Be opinionated. Tell me what is right, not a list of options.
- Challenge assumptions if something conflicts with good architecture or risk management.
- Prefer production-quality code over tutorial-level examples.
- Give complete files, not fragments. I cannot reliably integrate partial code.
- When I ask for architecture first — give me the full picture before writing any code.
- Always flag if something conflicts with prop firm rules (daily loss, consistency, weekend flat).
- Never write tick-based stop/target math for FDAX. Points only. €25 per point.
- Default to 1 contract. Never assume I want more size.
- Build one strategy, prove it, then the next. No parallel strategy development.

---

## Psychology rules — hardcode these into strategy design

- I overtrade when bored — enforce hard daily trade count limits
- I am most dangerous after a losing session — enforce daily loss halt with no override
- I trust systems more when I know why every parameter was chosen — document decisions
- I prefer consistent moderate returns over blowout days — protect the prop firm consistency rule
- If a trade cannot be explained in two sentences, it should not be automated
