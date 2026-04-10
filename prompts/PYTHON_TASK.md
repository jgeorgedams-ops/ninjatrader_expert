# PYTHON TASK PROMPT
# Use after START_SESSION. Fill in the task and paste.

---

I need to build or modify something in the Python trading system.
The system lives at C:\Users\jgdam\ruflo-trading\

Context you must know before writing any code:
- Python 3.14 on Windows
- Primary libraries: VectorBT, yfinance, ib_insync, Streamlit, pytest
- IBKR connection: ib_insync, TWS paper trading port 7497
- MockBroker / IBKRBroker pattern — one-line swap in trading_loop.py
- All timestamps displayed in AEST (UTC+10 / UTC+11 DST)
- PowerShell uses semicolons not && for command chaining

Task: [DESCRIBE WHAT YOU WANT BUILT OR CHANGED]

Requirements — enforce without me asking:
- Complete files, not fragments
- If modifying an existing file, show me the complete updated file
- If creating a new file, tell me exactly where to put it
- All timestamps in AEST — convert from UTC at display time, store as UTC
- Error handling with descriptive messages — no silent failures
- If this touches the broker abstraction, maintain the MockBroker/IBKRBroker
  one-line swap pattern — do not break it
- If this touches risk management, cross-check against brain/04_RISK_RULES.md
- After giving me the code, tell me exactly how to test it before running live
