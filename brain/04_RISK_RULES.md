# 04 — Risk rules

---

## Hardcoded constants — never expose as UI parameters

These are safety rails. They protect capital. They are never configurable.

```csharp
private const double StopPoints     = 15.0;   // €375/contract — non-negotiable
private const double Target1Points  =  5.0;   // €125/contract
private const double Target2Points  = 10.0;   // €250/contract
private const double PointValue     = 25.0;   // €25 per point — FDAX spec
private const int    MaxContracts   =  1;     // until 50+ trade sample proven
private const int    CooldownBars   =  6;     // 30 min on 5m bars after stop-out
private const int    SessionOpenBuffer  = 15; // minutes — no trades in first/last 15
private const int    SessionCloseBuffer = 15; // minutes
```

---

## Configurable parameters — expose as NinjaScriptProperty

```csharp
[NinjaScriptProperty]
[Range(200, 2000)]
public double DailyLossLimitEuro { get; set; } = 750.0;  // default = 2x stop

[NinjaScriptProperty]
[Range(50, 100)]
public int MinReversalScore { get; set; } = 65;

[NinjaScriptProperty]
public bool EnableSweepFade { get; set; } = true;

[NinjaScriptProperty]
public bool EnableReversal { get; set; } = false;  // off until built and tested

[NinjaScriptProperty]
public bool EnableTrendPullback { get; set; } = false;  // off until built and tested

[NinjaScriptProperty]
[Range(1, 10)]
public int MaxDailyTrades { get; set; } = 6;  // hard cap — prevents overtrading
```

---

## Daily loss halt — implementation pattern

```csharp
private double _dailyRealizedPnL = 0.0;
private bool   _haltedForDay     = false;
private int    _dailyTradeCount  = 0;

// In OnBarUpdate — check at top before any signal logic
if (_haltedForDay) return;
if (_dailyTradeCount >= MaxDailyTrades) return;

// Reset on new session
if (Bars.IsFirstBarOfSession)
{
    _dailyRealizedPnL = 0.0;
    _haltedForDay     = false;
    _dailyTradeCount  = 0;
}

// After any closed trade — update and check
_dailyRealizedPnL += tradeResultEuro;
_dailyTradeCount++;
if (_dailyRealizedPnL <= -DailyLossLimitEuro)
{
    _haltedForDay = true;
    Print("DAILY LOSS LIMIT HIT — halted for remainder of session");
}
```

---

## Prop firm compliance rules

These apply regardless of whether currently on a funded account.
Design for compliance from day one.

| Rule | Requirement | Implementation |
|------|------------|----------------|
| Daily loss limit | Typically 2–4% of account | DailyLossLimitEuro param — set per firm |
| Consistency | No single day > 40% of total profit | Track cumulative P&L; reduce size when daily target approached |
| Max contracts | Firm-specific — typically 5–10 | MaxContracts const — set conservatively |
| Weekend flat | No positions over weekend | Force flat Friday 15:45 UTC — hardcoded |
| No news trading | Varies | Manual blackout Phase 1 — automated calendar Phase 2 |
| Drawdown trailing | Varies by firm | Track peak equity; halt if drawdown from peak exceeds limit |

### Weekend flat enforcement (hardcoded, no override)
```csharp
if (Time[0].DayOfWeek == DayOfWeek.Friday)
{
    int t = ToTime(Time[0]);
    if (t >= 154500)  // 15:45 UTC
    {
        if (Position.MarketPosition != MarketPosition.Flat)
        {
            ExitLong("WeekendFlat", "");
            ExitShort("WeekendFlat", "");
        }
    }
}
```

---

## Portfolio-level risk — Phase 2

A single Python process that:
1. Reads live P&L from FDAX strategy (via NT8 ATI or file output)
2. Reads live P&L from NQ system (Python trading_loop.py)
3. Enforces combined daily loss limit across both instruments
4. Can send flatten command to both systems simultaneously
5. Writes combined P&L to dashboard

This is not built yet. Phase 2 after both individual systems are live and stable.
