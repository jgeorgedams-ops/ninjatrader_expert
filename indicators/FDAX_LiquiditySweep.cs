using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// FDAX_LiquiditySweep — detects liquidity sweep events on FDAX 5-minute bars.
    ///
    /// A sweep occurs when price spikes through a confirmed fractal swing high or low,
    /// triggers stop orders, then reverses and closes back on the correct side of the level.
    /// Signals are scored 3–10 based on confluence and displayed with direction-aware
    /// colour coding directly on the price pane.
    ///
    /// Swing confirmation has a built-in lag of SwingStrength bars — this is correct.
    /// </summary>
    public class FDAX_LiquiditySweep : Indicator
    {
        // ── Private state ─────────────────────────────────────────────────────

        private bool _debugMode = true;

        // Most recent confirmed fractal swing levels
        private double _lastSwingHigh     = 0;
        private double _lastSwingLow      = 0;
        private int    _lastSwingHighBar  = -1; // CurrentBar when swing high was confirmed
        private int    _lastSwingLowBar   = -1; // CurrentBar when swing low was confirmed

        // Tags for the current swing level lines (replaced when a new swing is confirmed)
        private string _swingHighTag = "SwingHigh";
        private string _swingLowTag  = "SwingLow";

        // Last signal state — used by the debug panel
        private string _lastSignalDir   = "—";
        private double _lastSignalScore = 0;
        private string _lastSignalBand  = "—";

        // ── User-configurable parameters ──────────────────────────────────────

        /// <summary>Number of bars on each side required to confirm a fractal swing point.</summary>
        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "Swing Strength", Order = 1, GroupName = "Swing Detection")]
        public int SwingStrength { get; set; }

        /// <summary>
        /// Minimum pierce beyond the swing level to qualify as a sweep.
        /// Expressed in full FDAX price points (3.0 = 3 pts = 75 EUR at 25 EUR/pt).
        /// </summary>
        [NinjaScriptProperty]
        [Range(0.5, 20.0)]
        [Display(Name = "Min Sweep Points (FDAX pts)", Order = 1, GroupName = "Sweep Detection")]
        public double MinSweepPoints { get; set; }

        /// <summary>When false, only Good (>=5.0) and Strong (>=7.0) signals are drawn.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Show Weak Signals", Order = 2, GroupName = "Sweep Detection")]
        public bool ShowWeakSignals { get; set; }

        /// <summary>Show the debug panel at TopLeft of the chart.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Show Debug Panel", Order = 1, GroupName = "Display")]
        public bool ShowDebugPanel { get; set; }

        /// <summary>Prints bar-by-bar diagnostics to the Output window.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Debug Mode", Order = 2, GroupName = "Display")]
        public bool DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }

        // ── NT8 lifecycle ─────────────────────────────────────────────────────

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name               = "FDAX_LiquiditySweep";
                Description        = "Detects liquidity sweep events at confirmed fractal swing highs/lows on FDAX 5-min bars. Scores sweeps 3–10 and draws direction-aware arrow signals with optional entry lines.";
                Calculate          = Calculate.OnBarClose;
                IsOverlay          = true;
                BarsRequiredToPlot = 20; // overwritten in DataLoaded

                // No plot lines — all output via Draw methods.

                SwingStrength   = 3;
                MinSweepPoints  = 3.0;
                ShowWeakSignals = false;
                ShowDebugPanel  = true;
                _debugMode      = true;
            }
            else if (State == State.Configure)
            {
                // No additional data series needed.
            }
            else if (State == State.DataLoaded)
            {
                // Need SwingStrength bars on each side plus ATR warm-up (20 bars).
                BarsRequiredToPlot = SwingStrength * 2 + 20;
            }
            else if (State == State.Terminated)
            {
                // No events or timers to unsubscribe.
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar < BarsRequiredToPlot) return;

                // ── STEP 1: Fractal swing point detection ─────────────────────
                //
                // A fractal swing high is confirmed SwingStrength bars AFTER the
                // pivot bar. At the current bar, we examine the bar SwingStrength
                // bars ago as the candidate pivot.
                //
                // Swing High: High[SwingStrength] must be strictly greater than
                //   High[0]..High[SwingStrength-1] (right side, newer bars)
                //   AND High[SwingStrength+1]..High[SwingStrength*2] (left side, older bars)
                //
                // Swing Low: Low[SwingStrength] must be strictly less than all
                //   neighbours on both sides by the same logic.

                int s = SwingStrength;

                // ── Swing High check ──────────────────────────────────────────
                double candidateHigh = High[s];
                bool isSwingHigh = true;
                for (int j = 0; j < s; j++)
                {
                    if (High[j] >= candidateHigh) { isSwingHigh = false; break; }
                }
                if (isSwingHigh)
                {
                    for (int j = s + 1; j <= s * 2; j++)
                    {
                        if (High[j] >= candidateHigh) { isSwingHigh = false; break; }
                    }
                }

                if (isSwingHigh && candidateHigh != _lastSwingHigh)
                {
                    _lastSwingHigh    = candidateHigh;
                    _lastSwingHighBar = CurrentBar - s; // the actual pivot bar index

                    // Draw/replace the swing high reference line
                    RemoveDrawObject(_swingHighTag);
                    Draw.Line(this, _swingHighTag, false,
                        s + 10, _lastSwingHigh, -10, _lastSwingHigh,
                        Brushes.DimGray, DashStyleHelper.Dash, 1);
                }

                // ── Swing Low check ───────────────────────────────────────────
                double candidateLow = Low[s];
                bool isSwingLow = true;
                for (int j = 0; j < s; j++)
                {
                    if (Low[j] <= candidateLow) { isSwingLow = false; break; }
                }
                if (isSwingLow)
                {
                    for (int j = s + 1; j <= s * 2; j++)
                    {
                        if (Low[j] <= candidateLow) { isSwingLow = false; break; }
                    }
                }

                if (isSwingLow && candidateLow != _lastSwingLow)
                {
                    _lastSwingLow    = candidateLow;
                    _lastSwingLowBar = CurrentBar - s;

                    // Draw/replace the swing low reference line
                    RemoveDrawObject(_swingLowTag);
                    Draw.Line(this, _swingLowTag, false,
                        s + 10, _lastSwingLow, -10, _lastSwingLow,
                        Brushes.DimGray, DashStyleHelper.Dash, 1);
                }

                // ── STEP 2: Sweep detection ───────────────────────────────────
                double atr      = ATR(14)[0];
                double barDelta = (Close[0] - Open[0]) * Volume[0];

                // Bullish sweep (spike below swing low, close back above it)
                bool bullSweep = _lastSwingLow > 0
                              && Low[0]   < _lastSwingLow - MinSweepPoints
                              && Close[0] > _lastSwingLow
                              && barDelta >= 0;

                // Bearish sweep (spike above swing high, close back below it)
                bool bearSweep = _lastSwingHigh > 0
                              && High[0]  > _lastSwingHigh + MinSweepPoints
                              && Close[0] < _lastSwingHigh
                              && barDelta <= 0;

                // ── STEP 3: Scoring ───────────────────────────────────────────
                // Only score if we actually have a sweep to report.
                if (bullSweep || bearSweep)
                {
                    double swingLevel  = bullSweep ? _lastSwingLow : _lastSwingHigh;
                    double sweepDist   = bullSweep
                        ? _lastSwingLow  - Low[0]
                        : High[0] - _lastSwingHigh;

                    double score = 3.0; // base score

                    // +1 if sweep distance is more than 2× MinSweepPoints
                    if (sweepDist > MinSweepPoints * 2.0)
                        score += 1.0;

                    // +1 if current ATR is above the 20-bar average ATR
                    double atrAvg = 0;
                    for (int j = 0; j < 20; j++) atrAvg += ATR(14)[j];
                    atrAvg /= 20.0;
                    if (atr > atrAvg)
                        score += 1.0;

                    // +1 if swing level is within 5 points of a round number (divisible by 50)
                    double nearestRound = Math.Round(swingLevel / 50.0) * 50.0;
                    if (Math.Abs(swingLevel - nearestRound) <= 5.0)
                        score += 1.0;

                    // +1 if close is within 2 points of the swing level (clean reclaim)
                    if (Math.Abs(Close[0] - swingLevel) <= 2.0)
                        score += 1.0;

                    score = Math.Min(score, 10.0);

                    // ── Score band classification ─────────────────────────────
                    bool isStrong = score >= 7.0;
                    bool isGood   = !isStrong && score >= 5.0;
                    bool isWeak   = !isStrong && !isGood; // score 3.0–4.9

                    // Suppress weak signals if ShowWeakSignals is off
                    if (isWeak && !ShowWeakSignals)
                    {
                        DrawDebugPanel("Sweep filtered (Weak)");
                        return;
                    }

                    string bandLabel = isStrong ? "Strong" : (isGood ? "Good" : "Weak");

                    // ── Colour selection: direction × band ────────────────────
                    Brush arrowBrush;
                    if (bullSweep)
                        arrowBrush = isStrong ? Brushes.LimeGreen
                                   : isGood   ? Brushes.Green
                                              : Brushes.OliveDrab;
                    else
                        arrowBrush = isStrong ? Brushes.Red
                                   : isGood   ? Brushes.Orange
                                              : Brushes.Gold;

                    // ── Entry price (rounded to nearest whole FDAX point) ──────
                    double entryPrice = Math.Round(Close[0], 0);

                    // ── Arrow ─────────────────────────────────────────────────
                    string arrowTag = "SweepArrow_" + CurrentBar.ToString();
                    if (bullSweep)
                        Draw.ArrowUp(this, arrowTag, false, 0,
                            Low[0] - atr * 0.4, arrowBrush);
                    else
                        Draw.ArrowDown(this, arrowTag, false, 0,
                            High[0] + atr * 0.4, arrowBrush);

                    // ── Entry line on Good and Strong signals only ─────────────
                    if (isGood || isStrong)
                    {
                        string lineTag   = "EntryLine_" + CurrentBar.ToString();
                        Brush  lineBrush = bullSweep ? Brushes.Green : Brushes.Red;
                        Draw.Line(this, lineTag, false,
                            3, entryPrice, -3, entryPrice,
                            lineBrush, DashStyleHelper.Dash, 2);
                    }

                    // ── Entry price label on all signal strengths ─────────────
                    string lblTag = "EntryLbl_" + CurrentBar.ToString();
                    if (bullSweep)
                    {
                        // Gray-green: Color.FromArgb(180, 100, 200, 100)
                        Brush lblBrush = new SolidColorBrush(
                            Color.FromArgb(180, 100, 200, 100));
                        Draw.Text(this, lblTag, false,
                            ((int)entryPrice).ToString(),
                            0, Close[0] + 1.0,
                            0, lblBrush,
                            new SimpleFont("Arial", 8),
                            System.Windows.TextAlignment.Center,
                            Brushes.Transparent, Brushes.Transparent, 0);
                    }
                    else
                    {
                        // Gray-red: Color.FromArgb(180, 200, 100, 100)
                        Brush lblBrush = new SolidColorBrush(
                            Color.FromArgb(180, 200, 100, 100));
                        Draw.Text(this, lblTag, false,
                            ((int)entryPrice).ToString(),
                            0, Close[0] - 1.0,
                            0, lblBrush,
                            new SimpleFont("Arial", 8),
                            System.Windows.TextAlignment.Center,
                            Brushes.Transparent, Brushes.Transparent, 0);
                    }

                    // ── Background tint on the sweep bar ─────────────────────
                    if (bullSweep)
                        BackBrushes[0] = new SolidColorBrush(
                            Color.FromArgb(30, 0, 255, 100));
                    else
                        BackBrushes[0] = new SolidColorBrush(
                            Color.FromArgb(30, 255, 50, 50));

                    // ── Update debug state ────────────────────────────────────
                    _lastSignalDir   = bullSweep ? "LONG" : "SHORT";
                    _lastSignalScore = score;
                    _lastSignalBand  = bandLabel;

                    if (_debugMode)
                        Print(string.Format(
                            "[{0}] Bar={1} {2} sweep | Level={3:F1} Dist={4:F1} " +
                            "Score={5:F1} ({6}) Entry={7} ATR={8:F1}",
                            Name, CurrentBar,
                            bullSweep ? "BULL" : "BEAR",
                            swingLevel, sweepDist,
                            score, bandLabel,
                            (int)entryPrice, atr));
                }

                DrawDebugPanel(bullSweep ? "BULL sweep fired" : bearSweep ? "BEAR sweep fired" : "No sweep");
            }
            catch (Exception ex)
            {
                Print("ERROR in " + Name + ": " + ex.Message);
            }
        }

        // ── Debug panel ───────────────────────────────────────────────────────

        private void DrawDebugPanel(string barStatus)
        {
            if (!ShowDebugPanel) return;

            int highBarsAgo = _lastSwingHighBar >= 0 ? CurrentBar - _lastSwingHighBar : -1;
            int lowBarsAgo  = _lastSwingLowBar  >= 0 ? CurrentBar - _lastSwingLowBar  : -1;

            string panel = string.Format(
                "── LIQUIDITY SWEEP ──────────────────\n" +
                "Swing High    : {0}  ({1})\n" +
                "Swing Low     : {2}  ({3})\n" +
                "Bar Status    : {4}\n" +
                "Last Score    : {5}  ({6})\n" +
                "Last Signal   : {7}",
                _lastSwingHigh    > 0 ? ((int)_lastSwingHigh).ToString()  : "—",
                highBarsAgo       >= 0 ? highBarsAgo + " bars ago"         : "—",
                _lastSwingLow     > 0 ? ((int)_lastSwingLow).ToString()   : "—",
                lowBarsAgo        >= 0 ? lowBarsAgo  + " bars ago"         : "—",
                barStatus,
                _lastSignalScore  > 0 ? _lastSignalScore.ToString("F1")   : "—",
                _lastSignalBand,
                _lastSignalDir);

            Draw.TextFixed(this, "SweepDebug", panel,
                TextPosition.TopLeft, Brushes.WhiteSmoke,
                new SimpleFont("Consolas", 10),
                Brushes.Black, Brushes.Black, 80);
        }
    }
}
