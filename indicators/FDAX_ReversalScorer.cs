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
// FDAX REVERSAL SCORER — v3
// ============================================================
// Scores reversal setups 0–100 when price is near a swing level.
// Score is DISPLAYED as 0–10 (divide by 10) throughout labels
// and the debug panel, matching the threshold parameter scale.
//
// DIRECTION LOGIC:
//   isBullishSetup = price is closer to the swing LOW
//                 = testing SUPPORT from above = LONG signal
//   !isBullishSetup = price is closer to the swing HIGH
//                  = testing RESISTANCE from below = SHORT signal
//   NOTE: This is MORE reliable than Close>Open (bar colour) for
//         reversals — a rejection candle at support often has a
//         bearish (red) body yet is still a LONG setup.
//
// CIRCLE PLACEMENT:
//   LONG  circles drawn ABOVE the signal bar
//   SHORT circles drawn BELOW the signal bar
//
// CIRCLE COLOURS (fully configurable):
//   LONG  Strong / Good / Weak = green shades
//   SHORT Strong / Good / Weak = red/orange shades
//
// SCORE THRESHOLDS (0–10 scale, configurable):
//   Strong (default 7.0) → CONFIRMED — draw entry/exit levels + alert
//   Good   (default 5.0) → BUILDING  — show warning text
//   Weak   (default 3.0) → BUILDING  — show faint warning
//   Below Weak           → no circle at all
//
// FDAX FIXED LEVELS:
//   T1   = entry ± 5 pts   = €125/contract
//   T2   = entry ± 10 pts  = €250/contract
//   Stop = entry ∓ 15 pts  = €375/contract
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators
{
    public class FDAX_ReversalScorer : Indicator
    {
        // ── FDAX fixed-point trade levels ─────────────────────────────
        private const double T1_POINTS   = 5.0;   // Target 1:  +5pts = €125
        private const double T2_POINTS   = 10.0;  // Target 2: +10pts = €250
        private const double STOP_POINTS = 15.0;  // Fixed stop: 15pts = €375

        // ── Auto-cleanup: remove level lines after this many bars ──────
        // 24 bars = 2 hours on a 5-min chart
        private const int LINE_AUTO_REMOVE = 24;

        // ── Score calculation parameters (internal 0–100 scale) ────────
        private const double PROXIMITY_ATR = 0.35;   // must be within 0.35 ATR of a level
        private const int    ATR_PERIOD    = 14;
        private const int    VOL_PERIOD    = 20;
        private const int    ER_PERIOD     = 10;
        private const int    DIR_PERIOD    = 10;

        // ── State machine ──────────────────────────────────────────────
        private enum SetupState { None, Building, Confirmed, Failed }
        private SetupState _setupState     = SetupState.None;
        private bool       _isLong         = false;
        private double     _entryPrice     = 0;
        private double     _stopPrice      = 0;
        private double     _t1Price        = 0;
        private double     _t2Price        = 0;
        private int        _signalBar      = -1;
        private bool       _t1Hit          = false;
        private bool       _failureFlagged = false;
        private string     _lastDirection  = "NONE";
        private string     _lastBand       = "—";
        private DateTime   _lastAlertTime  = DateTime.MinValue;
        private double     _lastScore      = 0;
        private string     _priorResult    = "—";

        // Tags for the active signal's level lines (used for cleanup)
        private string _tagEntry = "";
        private string _tagT1    = "";
        private string _tagT2    = "";
        private string _tagStop  = "";

        // ══════════════════════════════════════════════════════════════
        // USER-CONFIGURABLE PARAMETERS
        // ══════════════════════════════════════════════════════════════

        // ── Score thresholds (0–10 scale) ─────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "Strong Threshold (0-10)", Order = 1, GroupName = "Score Thresholds",
                 Description = "Score at or above = Strong. CONFIRMED state fires. Default 7.0")]
        public double StrongThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Good Threshold (0-10)", Order = 2, GroupName = "Score Thresholds",
                 Description = "Score at or above = Good. BUILDING state. Default 5.0")]
        public double GoodThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Weak Threshold (0-10)", Order = 3, GroupName = "Score Thresholds",
                 Description = "Score at or above = Weak. Below this = no circle. Default 3.0")]
        public double WeakThreshold { get; set; }

        // ── Display parameters ────────────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "Swing Lookback", Order = 4, GroupName = "Display",
                 Description = "Number of bars to look back for swing high/low levels")]
        public int SwingLookback { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Score Text", Order = 5, GroupName = "Display",
                 Description = "Show score text label next to each circle")]
        public bool ShowText { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Circle Size (1=Small 2=Med 3=Large)", Order = 6, GroupName = "Display",
                 Description = "Controls label text size. 1=Small 2=Medium 3=Large. Note: dot size is fixed in NT8.")]
        public int CircleSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Debug Panel", Order = 7, GroupName = "Display",
                 Description = "Show the FDAX DEBUG panel at TopRight")]
        public bool ShowDebugPanel { get; set; }

        // ── Long signal colours (not in generated code — use XmlIgnore) ─
        // LONG  = bullish reversal = price testing support from above

        [XmlIgnore]
        [Display(Name = "Long Strong Color", Order = 1, GroupName = "Long Colors")]
        public Brush LongStrongColor { get; set; }
        [Browsable(false)]
        public string LongStrongColorSerializable
        {
            get { return Serialize.BrushToString(LongStrongColor); }
            set { LongStrongColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Long Good Color", Order = 2, GroupName = "Long Colors")]
        public Brush LongGoodColor { get; set; }
        [Browsable(false)]
        public string LongGoodColorSerializable
        {
            get { return Serialize.BrushToString(LongGoodColor); }
            set { LongGoodColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Long Weak Color", Order = 3, GroupName = "Long Colors")]
        public Brush LongWeakColor { get; set; }
        [Browsable(false)]
        public string LongWeakColorSerializable
        {
            get { return Serialize.BrushToString(LongWeakColor); }
            set { LongWeakColor = Serialize.StringToBrush(value); }
        }

        // ── Short signal colours ──────────────────────────────────────
        // SHORT = bearish reversal = price testing resistance from below

        [XmlIgnore]
        [Display(Name = "Short Strong Color", Order = 1, GroupName = "Short Colors")]
        public Brush ShortStrongColor { get; set; }
        [Browsable(false)]
        public string ShortStrongColorSerializable
        {
            get { return Serialize.BrushToString(ShortStrongColor); }
            set { ShortStrongColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Good Color", Order = 2, GroupName = "Short Colors")]
        public Brush ShortGoodColor { get; set; }
        [Browsable(false)]
        public string ShortGoodColorSerializable
        {
            get { return Serialize.BrushToString(ShortGoodColor); }
            set { ShortGoodColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore]
        [Display(Name = "Short Weak Color", Order = 3, GroupName = "Short Colors")]
        public Brush ShortWeakColor { get; set; }
        [Browsable(false)]
        public string ShortWeakColorSerializable
        {
            get { return Serialize.BrushToString(ShortWeakColor); }
            set { ShortWeakColor = Serialize.StringToBrush(value); }
        }

        // ── MinScore kept for backward compatibility ──────────────────
        // Now controlled by WeakThreshold.  Hidden from properties panel.
        [Browsable(false)]
        public double MinScore { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description    = "FDAX Reversal Scorer — scores reversal setups 0–10";
                Name           = "FDAX_ReversalScorer";
                Calculate      = Calculate.OnBarClose;
                IsOverlay      = true;

                // Score thresholds (0–10 scale)
                StrongThreshold = 7.0;
                GoodThreshold   = 5.0;
                WeakThreshold   = 3.0;

                // Display
                SwingLookback  = 20;
                ShowText       = true;
                CircleSize     = 2;
                ShowDebugPanel = true;
                MinScore       = 30.0;  // internal fallback = WeakThreshold * 10

                // Long signal colours (green shades)
                LongStrongColor = Brushes.LimeGreen;
                LongGoodColor   = Brushes.Green;
                LongWeakColor   = Brushes.OliveDrab;

                // Short signal colours (red/orange shades)
                ShortStrongColor = Brushes.Red;
                ShortGoodColor   = Brushes.Orange;
                ShortWeakColor   = Brushes.Gold;

                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "Score");
            }
        }

        protected override void OnBarUpdate()
        {
            // ── 5-minute chart guard ──────────────────────────────────
            if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 5)
            {
                Draw.TextFixed(this, "RevDebug",
                    "⚠ FDAX Indicators optimised for 5-Min chart only",
                    TextPosition.TopRight, Brushes.OrangeRed,
                    new Gui.Tools.SimpleFont("Consolas", 12),
                    Brushes.Transparent, Brushes.Transparent, 0);
                return;
            }

            if (CurrentBar < Math.Max(SwingLookback, 60) + 5) return;

            // ── Track active confirmed signal — runs every bar ─────────
            // Catches stop hits and T1 hits regardless of whether a new
            // setup fires on the current bar.
            if (_setupState == SetupState.Confirmed && _signalBar != CurrentBar && !_failureFlagged)
            {
                if (!_t1Hit)
                {
                    // T1 check: if hit, stop tracking for failure
                    bool t1Reached = _isLong ? High[0] >= _t1Price : Low[0] <= _t1Price;
                    if (t1Reached)
                    {
                        _t1Hit       = true;
                        _priorResult = "T1 HIT +" + ((int)T1_POINTS) + "pts";
                    }
                    else
                    {
                        // Stop check: failure if stop hit before T1
                        bool stopHit = _isLong ? Low[0] <= _stopPrice : High[0] >= _stopPrice;
                        if (stopHit)
                        {
                            _setupState     = SetupState.Failed;
                            _failureFlagged = true;
                            _priorResult    = "FAILED @ " + ((int)_stopPrice);

                            // Log failure to NT8 Output window
                            Print(string.Format(
                                "[FDAX] FAILED SETUP | Direction: {0} | Entry: {1} | Stop Hit: {2} | Time: {3}",
                                _lastDirection, (int)_entryPrice, (int)_stopPrice,
                                Time[0].ToString("HH:mm")));
                        }
                    }
                }

                // Auto-remove level lines after LINE_AUTO_REMOVE bars with no outcome
                if (!_failureFlagged && CurrentBar - _signalBar >= LINE_AUTO_REMOVE)
                {
                    CleanupLevelLines();
                    _setupState = SetupState.None;
                }
            }

            // ── Feature 1: Efficiency Ratio ───────────────────────────
            // Low ER = choppy/auction = good for reversals
            double netMove   = Math.Abs(Close[0] - Close[ER_PERIOD]);
            double totalPath = 0.0001;
            for (int i = 0; i < ER_PERIOD; i++)
                totalPath += Math.Abs(Close[i] - Close[i + 1]);
            double er = Math.Min(netMove / totalPath, 1.0);

            // ── Feature 2: Distance to nearest swing level ────────────
            double swingHigh = double.MinValue;
            double swingLow  = double.MaxValue;
            for (int i = 1; i <= SwingLookback; i++)
            {
                if (High[i] > swingHigh) swingHigh = High[i];
                if (Low[i]  < swingLow)  swingLow  = Low[i];
            }

            double atr       = ATR(ATR_PERIOD)[0];
            double distHigh  = (swingHigh - Close[0]) / Math.Max(atr, 0.01);
            double distLow   = (Close[0]  - swingLow)  / Math.Max(atr, 0.01);
            double distLevel = Math.Min(distHigh, distLow);

            // Not near any level — update debug panel and exit
            if (distLevel > PROXIMITY_ATR)
            {
                _lastScore = 0;
                DrawDebugPanel();
                return;
            }

            // ── DIRECTION: proximity-based ────────────────────────────────────────
            // isBullishSetup = price closer to swing LOW  = testing support     = LONG
            // isBullishSetup = false = price closer to swing HIGH = testing resistance = SHORT
            bool isBullishSetup = distLow < distHigh;

            // ── Feature 3: ATR ratio ──────────────────────────────────
            double atrAvg = 0;
            for (int i = 0; i < 60; i++) atrAvg += ATR(ATR_PERIOD)[i];
            atrAvg /= 60.0;
            double atrRatio = atrAvg > 0 ? atr / atrAvg : 1.0;

            // ── Feature 4: Bar close position (0=bottom, 1=top) ───────
            double barRange = High[0] - Low[0];
            double closePos = barRange > 0 ? (Close[0] - Low[0]) / barRange : 0.5;

            // ── Feature 5: Directional consistency ────────────────────
            int upBars = 0;
            for (int i = 0; i < DIR_PERIOD; i++)
                if (Close[i] > Close[i + 1]) upBars++;
            double dirConsistency = upBars / (double)DIR_PERIOD;

            // ── Feature 6: Volume ratio ───────────────────────────────
            double volAvg = 0;
            for (int i = 0; i < VOL_PERIOD; i++) volAvg += Volume[i];
            volAvg /= VOL_PERIOD;
            double volRatio = volAvg > 0 ? Volume[0] / volAvg : 1.0;

            // ── Feature 7: Wick ratio ──────────────────────────────────
            double body      = Math.Abs(Close[0] - Open[0]);
            double wickRatio = barRange > 0 ? (barRange - body) / barRange : 0;

            // ── Score calculation (0–100 internal scale) ──────────────
            // Weighted combination of features from the Python GB model.
            double score = 0;

            // ER: low ER (auction) → better reversal conditions
            score += (1.0 - er) * 100 * 0.30;

            // Distance: closer to level → higher score
            score += Math.Max(0, (1.0 - distLevel / PROXIMITY_ATR)) * 100 * 0.25;

            // Directional alignment: LONG wants rising price (high dir %), SHORT wants falling
            double dirScore;
            if (isBullishSetup) dirScore = dirConsistency * 100;
            else                dirScore = (1.0 - dirConsistency) * 100;
            score += dirScore * 0.20;

            // ATR ratio: slight expansion is ideal
            double atrScoreV = 100 - Math.Abs(atrRatio - 1.0) * 60;
            score += Math.Max(0, Math.Min(100, atrScoreV)) * 0.10;

            // Close position: rejection wick = bullish has close near high
            double cpScore;
            if (isBullishSetup) cpScore = closePos * 100;
            else                cpScore = (1.0 - closePos) * 100;
            score += cpScore * 0.10;

            // Volume: above-average at level is confirmatory
            score += Math.Min(volRatio, 3.0) / 3.0 * 100 * 0.05;

            score        = Math.Max(0, Math.Min(100, score));
            Values[0][0] = score;
            _lastScore   = score;

            // ── Convert to 0–10 scale for threshold comparison ────────
            double score10 = score / 10.0;

            // ── Classify score band ───────────────────────────────────
            bool isStrong = score10 >= StrongThreshold;
            bool isGood   = !isStrong && score10 >= GoodThreshold;
            bool isWeak   = !isStrong && !isGood && score10 >= WeakThreshold;
            bool anySignal = isStrong || isGood || isWeak;

            if (!anySignal)
            {
                DrawDebugPanel();
                return;
            }

            // ── DIRECTION-AWARE COLOUR ASSIGNMENT ─────────────────────
            // FIX FOR PRIORITY BUG: colour is now determined by BOTH
            // direction (long/short) AND score band.
            // Previously colour was score-only — this caused green circles
            // on bearish (short) setups.
            Brush  dotBrush;
            string bandLabel;

            if (isStrong)
            {
                dotBrush  = isBullishSetup ? LongStrongColor : ShortStrongColor;
                bandLabel = "Strong";
            }
            else if (isGood)
            {
                dotBrush  = isBullishSetup ? LongGoodColor : ShortGoodColor;
                bandLabel = "Good";
            }
            else
            {
                dotBrush  = isBullishSetup ? LongWeakColor : ShortWeakColor;
                bandLabel = "Weak";
            }

            string dirLabel    = isBullishSetup ? "LONG" : "SHORT";
            string scoreDisplay = score10.ToString("F1");

            // ── CONFIRMED: draw fixed-point entry/exit levels ──────────
            if (isStrong && _signalBar != CurrentBar)
            {
                CleanupLevelLines();

                _isLong         = isBullishSetup;
                _entryPrice     = Math.Round(Close[0], 0);
                _signalBar      = CurrentBar;
                _setupState     = SetupState.Confirmed;
                _t1Hit          = false;
                _failureFlagged = false;
                _lastDirection  = dirLabel;
                _lastBand       = bandLabel;

                if (_isLong)
                {
                    _t1Price   = _entryPrice + T1_POINTS;
                    _t2Price   = _entryPrice + T2_POINTS;
                    _stopPrice = _entryPrice - STOP_POINTS;
                }
                else
                {
                    _t1Price   = _entryPrice - T1_POINTS;
                    _t2Price   = _entryPrice - T2_POINTS;
                    _stopPrice = _entryPrice + STOP_POINTS;
                }

                string pfx = "Rev_" + CurrentBar + "_";
                _tagEntry  = pfx + "Entry";
                _tagT1     = "";
                _tagT2     = "";
                _tagStop   = "";

                // Entry tick — short yellow dashed line anchored to signal candle, no forward projection
                Draw.Line(this, _tagEntry, false, 3, _entryPrice, 0, _entryPrice,
                    Brushes.Yellow, DashStyleHelper.Dash, 2);

                // Log confirmed setup to Output window
                Print(string.Format(
                    "[FDAX] CONFIRMED SETUP | Direction: {0} | Entry: {1} | Time: {2}",
                    _lastDirection, (int)_entryPrice, Time[0].ToString("HH:mm")));

                // ── NT8 alert sound (real-time bars only, 5s re-arm) ──
                if (State == State.Realtime
                    && (Time[0] - _lastAlertTime).TotalSeconds >= 5)
                {
                    Alert("RevConfirm", Priority.High,
                        dirLabel + " Reversal CONFIRMED at " + ((int)_entryPrice).ToString(),
                        NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav",
                        10, Brushes.LimeGreen, Brushes.Black);
                    _lastAlertTime = Time[0];
                }
            }

            // ── BUILDING: show warning text near the signal bar ────────
            // Score is Good or Weak — not yet Confirmed.
            if (!isStrong
                && _setupState != SetupState.Confirmed
                && _setupState != SetupState.Failed)
            {
                _setupState    = SetupState.Building;
                _lastDirection = dirLabel;
                _lastBand      = bandLabel;
                // No visual marker for Building state — dot only fires at Confirmed/score threshold
            }

            // ── Circle dot and optional label ─────────────────────────
            // Standard convention: LONG (buy) dot BELOW bar, SHORT (sell) dot ABOVE bar
            {
                double dotY, textY;
                if (isBullishSetup)
                {
                    // LONG — below bar (buy signal convention)
                    dotY  = Low[0]  - atr * 0.3;
                    textY = Low[0]  - atr * 0.7;
                }
                else
                {
                    // SHORT — above bar (sell signal convention)
                    dotY  = High[0] + atr * 0.3;
                    textY = High[0] + atr * 0.7;
                }

                Draw.Dot(this, "RevDot_" + CurrentBar, false, 0, dotY, dotBrush);

                if (ShowText)
                {
                    string circleLabel = dirLabel + " - " + bandLabel + " " + scoreDisplay;
                    Draw.Text(this, "RevTxt_" + CurrentBar, circleLabel, 0, textY, dotBrush);
                }
            }

            DrawDebugPanel();
        }

        // ── Remove the active signal's entry tick line ────────────────
        private void CleanupLevelLines()
        {
            if (!string.IsNullOrEmpty(_tagEntry))
            {
                RemoveDrawObject(_tagEntry);
                _tagEntry = "";
            }
        }

        // ── FDAX DEBUG panel at TopRight ───────────────────────────────
        // This is the "master" debug panel the user requested.
        // Shows all reversal scorer data. Regime shown as "—" unless
        // FDAX_RegimeDetector is loaded (read via try/catch).
        private void DrawDebugPanel()
        {
            if (!ShowDebugPanel) return;

            int    barsSince = _signalBar >= 0 ? CurrentBar - _signalBar : -1;
            string stateStr;
            switch (_setupState)
            {
                case SetupState.Building:  stateStr = "BUILDING";  break;
                case SetupState.Confirmed: stateStr = "CONFIRMED"; break;
                case SetupState.Failed:    stateStr = "FAILED";    break;
                default:                   stateStr = "NONE";      break;
            }

            // Try to read regime from FDAX_RegimeDetector (default params 10/14)
            // If not loaded or using different params → shows "—"
            string regimeStr = "—";
            try
            {
                var rd = FDAX_RegimeDetector(10, 14, true);
                if (rd != null && rd.Values[0].Count > 0)
                    regimeStr = rd.CurrentRegimeName;
            }
            catch { regimeStr = "—"; }

            string score10Str = _lastScore > 0 ? (_lastScore / 10.0).ToString("F1") : "—";
            string lastResult = _t1Hit ? "PASS" : (_setupState == SetupState.Failed ? "FAIL" : "PENDING");
            if (_priorResult != "—" && _priorResult.StartsWith("FAILED")) lastResult = "FAIL";
            if (_t1Hit) lastResult = "PASS";

            string panel = string.Format(
                "======= FDAX DEBUG =======\n" +
                "Regime        : {0}\n" +
                "Reversal Score: {1}\n" +
                "Score Band    : {2}\n" +
                "Sweep         : See TopRight panel\n" +
                "Direction     : {3}\n" +
                "Setup Status  : {4}\n" +
                "Entry Price   : {5}\n" +
                "Stop Level    : {6}\n" +
                "T1 Target     : {7}\n" +
                "T2 Target     : {8}\n" +
                "Bars Since    : {9}\n" +
                "Last Result   : {10}\n" +
                "==========================",
                regimeStr,
                score10Str,
                _lastBand,
                _lastDirection,
                stateStr,
                _signalBar >= 0 ? ((int)_entryPrice).ToString() : "N/A",
                _signalBar >= 0 ? ((int)_stopPrice).ToString()  : "N/A",
                _signalBar >= 0 ? ((int)_t1Price).ToString()    : "N/A",
                _signalBar >= 0 ? ((int)_t2Price).ToString()    : "N/A",
                barsSince >= 0  ? barsSince.ToString()          : "—",
                lastResult);

            Draw.TextFixed(this, "RevDebug", panel,
                TextPosition.BottomRight, Brushes.WhiteSmoke,
                new Gui.Tools.SimpleFont("Consolas", 10),
                Brushes.Black, Brushes.Black, 80);
        }
    }
}

/*
=================================================================
SIM VALIDATION PROTOCOL — REVERSAL SCORER

ENTRY CHECKLIST:
  [ ] Score >= 7.0 (Strong — CONFIRMED fires)
  [ ] Regime = AUCTION (RegimeDetector shows clear background)
  [ ] Level identified BEFORE price arrives
  [ ] 1m shows rejection candle at the level

FDAX FIXED LEVELS:
  Entry : close of signal bar (whole point)
  T1    : entry ± 5 pts   = €125/contract
  T2    : entry ± 10 pts  = €250/contract
  Stop  : entry ∓ 15 pts  = €375/contract

CIRCLES:
  LONG  — Green shades — drawn ABOVE signal bar
  SHORT — Red/Orange shades — drawn BELOW signal bar
  Label format: "LONG - Strong 7.4"

SCORE SCALE:
  7.0–10 = Strong  = CONFIRMED (full entry)
  5.0–7.0 = Good   = BUILDING  (watch for upgrade)
  3.0–5.0 = Weak   = BUILDING  (faint signal only)
  < 3.0   = no circle
=================================================================
*/
