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
    /// FDAX_SignalOverlay — price-pane overlay that replicates two signal types
    /// from the Python ruflo-trading system onto the NinjaTrader chart:
    ///
    ///   1. Delta Divergence — orange SD/BD text labels (bear and bull)
    ///   2. Liquidity Sweep  — yellow arrow up / arrow down
    ///
    /// Designed for 5-minute FDAX bars. Cumulative delta accumulators reset on
    /// Bars.IsFirstBarOfSession (07:00 CET per the FDAX session template).
    /// SweepMinPoints is expressed in full FDAX price points (e.g. 2.0 = 2 pts).
    /// </summary>
    public class FDAX_SignalOverlay : Indicator
    {
        // ── Private state ─────────────────────────────────────────────────────

        private bool _debugMode = true;

        // Cumulative delta stored as a Series so we can index back DivergenceLookback bars.
        // Proxy formula: bar_delta = (Close - Open) × Volume (no tick data required).
        private Series<double> _cumDelta;
        private double         _runningCumDelta;

        // ── User-configurable parameters ──────────────────────────────────────

        /// <summary>Number of bars to look back for delta divergence detection.</summary>
        [NinjaScriptProperty]
        [Range(2, 50)]
        [Display(Name = "Divergence Lookback", Order = 1, GroupName = "Delta Divergence")]
        public int DivergenceLookback { get; set; }

        /// <summary>Enable or disable delta divergence labels.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Show Divergence", Order = 2, GroupName = "Delta Divergence")]
        public bool ShowDivergence { get; set; }

        /// <summary>Bars used to identify the swing high and swing low reference levels.</summary>
        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Swing Lookback", Order = 1, GroupName = "Sweeps")]
        public int SwingLookback { get; set; }

        /// <summary>
        /// Minimum pierce beyond the swing level to qualify as a sweep.
        /// Expressed in full FDAX price points (2.0 = 2 points = 50 EUR at 25 EUR/pt).
        /// </summary>
        [NinjaScriptProperty]
        [Range(0.5, 50.0)]
        [Display(Name = "Sweep Min Points (FDAX pts)", Order = 2, GroupName = "Sweeps")]
        public double SweepMinPoints { get; set; }

        /// <summary>Enable or disable liquidity sweep arrows.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Show Sweeps", Order = 3, GroupName = "Sweeps")]
        public bool ShowSweeps { get; set; }

        /// <summary>Prints bar-by-bar diagnostic values to the Output window.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Debug Mode", Order = 1, GroupName = "General")]
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
                Name               = "FDAX_SignalOverlay";
                Description        = "Overlay combining delta divergence (orange SD/BD labels) and liquidity sweep (yellow arrows) signals. Ported from the Python ruflo-trading system.";
                Calculate          = Calculate.OnBarClose;
                IsOverlay          = true;
                BarsRequiredToPlot = 21; // placeholder — overwritten in DataLoaded

                // No plot lines. All output uses Draw.ArrowUp, Draw.ArrowDown, Draw.Text.

                DivergenceLookback = 8;
                ShowDivergence     = true;
                SwingLookback      = 20;
                SweepMinPoints     = 2.0;
                ShowSweeps         = true;
                _debugMode         = true;
            }
            else if (State == State.Configure)
            {
                // No additional data series needed.
            }
            else if (State == State.DataLoaded)
            {
                // Infinite lookback so _cumDelta[j] is always valid up to DivergenceLookback.
                _cumDelta        = new Series<double>(this, MaximumBarsLookBack.Infinite);
                _runningCumDelta = 0;

                BarsRequiredToPlot = Math.Max(DivergenceLookback, SwingLookback) + 1;
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

                // ── Session reset ─────────────────────────────────────────────
                // Fires on the first bar of each session per the instrument's NT8
                // session template (07:00 CET for FDAX).
                if (Bars.IsFirstBarOfSession)
                    _runningCumDelta = 0;

                // ── Bar delta proxy ───────────────────────────────────────────
                // Python source: bar_delta = (Close - Open) * Volume
                // Positive on bullish bars, negative on bearish bars.
                // True delta requires tick data; this proxy is sufficient for divergence.
                double barDelta   = (Close[0] - Open[0]) * Volume[0];
                _runningCumDelta += barDelta;
                _cumDelta[0]      = _runningCumDelta;

                // ── Swing reference levels ────────────────────────────────────
                // MAX/MIN in NT8 includes the current bar (index 0), giving a window
                // of SwingLookback bars ending at the current bar.
                double swingHigh = MAX(High, SwingLookback)[0];
                double swingLow  = MIN(Low,  SwingLookback)[0];

                // ═════════════════════════════════════════════════════════════
                // SIGNAL 1 — Delta Divergence  (orange "BD" text labels)
                //
                // Bear divergence: current High is at or above the window's highest
                //   High, but cumulative delta is BELOW its window maximum — price
                //   made a new high but buying pressure is weakening.
                //
                // Bull divergence: current Low is at or below the window's lowest
                //   Low, but cumulative delta is ABOVE its window minimum — price
                //   made a new low but selling pressure is weakening.
                //
                // Bear divergence draws "SD" (supply divergence), bull draws "BD" (bull divergence),
                // matching the Python ruflo-trading source labels.
                // ═════════════════════════════════════════════════════════════
                if (ShowDivergence && CurrentBar >= DivergenceLookback)
                {
                    // Walk the lookback window to find cumulative delta extremes.
                    // _cumDelta[0] = current bar; _cumDelta[DivergenceLookback] = oldest bar.
                    double maxCumDelta = _cumDelta[0];
                    double minCumDelta = _cumDelta[0];
                    for (int j = 1; j <= DivergenceLookback; j++)
                    {
                        if (_cumDelta[j] > maxCumDelta) maxCumDelta = _cumDelta[j];
                        if (_cumDelta[j] < minCumDelta) minCumDelta = _cumDelta[j];
                    }

                    double highestHigh = MAX(High, DivergenceLookback)[0];
                    double lowestLow   = MIN(Low,  DivergenceLookback)[0];

                    // Bear divergence
                    if (High[0] >= highestHigh && _cumDelta[0] < maxCumDelta)
                    {
                        Draw.Text(this,
                            "BearDiv_" + CurrentBar.ToString(),
                            false,
                            "SD",
                            0,
                            High[0] + 4 * TickSize,
                            0,
                            Brushes.Orange,
                            new SimpleFont("Arial", 8),
                            System.Windows.TextAlignment.Center,
                            Brushes.Transparent,
                            Brushes.Transparent,
                            0);
                    }

                    // Bull divergence
                    if (Low[0] <= lowestLow && _cumDelta[0] > minCumDelta)
                    {
                        Draw.Text(this,
                            "BullDiv_" + CurrentBar.ToString(),
                            false,
                            "BD",
                            0,
                            Low[0] - 4 * TickSize,
                            0,
                            Brushes.Orange,
                            new SimpleFont("Arial", 8),
                            System.Windows.TextAlignment.Center,
                            Brushes.Transparent,
                            Brushes.Transparent,
                            0);
                    }
                }

                // ═════════════════════════════════════════════════════════════
                // SIGNAL 2 — Liquidity Sweep  (yellow arrows)
                //
                // SweepMinPoints is in full FDAX price points.
                // FDAX tick size = 0.5 pts; SweepMinPoints = 5.0 means the wick
                // must extend at least 5 full points beyond the swing level.
                //
                // Long sweep:  Low pierces BELOW (swingLow − SweepMinPoints),
                //              Close recovers back ABOVE swingLow,
                //              barDelta >= 0 (neutral or net buying).
                //
                // Short sweep: High pierces ABOVE (swingHigh + SweepMinPoints),
                //              Close falls back BELOW swingHigh,
                //              barDelta <= 0 (neutral or net selling).
                // ═════════════════════════════════════════════════════════════
                if (ShowSweeps)
                {
                    // Long sweep
                    bool longPierce  = Low[0]   < swingLow  - SweepMinPoints;
                    bool longRecover = Close[0]  > swingLow;
                    bool longDelta   = barDelta >= 0;

                    if (longPierce && longRecover && longDelta)
                    {
                        Draw.ArrowUp(this,
                            "Sweep_L_" + CurrentBar.ToString(),
                            false,
                            0,
                            Low[0] - 4 * TickSize,
                            Brushes.Yellow);
                    }

                    // Short sweep
                    bool shortPierce  = High[0]  > swingHigh + SweepMinPoints;
                    bool shortRecover = Close[0]  < swingHigh;
                    bool shortDelta   = barDelta <= 0;

                    if (shortPierce && shortRecover && shortDelta)
                    {
                        Draw.ArrowDown(this,
                            "Sweep_S_" + CurrentBar.ToString(),
                            false,
                            0,
                            High[0] + 4 * TickSize,
                            Brushes.Yellow);
                    }
                }

                // ── Debug output ──────────────────────────────────────────────
                if (_debugMode)
                    Print(string.Format(
                        "[{0}] Bar={1} Close={2:F1} CumDelta={3:F0} " +
                        "BarDelta={4:F0} SwHi={5:F1} SwLo={6:F1}",
                        Name, CurrentBar, Close[0],
                        _runningCumDelta, barDelta, swingHigh, swingLow));
            }
            catch (Exception ex)
            {
                Print("ERROR in " + Name + ": " + ex.Message);
            }
        }
    }
}
