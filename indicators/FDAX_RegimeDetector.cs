#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Core.FloatingPoint;
#endregion

// ============================================================
// FDAX REGIME DETECTOR — v3
// ============================================================
// Classifies each bar as AUCTION / TREND-UP / TREND-DOWN
// using Efficiency Ratio + ATR ratio + Directional Consistency.
//
// Draws a TopLeft debug panel showing regime + raw indicator values.
// Draws a subtle background tint and regime-change dots.
//
// PUBLIC PROPERTY — readable by other indicators on the same chart:
//   CurrentRegime     → 0 = Auction, 1 = Trend Up, 2 = Trend Down
//   CurrentRegimeName → human-readable string for debug panels
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators
{
    public class FDAX_RegimeDetector : Indicator
    {
        // ── Classification thresholds (calibrated to 2.5yr FDAX data) ──
        private const double ER_THRESHOLD  = 0.45;   // ER above this = efficient / trending
        private const double ATR_THRESHOLD = 1.05;   // ATR ratio above this = expanding
        private const double DIR_UP        = 0.65;   // >65% of last 10 bars up = Trend Up
        private const double DIR_DOWN      = 0.35;   // <35% of last 10 bars up = Trend Down

        // ── Lookback periods ─────────────────────────────────────────
        private const int ER_PERIOD  = 10;
        private const int ATR_PERIOD = 14;
        private const int ATR_SLOW   = 60;
        private const int DIR_PERIOD = 10;

        // ── Internal series ──────────────────────────────────────────
        private Series<double> _er;
        private Series<double> _atr;
        private Series<double> _atrAvg;

        // ── Regime persistence tracking ───────────────────────────────
        private int _barsOnCurrentRegime = 0;

        // ── NinjaScript properties ────────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "ER Period", Order = 1, GroupName = "Parameters")]
        public int ErPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ATR Period", Order = 2, GroupName = "Parameters")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Debug Panel", Order = 3, GroupName = "Parameters",
                 Description = "Show the TopLeft regime debug panel")]
        public bool ShowDebugPanel { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description    = "FDAX Regime Detector — Auction / Trend-Up / Trend-Down";
                Name           = "FDAX_RegimeDetector";
                Calculate      = Calculate.OnBarClose;
                IsOverlay      = true;
                ErPeriod       = ER_PERIOD;
                AtrPeriod      = ATR_PERIOD;
                ShowDebugPanel = true;

                // Plot stores numeric regime value (0/1/2) for external reference
                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Regime");
            }
            else if (State == State.DataLoaded)
            {
                _er     = new Series<double>(this);
                _atr    = new Series<double>(this);
                _atrAvg = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            // ── 5-minute chart guard ──────────────────────────────────
            // Calibrated for 5-min FDAX only. Show clear warning on other TFs.
            if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 5)
            {
                Draw.TextFixed(this, "RegimeLabel",
                    "⚠ FDAX Indicators optimised for 5-Min chart only",
                    TextPosition.TopLeft, Brushes.OrangeRed,
                    new Gui.Tools.SimpleFont("Consolas", 13),
                    Brushes.Transparent, Brushes.Transparent, 0);
                return;
            }

            if (CurrentBar < ATR_SLOW + 5) return;

            // ── Efficiency Ratio ──────────────────────────────────────
            // Low ER = choppy/auction.  High ER = directional trend.
            double netMove   = Math.Abs(Close[0] - Close[ErPeriod]);
            double totalPath = 0;
            for (int i = 0; i < ErPeriod; i++)
                totalPath += Math.Abs(Close[i] - Close[i + 1]);
            double er = totalPath > 0 ? netMove / totalPath : 0;
            _er[0] = er;

            // ── ATR ratio ─────────────────────────────────────────────
            // >1.05 = volatility expanding — confirms trending behaviour
            double atr    = ATR(AtrPeriod)[0];
            double atrAvg = 0;
            for (int i = 0; i < ATR_SLOW; i++)
                atrAvg += ATR(AtrPeriod)[i];
            atrAvg /= ATR_SLOW;
            double atrRatio = atrAvg > 0 ? atr / atrAvg : 1.0;

            // ── Directional consistency ───────────────────────────────
            // % of last DIR_PERIOD bars that closed above their prior close
            int upBars = 0;
            for (int i = 0; i < DIR_PERIOD; i++)
                if (Close[i] > Close[i + 1]) upBars++;
            double dirConsistency = upBars / (double)DIR_PERIOD;

            // ── Classify regime ───────────────────────────────────────
            // Trend requires BOTH high ER AND expanding ATR.
            // If either condition is missing → Auction.
            int  regime;
            bool isEfficient = er       > ER_THRESHOLD;
            bool isExpanded  = atrRatio > ATR_THRESHOLD;

            if      (isEfficient && isExpanded && dirConsistency > DIR_UP)   regime = 1;  // Trend Up
            else if (isEfficient && isExpanded && dirConsistency < DIR_DOWN) regime = 2;  // Trend Down
            else                                                              regime = 0;  // Auction

            Values[0][0] = regime;

            // ── Track consecutive bars in current regime ──────────────
            if (CurrentBar > 1 && (int)Values[0][1] == regime)
                _barsOnCurrentRegime++;
            else
                _barsOnCurrentRegime = 1;

            // ── Background coloring ───────────────────────────────────
            // Subtle tint: green = Trend Up, red = Trend Down, clear = Auction
            switch (regime)
            {
                case 1:   BackBrushes[0] = new SolidColorBrush(Color.FromArgb(25, 0, 200, 80));   break;
                case 2:   BackBrushes[0] = new SolidColorBrush(Color.FromArgb(25, 220, 50, 50));  break;
                default:  BackBrushes[0] = Brushes.Transparent;                                   break;
            }

            // ── TopLeft debug panel ───────────────────────────────────
            if (ShowDebugPanel)
            {
                string regimeName;
                Brush  labelColor;
                switch (regime)
                {
                    case 1:  regimeName = "TREND  ▲"; labelColor = Brushes.LimeGreen; break;
                    case 2:  regimeName = "TREND  ▼"; labelColor = Brushes.OrangeRed; break;
                    default: regimeName = "AUCTION";  labelColor = Brushes.Yellow;    break;
                }

                string panelText = string.Format(
                    "REGIME   : {0}  [{1} bars]\n" +
                    "ER       : {2:F2}  (trend > {3:F2})\n" +
                    "ATR Ratio: {4:F2}  (expand > {5:F2})\n" +
                    "Dir %    : {6:P0} up",
                    regimeName, _barsOnCurrentRegime,
                    er,       ER_THRESHOLD,
                    atrRatio, ATR_THRESHOLD,
                    dirConsistency);

                Draw.TextFixed(this, "RegimeLabel", panelText,
                    TextPosition.TopRight, labelColor,
                    new Gui.Tools.SimpleFont("Consolas", 12),
                    Brushes.Transparent, Brushes.Transparent, 0);
            }
            else
            {
                // Panel off — just show the regime name compactly
                string compactName = regime == 1 ? "REGIME: TREND ▲"
                                   : regime == 2 ? "REGIME: TREND ▼"
                                   : "REGIME: AUCTION";
                Brush compactColor = regime == 1 ? Brushes.LimeGreen
                                   : regime == 2 ? Brushes.OrangeRed : Brushes.Yellow;
                Draw.TextFixed(this, "RegimeLabel", compactName,
                    TextPosition.TopRight, compactColor,
                    new Gui.Tools.SimpleFont("Consolas", 12),
                    Brushes.Transparent, Brushes.Transparent, 0);
            }

            // ── Regime change tick ────────────────────────────────────
            // Short horizontal line anchored to the signal candle.
            // Spans 3 bars (2 bars before → current bar) so it does NOT
            // project forward and does not clutter future price action.
            if (CurrentBar > 1 && Values[0][0] != Values[0][1])
            {
                string tag    = "RegimeChange_" + CurrentBar;
                Brush lineClr = regime == 0 ? Brushes.Yellow :
                                regime == 1 ? Brushes.LimeGreen : Brushes.OrangeRed;
                double price  = regime == 2 ? High[0] + 4.0 : Low[0] - 4.0;

                // barsAgo 2 → 0: 3-bar-wide tick, left-anchored, no forward projection
                Draw.Line(this, tag, false,
                    2, price,
                    0, price,
                    lineClr, DashStyleHelper.Solid, 2);
            }
        }

        // ── Public accessors — readable by other indicators ───────────
        /// <summary>Returns 0=Auction, 1=TrendUp, 2=TrendDown</summary>
        public int CurrentRegime
        {
            get
            {
                if (Values[0].Count == 0) return 0;
                return (int)Values[0][0];
            }
        }

        /// <summary>Returns human-readable regime name for debug panels</summary>
        public string CurrentRegimeName
        {
            get
            {
                int r = CurrentRegime;
                return r == 1 ? "TREND ▲" : (r == 2 ? "TREND ▼" : "AUCTION");
            }
        }
    }
}

/*
=================================================================
REGIME GUIDE

AUCTION (clear background):
  → Both long and short reversal setups valid
  → This is the best regime for ReversalScorer and SweepClassifier signals

TREND UP (green background):
  → Long pullback entries only — no short reversals
  → Ignore ReversalScorer SHORT signals in this regime

TREND DOWN (red background):
  → Short pullback entries only — no long reversals
  → Ignore ReversalScorer LONG signals in this regime

REGIME CHANGE ticks (short 3-bar horizontal line):
  Yellow = to Auction  →  start looking for reversals
  Green  = to Trend Up →  long-only mode
  Red    = to Trend Dn →  short-only mode
=================================================================
*/

