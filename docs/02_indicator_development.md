# 02 — Indicator Development

**Official source:** https://developer.ninjatrader.com/docs/desktop/indicator  
**Tutorial guide:** https://ninjatrader.com/support/helpguides/nt8/developing_indicators.htm  
**Educational resources:** https://developer.ninjatrader.com/docs/desktop/educational_resources

---

## Indicator Architecture

A custom indicator inherits from `Indicator` and must implement:
- `OnStateChange()` — configure plots, properties, data series
- `OnBarUpdate()` — calculation logic called on every bar/tick

All built-in NT8 indicators ship with **full source code** — accessible in NinjaScript Editor under Indicators. These are the best learning resource available.

---

## Minimal Indicator Template

```csharp
#region Using declarations
using System;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using System.Windows.Media;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MyIndicator : Indicator
    {
        private double _myValue;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "MyIndicator";
                Description = "Description of what this indicator does";
                Calculate   = Calculate.OnBarClose;
                IsOverlay   = false;  // false = subgraph panel, true = price pane
                
                // Define a plot (line on chart)
                AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "Signal");
                
                // User-configurable input
                Period = 14;
            }
            else if (State == State.DataLoaded)
            {
                BarsRequiredToPlot = Period;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToPlot) return;
            
            // Your calculation here
            // Values[0][0] = plot index 0, current bar
            Values[0][0] = SMA(Close, Period)[0];
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Period", GroupName = "Parameters", Order = 0)]
        public int Period { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Signal => Values[0];
        #endregion
    }
}
```

---

## Plots

Plots are the visual lines/dots/histograms drawn on the chart.

### Adding Plots (in SetDefaults)
```csharp
// Line plot
AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "MyLine");

// Histogram (bar chart below)
AddPlot(new Stroke(Brushes.Green, 1), PlotStyle.Bar, "Histogram");

// Dot/cross markers
AddPlot(new Stroke(Brushes.Red, 3), PlotStyle.Dot, "Signal");

// Solid block (like Heiken Ashi candle body)
AddPlot(new Stroke(Brushes.Yellow, 1), PlotStyle.Block, "Block");
```

### Adding Horizontal Lines (in SetDefaults)
```csharp
AddLine(Brushes.Gray, 0,  "ZeroLine");     // at level 0
AddLine(Brushes.Red,  70, "Overbought");
AddLine(Brushes.Green,30, "Oversold");
```

### Setting Plot Values (in OnBarUpdate)
```csharp
// By index (0 = first AddPlot call, 1 = second, etc.)
Values[0][0] = myCalculatedValue;
Values[1][0] = anotherValue;

// By named property (if you expose it)
Signal[0]    = myCalculatedValue;
```

### Dynamic Plot Colours
```csharp
// Colour the plot differently per bar
if (Close[0] > Close[1])
    PlotBrushes[0][0] = Brushes.Green;
else
    PlotBrushes[0][0] = Brushes.Red;
```

---

## Accessing Price and Volume Data

```csharp
// Current bar (index 0 = current, 1 = 1 bar ago, etc.)
double close   = Close[0];
double open    = Open[0];
double high    = High[0];
double low     = Low[0];
double volume  = Volume[0];
double typical = (High[0] + Low[0] + Close[0]) / 3.0;

// Previous bars
double prevClose    = Close[1];
double twoAgoClose  = Close[2];

// Bar time
DateTime barTime = Time[0];
int      barHour = Time[0].Hour;
```

---

## Using Built-in Indicators Inside Your Indicator

```csharp
// Call any built-in indicator — returns its current value
double ema20  = EMA(Close, 20)[0];
double sma50  = SMA(Close, 50)[0];
double rsi14  = RSI(Close, 14, 3)[0];
double atr14  = ATR(14)[0];
double macd   = MACD(Close, 12, 26, 9).Value[0];
double upper  = BollingerBands(Close, 14, 2).Upper[0];
double lower  = BollingerBands(Close, 14, 2).Lower[0];
double stochK = Stochastics(14, 3, 3).K[0];
double adx    = ADX(14)[0];
double cci    = CCI(14)[0];
double wvap   = VWAP()[0];

// On a different data series (e.g. a Volume series)
double volEma = EMA(Volume, 20)[0];
```

---

## Multi-Timeframe (MTF) Indicators

Access data from a different bar period within your indicator.

```csharp
// In OnStateChange → State.Configure:
AddDataSeries(Data.BarsPeriodType.Minute, 60);  // Add 60-min data as BarsArray[1]

// In OnBarUpdate:
// Must check which series triggered the update
if (BarsInProgress != 0) return;  // Only process on primary (index 0) series

double hourlyClose = BarsArray[1].GetClose(0);  // Current 60-min bar close
double hourlyEMA   = EMA(BarsArray[1], 20)[0];
```

---

## Drawing on the Chart From an Indicator

```csharp
// Horizontal line at a price level
Draw.HorizontalLine(this, "support", 4500.00, Brushes.Gray);

// Arrow up/down markers
Draw.ArrowUp(this, "up" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.LimeGreen);
Draw.ArrowDown(this, "dn" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);

// Text label
Draw.Text(this, "lbl" + CurrentBar, "Signal", 0, High[0] + 3 * TickSize);

// Rectangle (e.g. highlight a zone)
Draw.Rectangle(this, "rect" + CurrentBar, false, 5, High[5], 0, Low[0], Brushes.Yellow, Brushes.Transparent, 30);

// Trend line between two bars
Draw.Line(this, "tl", false, 10, Low[10], 0, Low[0], Brushes.Orange, DashStyleHelper.Solid, 1);

// Remove a drawing
RemoveDrawObject("tag");
```

---

## Series\<T\> — Custom Data Series

Store your own calculated data in a series (auto-indexed like price data):

```csharp
private Series<double> _myData;
private Series<bool>   _isSignal;

// In State.DataLoaded:
_myData  = new Series<double>(this);
_isSignal = new Series<bool>(this);

// In OnBarUpdate:
_myData[0]   = Close[0] - Open[0];  // bar range
_isSignal[0] = _myData[0] > _myData[1];  // rising range

// Access history:
double prevRange = _myData[1];
```

---

## Exposing Your Indicator to Strategies

When another strategy or indicator calls your indicator, they access it via the named plot properties:

```csharp
// In your indicator — expose the plot as a public property
[Browsable(false)]
[XmlIgnore]
public Series<double> Signal => Values[0];

// In a strategy that uses your indicator:
double sigVal = MyIndicator(14).Signal[0];
```

---

## Common Indicator Patterns

### Crossover Detection
```csharp
// EMA crossover
bool crossedUp   = CrossAbove(EMA(Close, 9), EMA(Close, 21), 1);
bool crossedDown = CrossBelow(EMA(Close, 9), EMA(Close, 21), 1);
// Second param: lookback bars to check (1 = just current bar)
```

### Rising/Falling
```csharp
bool isRising  = Rising(EMA(Close, 20));
bool isFalling = Falling(EMA(Close, 20));
```

### Highest/Lowest
```csharp
double highestHigh = MAX(High, 20)[0];   // Highest high over 20 bars
double lowestLow   = MIN(Low, 20)[0];    // Lowest low over 20 bars
```

---

## Indicator Reference Links

| Resource | URL |
|---|---|
| Indicator API reference | https://developer.ninjatrader.com/docs/desktop/indicator |
| Developing indicators (tutorial) | https://ninjatrader.com/support/helpguides/nt8/developing_indicators.htm |
| All educational resources | https://developer.ninjatrader.com/docs/desktop/educational_resources |
| Common classes reference | https://developer.ninjatrader.com/docs/desktop/common |
| Drawing tools reference | https://developer.ninjatrader.com/docs/desktop/drawing_tools |
| SharpDX custom rendering | https://developer.ninjatrader.com/docs/desktop/sharpdx |
