using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// FDAX_SessionVWAP — session VWAP with one standard deviation bands and
    /// mean-reversion arrow signals. Accumulators reset on the first bar of each
    /// trading session as defined by the instrument's NT8 session template
    /// (07:00 – 17:30 CET for FDAX). Designed for 5-minute bars on FDAX.
    /// </summary>
    public class FDAX_SessionVWAP : Indicator
    {
        // --- Private state ---------------------------------------------------
        private bool   _debugMode      = true;
        private double _cumulativeTPV;   // Σ (TypicalPrice × Volume)
        private double _cumulativeVol;   // Σ Volume
        private double _cumulativeTP2V; // Σ (TypicalPrice² × Volume) — needed for running variance
        private bool   _wasAboveUpper;  // close was above upper band on previous bar
        private bool   _wasBelowLower;  // close was below lower band on previous bar

        // --- User-configurable properties ------------------------------------

        /// <summary>Multiplier applied to the session standard deviation to position the bands.</summary>
        [NinjaScriptProperty]
        [Range(0.1, 5.0)]
        [Display(Name = "Std Dev Multiplier", Order = 1, GroupName = "Parameters")]
        public double StdDevMultiplier { get; set; }

        /// <summary>When true, prints bar-by-bar VWAP diagnostics to the Output window.</summary>
        [NinjaScriptProperty]
        [Display(Name = "Debug Mode", Order = 2, GroupName = "Parameters")]
        public bool DebugMode
        {
            get { return _debugMode; }
            set { _debugMode = value; }
        }

        // --- NT8 lifecycle ---------------------------------------------------

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name               = "FDAX_SessionVWAP";
                Description        = "Session VWAP with ±1 SD bands and mean-reversion signals. Resets at each session open (07:00 CET for FDAX).";
                Calculate          = Calculate.OnBarClose;
                IsOverlay          = true;
                BarsRequiredToPlot = 1;

                // Plot 0: VWAP — colour is set dynamically via PlotBrushes
                AddPlot(new Stroke(Brushes.White, 2), PlotStyle.Line, "VWAP");
                // Plot 1: Upper standard deviation band
                AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "UpperBand");
                // Plot 2: Lower standard deviation band
                AddPlot(new Stroke(Brushes.DimGray, 1), PlotStyle.Line, "LowerBand");

                StdDevMultiplier = 1.0;
                _debugMode       = true;
            }
            else if (State == State.Configure)
            {
                // No additional data series needed for a single-timeframe VWAP.
            }
            else if (State == State.DataLoaded)
            {
                // Initialise running accumulators — they will also be reset on
                // Bars.IsFirstBarOfSession inside OnBarUpdate.
                ResetAccumulators();
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

                // ── Session reset ────────────────────────────────────────────
                // Bars.IsFirstBarOfSession is true for the first bar delivered
                // after the session gap as defined by the instrument's session
                // template in NT8 (07:00 CET for FDAX).
                if (Bars.IsFirstBarOfSession)
                    ResetAccumulators();

                // ── Accumulate VWAP components ───────────────────────────────
                double tp  = (High[0] + Low[0] + Close[0]) / 3.0; // typical price
                double vol = Volume[0];

                _cumulativeTPV  += tp  * vol;
                _cumulativeVol  += vol;
                _cumulativeTP2V += tp  * tp * vol;

                // Guard against bars with zero volume (gap bars, synthetic data)
                if (_cumulativeVol <= 0)
                    return;

                // ── VWAP and band calculation ────────────────────────────────
                double vwap     = _cumulativeTPV / _cumulativeVol;

                // Running variance: Var = E[X²] − (E[X])²
                // where X is the volume-weighted typical price.
                double variance = (_cumulativeTP2V / _cumulativeVol) - (vwap * vwap);
                double stdDev   = variance > 0 ? Math.Sqrt(variance) : 0;

                double upper = vwap + StdDevMultiplier * stdDev;
                double lower = vwap - StdDevMultiplier * stdDev;

                // ── Assign plots ─────────────────────────────────────────────
                VWAP[0]      = vwap;
                UpperBand[0] = upper;
                LowerBand[0] = lower;

                // ── VWAP line colour — green above, red below ─────────────────
                PlotBrushes[0][0] = Close[0] >= vwap ? Brushes.LimeGreen : Brushes.Red;

                // ── Reversion signal arrows ───────────────────────────────────
                // We need the state from the previous bar to detect the crossing.
                // Skip the very first bar where _wasAbove/Below are both false
                // and no prior state exists.
                if (CurrentBar > BarsRequiredToPlot)
                {
                    bool nowInside = Close[0] >= lower && Close[0] <= upper;

                    // Bullish reversion: price was below lower band, now back inside bands
                    if (_wasBelowLower && nowInside)
                    {
                        Draw.ArrowUp(this,
                            "RevUp_" + CurrentBar.ToString(),
                            false,          // autoScale
                            0,              // barsAgo — current bar
                            Low[0] - 2 * TickSize,
                            Brushes.LimeGreen);
                    }
                    // Bearish reversion: price was above upper band, now back inside bands
                    else if (_wasAboveUpper && nowInside)
                    {
                        Draw.ArrowDown(this,
                            "RevDn_" + CurrentBar.ToString(),
                            false,
                            0,
                            High[0] + 2 * TickSize,
                            Brushes.Red);
                    }
                }

                // Update band-breach state for the next bar's comparison
                _wasAboveUpper = Close[0] > upper;
                _wasBelowLower = Close[0] < lower;

                // ── Debug output ─────────────────────────────────────────────
                if (_debugMode)
                    Print(string.Format("[{0}] Bar={1} VWAP={2:F2} Upper={3:F2} Lower={4:F2} Close={5:F2}",
                        Name, CurrentBar, vwap, upper, lower, Close[0]));
            }
            catch (Exception ex)
            {
                Print("ERROR in " + Name + ": " + ex.Message);
            }
        }

        // --- Helpers ---------------------------------------------------------

        private void ResetAccumulators()
        {
            _cumulativeTPV  = 0;
            _cumulativeVol  = 0;
            _cumulativeTP2V = 0;
            _wasAboveUpper  = false;
            _wasBelowLower  = false;
        }

        // --- Named plot accessors (allow other scripts to consume this indicator) ---

        /// <summary>Session VWAP. Painted LimeGreen when price is above, Red when below.</summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> VWAP
        {
            get { Update(); return Values[0]; }
        }

        /// <summary>Upper standard deviation band (VWAP + N × σ).</summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> UpperBand
        {
            get { Update(); return Values[1]; }
        }

        /// <summary>Lower standard deviation band (VWAP − N × σ).</summary>
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> LowerBand
        {
            get { Update(); return Values[2]; }
        }
    }
}
