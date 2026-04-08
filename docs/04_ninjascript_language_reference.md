# 04 — NinjaScript Language Reference

**Common reference:** https://developer.ninjatrader.com/docs/desktop/common  
**Basic syntax:** https://ninjatrader.com/support/helpguides/nt8/basic_syntax.htm  
**Drawing tools:** https://developer.ninjatrader.com/docs/desktop/drawing_tools  
**Bars type reference:** https://developer.ninjatrader.com/docs/desktop/bars_type  
**SharpDX rendering:** https://developer.ninjatrader.com/docs/desktop/sharpdx  
**Performance metrics:** https://developer.ninjatrader.com/docs/desktop/performance_metrics  
**Optimizer reference:** https://developer.ninjatrader.com/docs/desktop/optimizer  
**Market Analyzer column:** https://developer.ninjatrader.com/docs/desktop/market_analyzer_column

---

## NinjaScript Is C#

NinjaScript is C# with NT8-specific base classes and APIs. All standard .NET types, collections, LINQ, and System.* namespaces are available. If you know C#, you know NinjaScript — you just need to learn the NT8-specific APIs.

---

## Basic C# / NinjaScript Syntax

### Variables and Types
```csharp
int    myInt    = 10;
double myDouble = 3.14;
bool   myBool   = true;
string myString = "hello";

// Declare class-level fields (persist across bars)
private int    _barCount;
private double _lastHigh;
private bool   _isSignalActive;
```

### Arithmetic
```csharp
int    sum  = 10 + 7;           // 17
double div  = 10.0 / 3.0;      // 3.333...
double pct  = (Close[0] - Open[0]) / Open[0] * 100;
double abs  = Math.Abs(-5.0);  // 5
double sqrt = Math.Sqrt(16.0); // 4
double pow  = Math.Pow(2, 10); // 1024
double max  = Math.Max(a, b);
double min  = Math.Min(a, b);
double rnd  = Math.Round(3.567, 2); // 3.57
```

### Conditionals
```csharp
if (Close[0] > Open[0])
{
    // bullish bar
}
else if (Close[0] < Open[0])
{
    // bearish bar
}
else
{
    // doji
}

// Ternary operator
string dir = Close[0] > Open[0] ? "Up" : "Down";
```

### Loops
```csharp
// Count bars back
for (int i = 0; i < 20; i++)
{
    double c = Close[i];
}

// While loop
int count = 0;
while (count < 10) { count++; }
```

### String Formatting
```csharp
string s1 = "Price: " + Close[0];
string s2 = string.Format("Bar {0}: O={1:F2} H={2:F2} L={3:F2} C={4:F2}",
                           CurrentBar, Open[0], High[0], Low[0], Close[0]);
string s3 = $"Bar {CurrentBar}: Close={Close[0]:F2}";  // C# interpolation
```

---

## Core NT8 Properties (Available in All Scripts)

### Bar Data
```csharp
Close[0]          // Close price — current bar
Open[0]           // Open price
High[0]           // High price
Low[0]            // Low price
Volume[0]         // Volume (contracts/shares traded)
Median[0]         // (High + Low) / 2
Typical[0]        // (High + Low + Close) / 3
Weighted[0]       // (High + Low + Close + Close) / 4
Time[0]           // DateTime of current bar
TimeLastBarRemoved// DateTime of last removed bar (for live updates)
CurrentBar        // Index of current bar (0-based from start of data)
IsFirstTickOfBar  // true on the first tick of a new bar
```

### Instrument Information
```csharp
Instrument.FullName        // "ES 03-25"
Instrument.MasterInstrument.Name  // "ES"
TickSize                   // Minimum price increment (e.g. 0.25 for ES)
TickValue                  // Dollar value per tick (e.g. $12.50 for ES)
double pointValue = Instrument.MasterInstrument.PointValue;  // e.g. 50 for ES
```

### Bar Period
```csharp
BarsPeriod.BarsPeriodType  // Minute, Tick, Volume, Range, etc.
BarsPeriod.Value           // e.g. 5 for 5-minute
```

---

## Data Series

### Built-in Series
All price series are `ISeries<double>` — indexed like an array:
```csharp
Close[0]    // current
Close[1]    // 1 bar ago
Close[N]    // N bars ago — valid range: 0 to CurrentBar
```

### Custom Series
```csharp
// Declare field
private Series<double> _myCalc;
private Series<bool>   _isSignal;

// Initialise in State.DataLoaded
_myCalc  = new Series<double>(this);
_isSignal = new Series<bool>(this, MaximumBarsLookBack.Infinite); // if need deep history

// Use in OnBarUpdate
_myCalc[0] = High[0] - Low[0];         // bar range
_isSignal[0] = _myCalc[0] > _myCalc[1]; // range expanding
double prevCalc = _myCalc[1];           // access history
```

---

## Common Utility Methods

### Crossover / Direction
```csharp
CrossAbove(series1, series2, lookback)  // true if series1 crossed above series2
CrossBelow(series1, series2, lookback)  // true if series1 crossed below series2
Rising(series)   // true if series[0] > series[1]
Falling(series)  // true if series[0] < series[1]

// Examples
CrossAbove(EMA(Close, 9), EMA(Close, 21), 1)
CrossBelow(RSI(Close, 14, 3), 30, 1)   // RSI crossed below 30
Rising(Volume)
```

### High / Low Lookback
```csharp
MAX(High,  20)[0]   // Highest high over last 20 bars
MIN(Low,   20)[0]   // Lowest low over last 20 bars
MAX(Close, 10)[1]   // Highest close over 10 bars, 1 bar ago
```

### Time Utilities
```csharp
ToTime(Time[0])             // DateTime → int (HHMMSS)
ToTime(15, 30, 0)           // Build time int from H,M,S → 153000
Time[0].Hour                // 0–23
Time[0].Minute              // 0–59
Time[0].DayOfWeek           // DayOfWeek.Monday etc.
Time[0].Date                // DateTime with time set to 00:00:00

// Check if within session
bool inSession = ToTime(Time[0]) >= 93000 && ToTime(Time[0]) < 160000;
```

### Rounding to Tick Size
```csharp
double roundedPrice = Math.Round(myPrice / TickSize) * TickSize;
```

---

## All Built-in Indicators — Quick Reference

```csharp
// Trend
EMA(Close, period)[0]
SMA(Close, period)[0]
WMA(Close, period)[0]                    // Weighted MA
DEMA(Close, period)[0]                   // Double EMA
TEMA(Close, period)[0]                   // Triple EMA
HMA(Close, period)[0]                    // Hull MA
KAMA(Close, period)[0]                   // Kaufman Adaptive MA
LinReg(Close, period)[0]                 // Linear Regression
LinRegSlope(Close, period)[0]            // LinReg Slope
Ichimoku(9, 26, 52).Tenkan[0]           // Ichimoku components
SuperTrend(3, 7).SuperTrend[0]

// Oscillators / Momentum
RSI(Close, period, smooth)[0]
MACD(Close, fast, slow, signal).Value[0]
MACD(Close, fast, slow, signal).Diff[0]  // Histogram
Stochastics(period, smooth, smooth).K[0]
Stochastics(period, smooth, smooth).D[0]
CCI(period)[0]
MFI(period)[0]                           // Money Flow Index
CMO(Close, period)[0]                    // Chande Momentum
ROC(Close, period)[0]                    // Rate of Change
Momentum(Close, period)[0]

// Volatility
ATR(period)[0]
BollingerBands(Close, period, numStdDev).Upper[0]
BollingerBands(Close, period, numStdDev).Lower[0]
BollingerBands(Close, period, numStdDev).Middle[0]
KeltnerChannel(Close, period, multiplier).Upper[0]
DonchianChannel(period).Upper[0]
DonchianChannel(period).Lower[0]
ChaikinVolatility(period, smooth)[0]
HV(Close, period)[0]                     // Historical Volatility

// Volume
VOL()[0]                                 // Raw volume
OBV()[0]                                 // On-Balance Volume
VWAP()[0]                                // Session VWAP
ChaikinMoneyFlow(period)[0]
ADL()[0]                                 // Accumulation/Distribution Line

// Trend Strength
ADX(period)[0]
DMI(period).DMMinus[0]
DMI(period).DMPlus[0]
Aroon(period).AroonUp[0]
Aroon(period).AroonDown[0]

// Other
PivotPoints().PP[0]                      // Daily pivot
PivotPoints().R1[0]                      // Resistance 1
PivotPoints().S1[0]                      // Support 1
```

---

## Collections and Lists

```csharp
// List — ordered, variable size
private List<double> _pivotLevels = new List<double>();

_pivotLevels.Add(High[0]);
_pivotLevels.Remove(oldLevel);
int count = _pivotLevels.Count;
double first = _pivotLevels[0];

// Dictionary — key-value
private Dictionary<string, double> _levels = new Dictionary<string, double>();
_levels["support"] = Low[5];
double s = _levels["support"];
bool exists = _levels.ContainsKey("support");
```

---

## Exception Handling

```csharp
protected override void OnBarUpdate()
{
    try
    {
        if (CurrentBar < BarsRequiredToPlot) return;

        // ... logic ...
    }
    catch (Exception ex)
    {
        Print("ERROR [" + Name + "] Bar " + CurrentBar + ": " + ex.Message);
        // Optionally re-throw if you want the script to halt:
        // throw;
    }
}
```

---

## Drawing Tools Reference

All drawing tools are accessed via `Draw.MethodName(this, tag, ...)`. The `tag` string uniquely identifies the object — calling `Draw` with the same tag updates the existing object.

```csharp
Draw.Line(this, tag, bool autoScale, int barsAgo1, double y1, int barsAgo2, double y2, Brush, DashStyle, int width)
Draw.HorizontalLine(this, tag, double y, Brush)
Draw.VerticalLine(this, tag, int barsAgo, Brush)
Draw.ArrowUp(this, tag, bool autoScale, int barsAgo, double y, Brush)
Draw.ArrowDown(this, tag, bool autoScale, int barsAgo, double y, Brush)
Draw.Diamond(this, tag, bool autoScale, int barsAgo, double y, Brush)
Draw.Dot(this, tag, bool autoScale, int barsAgo, double y, Brush)
Draw.Text(this, tag, string text, int barsAgo, double y)
Draw.TextFixed(this, tag, string text, TextPosition.TopLeft)
Draw.Rectangle(this, tag, bool autoScale, int barsAgo1, double y1, int barsAgo2, double y2, Brush border, Brush fill, int opacity)
Draw.Triangle(this, tag, bool autoScale, int barsAgo1, double y1, int barsAgo2, double y2, int barsAgo3, double y3, Brush)
Draw.Fibonacci(this, tag, bool autoScale, int barsAgo1, double y1, int barsAgo2, double y2)
Draw.RegionHighlightX(this, tag, int barsAgo1, int barsAgo2, Brush fill, Brush border, int opacity)
Draw.RegionHighlightY(this, tag, double y1, double y2, Brush fill, Brush border, int opacity)

RemoveDrawObject(tag);
RemoveDrawObjects();   // Remove all
```

---

## Language Reference Links

| Resource | URL |
|---|---|
| Common reference (shared classes) | https://developer.ninjatrader.com/docs/desktop/common |
| Basic syntax guide | https://ninjatrader.com/support/helpguides/nt8/basic_syntax.htm |
| Drawing tools reference | https://developer.ninjatrader.com/docs/desktop/drawing_tools |
| Bars type reference | https://developer.ninjatrader.com/docs/desktop/bars_type |
| SharpDX custom rendering | https://developer.ninjatrader.com/docs/desktop/sharpdx |
| Performance metrics | https://developer.ninjatrader.com/docs/desktop/performance_metrics |
| Optimizer reference | https://developer.ninjatrader.com/docs/desktop/optimizer |
| Market Analyzer column | https://developer.ninjatrader.com/docs/desktop/market_analyzer_column |
