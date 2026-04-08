# 03 — Strategy Development & Backtesting

**Strategy API reference:** https://developer.ninjatrader.com/docs/desktop/strategy  
**Strategy development process:** https://ninjatrader.com/support/helpguides/nt8/the_strategy_development_process.htm  
**Strategy Analyzer overview:** https://ninjatrader.com/support/helpguides/nt8/strategy_analyzer.htm  
**Backtest guide:** https://ninjatrader.com/support/helpguides/nt8/backtest_a_strategy.htm  
**Optimise guide:** https://ninjatrader.com/support/helpguides/nt8/optimize_a_strategy.htm  
**Multi-objective optimisation:** https://ninjatrader.com/support/helpguides/nt8/multi-objective_optimization.htm  
**Basket testing:** https://ninjatrader.com/support/helpguides/nt8/basket_test.htm

---

## Strategy vs Indicator

| | Indicator | Strategy |
|---|---|---|
| Base class | `Indicator` | `Strategy` |
| Can place orders | No | Yes |
| Has `OnBarUpdate` | Yes | Yes |
| Position tracking | No | Yes (`Position`, `StrategyInfo`) |
| Can use indicators | Yes | Yes |

---

## Managed vs Unmanaged Orders

### Managed Mode (default — start here)
NT8 automatically handles order lifecycle: fills, partial fills, cancel-on-flat. You call simple methods and NT8 does the rest.

### Unmanaged Mode
You control every order state directly. Required for: multiple simultaneous entries of the same direction, complex bracket logic, or when managed mode's automatic behaviour conflicts with your strategy. Requires `IsUnmanaged = true` in `SetDefaults`.

**Use managed mode unless you have a specific reason not to.**

---

## Minimal Strategy Template (Managed)

```csharp
namespace NinjaTrader.NinjaScript.Strategies
{
    public class MyStrategy : Strategy
    {
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name              = "MyStrategy";
                Description       = "What this strategy does";
                Calculate         = Calculate.OnBarClose;
                
                // Order management defaults
                EntriesPerDirection   = 1;   // 1 entry per direction at a time
                EntryHandling         = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;   // Flatten at session end
                ExitOnSessionCloseSeconds    = 30;
                
                // Default parameters
                FastPeriod = 9;
                SlowPeriod = 21;
                StopTicks  = 8;
                TargetTicks = 16;
            }
            else if (State == State.DataLoaded)
            {
                BarsRequiredToTrade = SlowPeriod + 1;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade) return;

            // Only trade on primary series, and don't trade on historical if not needed
            if (BarsInProgress != 0) return;

            // Example: EMA crossover
            double fastEma = EMA(Close, FastPeriod)[0];
            double slowEma = EMA(Close, SlowPeriod)[0];

            // Long entry
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (CrossAbove(EMA(Close, FastPeriod), EMA(Close, SlowPeriod), 1))
                    EnterLong(1, "LongEntry");

                if (CrossBelow(EMA(Close, FastPeriod), EMA(Close, SlowPeriod), 1))
                    EnterShort(1, "ShortEntry");
            }
        }

        // Properties
        [NinjaScriptProperty][Range(1, 100)]
        [Display(Name="Fast Period", GroupName="Parameters", Order=0)]
        public int FastPeriod { get; set; }

        [NinjaScriptProperty][Range(1, 200)]
        [Display(Name="Slow Period", GroupName="Parameters", Order=1)]
        public int SlowPeriod { get; set; }

        [NinjaScriptProperty][Range(1, 100)]
        [Display(Name="Stop (ticks)", GroupName="Risk", Order=0)]
        public int StopTicks { get; set; }

        [NinjaScriptProperty][Range(1, 200)]
        [Display(Name="Target (ticks)", GroupName="Risk", Order=1)]
        public int TargetTicks { get; set; }
    }
}
```

---

## Order Entry Methods

### Market Orders
```csharp
EnterLong(quantity, "SignalName");
EnterShort(quantity, "SignalName");
```

### Limit Orders
```csharp
EnterLongLimit(quantity, limitPrice, "LimitLong");
EnterShortLimit(quantity, limitPrice, "LimitShort");
```

### Stop-Market Orders
```csharp
EnterLongStopMarket(quantity, stopPrice, "StopLong");
EnterShortStopMarket(quantity, stopPrice, "StopShort");
```

### Stop-Limit Orders
```csharp
EnterLongStopLimit(quantity, stopPrice, limitPrice, "StopLimitLong");
```

---

## Exit Methods

```csharp
ExitLong("ExitSignal", "EntrySignalName");      // Exit long by entry name
ExitShort("ExitSignal", "EntrySignalName");
ExitLong(partialQty, "PartialExit", "EntryName"); // Partial exit
ExitLongLimit(quantity, limitPrice, "LimitExit", "EntryName");
ExitLongStopMarket(quantity, stopPrice, "StopExit", "EntryName");
```

---

## Stop Loss, Target & Trailing Stop

Set in `OnStateChange` → `State.DataLoaded`, or dynamically in `OnBarUpdate`:

```csharp
// Fixed stop loss — tick-based
SetStopLoss("EntryName", CalculationMode.Ticks, StopTicks, false);

// Fixed profit target
SetProfitTarget("EntryName", CalculationMode.Ticks, TargetTicks);

// Trailing stop
SetTrailStop("EntryName", CalculationMode.Ticks, TrailTicks, false);

// Dollar-based stop
SetStopLoss("EntryName", CalculationMode.Currency, 100.0, false);

// Percent-based
SetStopLoss("EntryName", CalculationMode.Percent, 0.5, false);

// Price-based (absolute price)
SetStopLoss("EntryName", CalculationMode.Price, stopPrice, false);
```

---

## Position Information

```csharp
Position.MarketPosition   // Flat, Long, Short
Position.Quantity         // Number of contracts
Position.AveragePrice     // Average entry price
Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])

// Check if flat
if (Position.MarketPosition == MarketPosition.Flat) { ... }

// Check if long
if (Position.MarketPosition == MarketPosition.Long) { ... }
```

---

## Session / Time Filtering

```csharp
// Only trade during a time window
if (ToTime(Time[0]) >= 93000 && ToTime(Time[0]) <= 155000)
{
    // Trade between 9:30am and 3:50pm (format: HHMMSS as int)
}

// Check day of week
if (Time[0].DayOfWeek == DayOfWeek.Monday) { ... }

// Time to int conversion
int t = ToTime(Time[0]);   // e.g. 93000 = 9:30:00
```

---

## Event Callbacks

Beyond `OnBarUpdate`, strategies can override:

```csharp
// Called on every order state change
protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice,
    int quantity, int filled, double averageFillPrice, OrderState orderState,
    DateTime time, ErrorCode error, string comment)
{
    Print("Order: " + order.Name + " State: " + orderState);
}

// Called on every execution (fill)
protected override void OnExecutionUpdate(Execution execution, string executionId,
    double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
{
    Print("Fill: " + quantity + " @ " + price);
}

// Called on every market data tick (when Calculate = OnEachTick or OnPriceChange)
protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
{
    if (marketDataUpdate.MarketDataType == MarketDataType.Last)
        Print("Last: " + marketDataUpdate.Price);
}
```

---

## The Strategy Development Process

From NT8's official guidance:

1. **Define** — document your entry/exit rules, risk parameters in plain English first
2. **Build** — code the strategy in NinjaScript
3. **Backtest** — run Strategy Analyzer with historical data
4. **Review** — check performance report: net profit, max drawdown, profit factor, Sharpe ratio
5. **Optimise** — vary parameters within ranges to find optimal values
6. **Simulate** — run on real-time simulation (paper trading) to validate live behaviour
7. **Live** — deploy with real capital, monitor closely

---

## Strategy Analyzer — Backtesting

**Access:** Control Center → New → Strategy Analyzer

### Settings Panel
| Setting | What It Does |
|---|---|
| Strategy | Select the NinjaScript strategy to test |
| Instrument | Symbol (e.g. ES 03-25, MES 03-25) |
| Type | Bar type (Minute, Tick, Volume, Range) |
| Value | Bar period value (e.g. 5 for 5-minute) |
| Time frame | Start/end date range |
| Backtest type | Backtest, Optimisation, Walk-Forward |

### Bar Replay vs Tick Replay
| Mode | Speed | Accuracy | Use For |
|---|---|---|---|
| Bar Replay | Fast | Lower — 50/50 on intra-bar direction | Initial parameter screening |
| Tick Replay | Slow | High — exact execution simulation | Final validation before live |

### Key Performance Metrics
```
Net Profit          — Total P&L over the period
Max Drawdown        — Largest peak-to-trough loss (dollar and %)
Profit Factor       — Gross profit / Gross loss (>1.5 is acceptable, >2 is good)
Sharpe Ratio        — Risk-adjusted return (>1 acceptable, >2 good)
Win Rate            — % of trades that were profitable
Avg Win / Avg Loss  — Ratio of average winner to average loser
Total Trades        — Sample size (need >30 ideally >100 for statistical validity)
Max Consecutive Loss — Worst losing streak (stress test for psychology/risk)
```

---

## Optimisation

**Access:** Strategy Analyzer → Backtest type: Optimisation

### How It Works
Set Min, Max and Increment for each parameter. NT8 iterates every combination (or uses genetic algorithm for large search spaces) and ranks results.

```
FastPeriod: Min=5, Max=20, Increment=1   → tests 5,6,7,...,20
SlowPeriod: Min=20, Max=60, Increment=2  → tests 20,22,24,...,60
```

Total tests = 16 × 21 = 336 backtests

### Optimisation Fitness Options
- Net Profit
- Profit Factor
- Sharpe Ratio
- Custom fitness function (see Performance Metrics reference)

### Multi-Objective Optimisation
Tests against two fitness metrics simultaneously. Results shown as Pareto frontier graph — no single "best" result; you choose the trade-off point.

Requires: `docs/03` → Multi-Objective link above.

### Walk-Forward Optimisation
Avoids curve-fitting by splitting data into optimisation + out-of-sample validation windows, rolling forward. Most robust backtesting method.

### Overfitting Warning
- More parameters = more risk of overfitting
- Optimise in small related groups, not all at once
- Always validate on out-of-sample data
- Results that look too good usually are

### Basket Testing
Test a single strategy across multiple instruments simultaneously:

Strategy Analyzer → Instrument: [select an Instrument List]

---

## Strategy Reference Links

| Resource | URL |
|---|---|
| Strategy API reference | https://developer.ninjatrader.com/docs/desktop/strategy |
| Strategy development process | https://ninjatrader.com/support/helpguides/nt8/the_strategy_development_process.htm |
| Strategy Analyzer overview | https://ninjatrader.com/support/helpguides/nt8/strategy_analyzer.htm |
| Backtest a strategy | https://ninjatrader.com/support/helpguides/nt8/backtest_a_strategy.htm |
| Optimise a strategy | https://ninjatrader.com/support/helpguides/nt8/optimize_a_strategy.htm |
| Multi-objective optimisation | https://ninjatrader.com/support/helpguides/nt8/multi-objective_optimization.htm |
| Basket testing | https://ninjatrader.com/support/helpguides/nt8/basket_test.htm |
| Performance metrics reference | https://developer.ninjatrader.com/docs/desktop/performance_metrics |
| Optimizer reference | https://developer.ninjatrader.com/docs/desktop/optimizer |
