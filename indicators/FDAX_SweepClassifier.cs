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
// FDAX SWEEP CLASSIFIER — v3
// ============================================================
// Detects liquidity sweeps (stop runs) and scores them 0–100.
// Score is DISPLAYED as 0–10 (divide by 10) in labels and panel.
//
// A sweep = bar that spikes through a prior swing high/low
//           AND closes back inside on the same bar.
//
// DIRECTION:
//   Bullish sweep (spike DOWN, close back UP) = LONG reversal
//   Bearish sweep (spike UP, close back DOWN) = SHORT reversal
//
// ARROW PLACEMENT (conventional):
//   LONG  (bull sweep): ArrowUp drawn BELOW bar (pointing up)
//   SHORT (bear sweep): ArrowDown drawn ABOVE bar (pointing down)
//
// ARROW COLOURS (direction-aware):
//   LONG  Strong/Good/Weak = green shades
//   SHORT Strong/Good/Weak = red/orange shades
//   Previously: colour was score-only (all Greens on any direction)
//
// SCORE THRESHOLDS (0–10 scale, configurable):
//   Strong (default 7.0) → CONFIRMED — draw entry/exit levels + alert
//   Good   (default 5.0) → BUILDING  — show warning text
//   Weak   (default 3.0) → BUILDING  — show faint warning
//   Below Weak           → no arrow drawn
//
// FDAX FIXED LEVELS:
//   T1   = entry ± 5 pts   = €125/contract
//   T2   = entry ± 10 pts  = €250/contract
//   Stop = entry ∓ 15 pts  = €375/contract
// ============================================================

namespace NinjaTrader.NinjaScript.Indicators
{
    public class FDAX_SweepClassifier : Indicator
    {
        // ── FDAX fixed-point trade levels ─────────────────────────────
        private const double T1_POINTS   = 5.0;   // Target 1:  +5pts = €125
        private const double T2_POINTS   = 10.0;  // Target 2: +10pts = €250
        private const double STOP_POINTS = 15.0;  // Fixed stop: 15pts = €375

        // ── Auto-cleanup: remove level lines after this many bars ──────
        private const int LINE_AUTO_REMOVE = 24;  // 2 hours on 5-min chart

        // ── Score calculation parameters ───────────────────────────────
        private const int    LOOKBACK_DEFAULT = 20;
        private const double MIN_SPIKE_PTS    = 3.0;
        private const int    ATR_PERIOD       = 14;
        private const int    VOL_PERIOD       = 20;
        private const int    ER_PERIOD        = 10;

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
                 Description = "Score at or above = Strong. CONFIRMED fires. Default 7.0")]
        public double StrongThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Good Threshold (0-10)", Order = 2, GroupName = "Score Thresholds",
                 Description = "Score at or above = Good. BUILDING state. Default 5.0")]
        public double GoodThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Weak Threshold (0-10)", Order = 3, GroupName = "Score Thresholds",
                 Description = "Score at or above = Weak. Below this = no arrow. Default 3.0")]
        public double WeakThreshold { get; set; }

        // ── Sweep detection parameters ────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "Min Spike Points", Order = 4, GroupName = "Sweep Detection",
                 Description = "Min whole FDAX points beyond prior high/low to qualify as a sweep")]
        public double MinSpike { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Swing Lookback", Order = 5, GroupName = "Sweep Detection",
                 Description = "Number of bars to look back for prior swing high/low")]
        public int SweepLookback { get; set; }

        // ── Display parameters ────────────────────────────────────────
        [NinjaScriptProperty]
        [Display(Name = "Circle Size (1=Small 2=Med 3=Large)", Order = 6, GroupName = "Display",
                 Description = "Controls label text size. 1=Small 2=Medium 3=Large")]
        public int CircleSize { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Debug Panel", Order = 7, GroupName = "Display",
                 Description = "Show the Sweep Classifier debug panel at BottomLeft")]
        public bool ShowDebugPanel { get; set; }

        // ── Long (bull sweep) signal colours ──────────────────────────
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

        // ── Short (bear sweep) signal colours ─────────────────────────
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

        // ── MinScore kept for internal backward compatibility ──────────
        [Browsable(false)]
        public double MinScore { get; set; }

        // ── Public accessors — readable by FDAX_ReversalScorer ────────
        public string ActiveSweepDirection { get { return _lastDirection; } }
        public string ActiveSweepState     { get { return _setupState.ToString(); } }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description    = "FDAX Sweep Classifier — scores liquidity sweeps 0–10";
                Name           = "FDAX_SweepClassifier";
                Calculate      = Calculate.OnBarClose;
                IsOverlay      = true;

                // Score thresholds (0–10 scale)
                StrongThreshold = 7.0;
                GoodThreshold   = 5.0;
                WeakThreshold   = 3.0;

                // Sweep detection
                MinSpike      = MIN_SPIKE_PTS;
                SweepLookback = LOOKBACK_DEFAULT;
                MinScore      = 30.0;  // internal fallback

                // Display
                CircleSize     = 2;
                ShowDebugPanel = true;

                // Long (bull sweep) colours — green shades
                LongStrongColor = Brushes.LimeGreen;
                LongGoodColor   = Brushes.Green;
                LongWeakColor   = Brushes.OliveDrab;

                // Short (bear sweep) colours — red/orange shades
                ShortStrongColor = Brushes.Red;
                ShortGoodColor   = Brushes.Orange;
                ShortWeakColor   = Brushes.Gold;

                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Line, "SweepScore");
            }
        }

        protected override void OnBarUpdate()
        {
            // ── 5-minute chart guard ──────────────────────────────────
            if (BarsPeriod.BarsPeriodType != BarsPeriodType.Minute || BarsPeriod.Value != 5)
            {
                Draw.TextFixed(this, "SweepDebug",
                    "⚠ FDAX Indicators optimised for 5-Min chart only",
                    TextPosition.BottomLeft, Brushes.OrangeRed,
                    new Gui.Tools.SimpleFont("Consolas", 12),
                    Brushes.Transparent, Brushes.Transparent, 0);
                return;
            }

            if (CurrentBar < SweepLookback + 10) return;

            // ── Track active confirmed signal — runs every bar ─────────
            if (_setupState == SetupState.Confirmed && _signalBar != CurrentBar && !_failureFlagged)
            {
                if (!_t1Hit)
                {
                    bool t1Reached = _isLong ? High[0] >= _t1Price : Low[0] <= _t1Price;
                    if (t1Reached)
                    {
                        _t1Hit       = true;
                        _priorResult = "T1 HIT +" + ((int)T1_POINTS) + "pts";
                    }
                    else
                    {
                        bool stopHit = _isLong ? Low[0] <= _stopPrice : High[0] >= _stopPrice;
                        if (stopHit)
                        {
                            _setupState     = SetupState.Failed;
                            _failureFlagged = true;
                            _priorResult    = "FAILED @ " + ((int)_stopPrice);

                            double failY = _isLong
                                ? High[0] + ATR(ATR_PERIOD)[0] * 0.5
                                : Low[0]  - ATR(ATR_PERIOD)[0] * 0.5;

                            Draw.Text(this, "SwFail_" + CurrentBar,
                                "✖ FAIL -" + ((int)STOP_POINTS) + "pts",
                                0, failY, Brushes.Red);

                            Print(string.Format(
                                "[FDAX] FAILED SETUP | Direction: {0} | Entry: {1} | Stop Hit: {2} | Time: {3}",
                                _lastDirection, (int)_entryPrice, (int)_stopPrice,
                                Time[0].ToString("HH:mm")));
                        }
                    }
                }

                if (!_failureFlagged && CurrentBar - _signalBar >= LINE_AUTO_REMOVE)
                {
                    CleanupLevelLines();
                    _setupState = SetupState.None;
                }
            }

            // ── Find prior swing high and low ──────────────────────────
            double priorHigh = double.MinValue;
            double priorLow  = double.MaxValue;
            for (int i = 1; i <= SweepLookback; i++)
            {
                if (High[i] > priorHigh) priorHigh = High[i];
                if (Low[i]  < priorLow)  priorLow  = Low[i];
            }

            double barHigh  = High[0];
            double barLow   = Low[0];
            double barClose = Close[0];
            double atr      = ATR(ATR_PERIOD)[0];

            // ── Check for sweep conditions ─────────────────────────────
            // Bearish sweep: spike ABOVE prior high, close BACK below
            bool isBearishSweep = barHigh  > priorHigh + MinSpike
                                  && barClose < priorHigh;
            // Bullish sweep: spike BELOW prior low, close BACK above
            bool isBullishSweep = barLow   < priorLow  - MinSpike
                                  && barClose > priorLow;

            if (!isBearishSweep && !isBullishSweep)
            {
                DrawSweepDebugPanel();
                return;
            }

            // ── Direction: sweep type determines trade direction ────────
            // Bullish sweep (stop run on lows) = LONG reversal
            // Bearish sweep (stop run on highs) = SHORT reversal
            bool isLongSetup = isBullishSweep;

            // ── Compute features for scoring ───────────────────────────

            // 1. Spike depth (in ATR units) — top feature (0.226 importance)
            double spikeDepth;
            if (isBearishSweep)
                spikeDepth = (barHigh - priorHigh) / Math.Max(atr, 0.01);
            else
                spikeDepth = (priorLow - barLow)   / Math.Max(atr, 0.01);

            // 2. Volume z-score — 0.131 importance
            double volAvg = 0, volStd = 0;
            for (int i = 0; i < VOL_PERIOD; i++) volAvg += Volume[i];
            volAvg /= VOL_PERIOD;
            for (int i = 0; i < VOL_PERIOD; i++)
                volStd += Math.Pow(Volume[i] - volAvg, 2);
            volStd = Math.Sqrt(volStd / VOL_PERIOD);
            double volZscore = volStd > 0 ? (Volume[0] - volAvg) / volStd : 0;

            // 3. Volume ratio — 0.123 importance
            double volRatio = volAvg > 0 ? Volume[0] / volAvg : 1.0;

            // 4. ATR ratio — 0.117 importance
            double atrAvg = 0;
            for (int i = 0; i < 60; i++) atrAvg += ATR(ATR_PERIOD)[i];
            atrAvg /= 60.0;
            double atrRatio = atrAvg > 0 ? atr / atrAvg : 1.0;

            // 5. Efficiency ratio — 0.101 importance
            double netMove   = Math.Abs(Close[0] - Close[ER_PERIOD]);
            double totalPath = 0.0001;
            for (int i = 0; i < ER_PERIOD; i++)
                totalPath += Math.Abs(Close[i] - Close[i + 1]);
            double er = Math.Min(netMove / totalPath, 1.0);

            // 6. Bar close position — how cleanly did price reclaim the level?
            double barRange   = barHigh - barLow;
            double closePos   = barRange > 0 ? (barClose - barLow) / barRange : 0.5;
            double reclaimScore;
            if (isBullishSweep)
                reclaimScore = closePos * 100;            // want close near high
            else
                reclaimScore = (1.0 - closePos) * 100;   // want close near low

            // 7. Wick ratio — large wick = clean rejection signature
            double body      = Math.Abs(Close[0] - Open[0]);
            double wickRatio = barRange > 0 ? (barRange - body) / barRange : 0;

            // ── Score calculation (0–100 internal scale) ──────────────
            double score = 0;

            double spikeScore;
            if (spikeDepth < 0.3)
                spikeScore = spikeDepth / 0.3 * 60;
            else if (spikeDepth <= 1.5)
                spikeScore = 60 + (spikeDepth - 0.3) / 1.2 * 40;
            else
                spikeScore = Math.Max(0, 100 - (spikeDepth - 1.5) * 30);
            score += Math.Max(0, Math.Min(100, spikeScore)) * 0.25;

            score += Math.Min(100, Math.Max(0, 50 + volZscore * 15)) * 0.15;
            score += Math.Min(volRatio / 3.0, 1.0) * 100 * 0.12;

            double atrScore = 100 - Math.Abs(atrRatio - 1.1) * 50;
            score += Math.Max(0, Math.Min(100, atrScore)) * 0.12;

            score += (1.0 - er) * 100 * 0.10;
            score += reclaimScore * 0.15;
            score += wickRatio * 100 * 0.11;

            score        = Math.Max(0, Math.Min(100, score));
            Values[0][0] = score;
            _lastScore   = score;

            // ── Convert to 0–10 scale for threshold comparison ────────
            double score10 = score / 10.0;

            // ── Classify score band ───────────────────────────────────
            bool isStrong  = score10 >= StrongThreshold;
            bool isGood    = !isStrong && score10 >= GoodThreshold;
            bool isWeak    = !isStrong && !isGood && score10 >= WeakThreshold;
            bool anySignal = isStrong || isGood || isWeak;

            if (!anySignal)
            {
                DrawSweepDebugPanel();
                return;
            }

            // ── DIRECTION-AWARE COLOUR ASSIGNMENT ─────────────────────
            // FIX: Arrow colour now uses direction (long/short) AND score band.
            // Previously: colour was score-only (green for all strong signals
            // regardless of bull or bear direction).
            Brush  arrowBrush;
            string bandLabel;

            if (isStrong)
            {
                arrowBrush = isLongSetup ? LongStrongColor : ShortStrongColor;
                bandLabel  = "Strong";
            }
            else if (isGood)
            {
                arrowBrush = isLongSetup ? LongGoodColor : ShortGoodColor;
                bandLabel  = "Good";
            }
            else
            {
                arrowBrush = isLongSetup ? LongWeakColor : ShortWeakColor;
                bandLabel  = "Weak";
            }

            string dirLabel    = isLongSetup ? "LONG" : "SHORT";
            string scoreDisplay = score10.ToString("F1");
            string starStr     = isStrong ? " ★★" : (isGood ? " ★" : "");
            string arrowLabel  = dirLabel + " - " + bandLabel + " " + scoreDisplay + starStr;

            // ── CONFIRMED: draw fixed-point entry/exit levels ──────────
            if (isStrong && _signalBar != CurrentBar)
            {
                CleanupLevelLines();

                _isLong         = isLongSetup;
                _entryPrice     = Math.Round(barClose, 0);
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

                string pfx = "Sw_" + CurrentBar + "_";
                _tagEntry  = pfx + "Entry";
                _tagT1     = pfx + "T1";
                _tagT2     = pfx + "T2";
                _tagStop   = pfx + "Stop";

                double lblOff     = _isLong ?  atr * 0.2 : -atr * 0.2;
                double stopLblOff = _isLong ? -atr * 0.2 :  atr * 0.2;

                // Entry line — Yellow dashed (narrow: ±3 bars around signal candle)
                Draw.Line(this, _tagEntry, false, 3, _entryPrice, -3, _entryPrice,
                    Brushes.Yellow, DashStyleHelper.Dash, 2);
                Draw.Text(this, _tagEntry + "Lbl",
                    (_isLong ? "LONG ENTRY @ " : "SHORT ENTRY @ ") + ((int)_entryPrice).ToString(),
                    0, _entryPrice + lblOff, Brushes.Yellow);

                // Target 1 — Light Green dotted (+5pts  €125)
                Draw.Line(this, _tagT1, false, 3, _t1Price, -3, _t1Price,
                    Brushes.LightGreen, DashStyleHelper.Dot, 1);
                Draw.Text(this, _tagT1 + "Lbl",
                    "T1 +5pts / +€125  " + ((int)_t1Price).ToString(),
                    0, _t1Price + lblOff, Brushes.LightGreen);

                // Target 2 — Cyan dotted (+10pts  €250)
                Draw.Line(this, _tagT2, false, 3, _t2Price, -3, _t2Price,
                    Brushes.Cyan, DashStyleHelper.Dot, 1);
                Draw.Text(this, _tagT2 + "Lbl",
                    "T2 +10pts / +€250  " + ((int)_t2Price).ToString(),
                    0, _t2Price + lblOff, Brushes.Cyan);

                // Stop — Red solid (-15pts  €375)
                Draw.Line(this, _tagStop, false, 3, _stopPrice, -3, _stopPrice,
                    Brushes.Red, DashStyleHelper.Solid, 1);
                Draw.Text(this, _tagStop + "Lbl",
                    "STOP -15pts / -€375  " + ((int)_stopPrice).ToString(),
                    0, _stopPrice + stopLblOff, Brushes.Red);

                // Alert text near the signal bar
                // LONG: ArrowUp is below bar, so put alert text below bar too
                // SHORT: ArrowDown is above bar, so put alert text above bar
                double alertY = _isLong
                    ? barLow  - atr * 2.5
                    : barHigh + atr * 2.5;
                Draw.Text(this, pfx + "Alert",
                    (_isLong ? "✅ LONG ENTRY" : "✅ SHORT ENTRY"),
                    0, alertY, arrowBrush);

                // Log confirmed setup to Output window
                Print(string.Format(
                    "[FDAX] CONFIRMED SETUP | Direction: {0} | Entry: {1} | T1: {2} | T2: {3} | Stop: {4} | Time: {5}",
                    _lastDirection, (int)_entryPrice, (int)_t1Price, (int)_t2Price,
                    (int)_stopPrice, Time[0].ToString("HH:mm")));

                // NT8 alert sound (real-time bars only, 5s re-arm)
                if (State == State.Realtime
                    && (Time[0] - _lastAlertTime).TotalSeconds >= 5)
                {
                    Alert("SwConfirm", Priority.High,
                        dirLabel + " Sweep CONFIRMED at " + ((int)_entryPrice).ToString(),
                        NinjaTrader.Core.Globals.InstallDir + @"\sounds\Alert1.wav",
                        10, Brushes.LimeGreen, Brushes.Black);
                    _lastAlertTime = Time[0];
                }
            }

            // ── BUILDING: show warning text near the signal bar ────────
            if (!isStrong
                && _setupState != SetupState.Confirmed
                && _setupState != SetupState.Failed)
            {
                _setupState    = SetupState.Building;
                _lastDirection = dirLabel;
                _lastBand      = bandLabel;

                // LONG arrow is below bar → put building text below bar
                // SHORT arrow is above bar → put building text above bar
                double warnY = isLongSetup
                    ? barLow  - atr * 2.0
                    : barHigh + atr * 2.0;

                Brush warnBrush = isLongSetup ? Brushes.Yellow : Brushes.Orange;
                Draw.Text(this, "SwBuild_" + CurrentBar,
                    isLongSetup
                        ? "⚠ LONG SETUP BUILDING"
                        : "⚠ SHORT SETUP BUILDING",
                    0, warnY, warnBrush);
            }

            // ── Draw directional arrow and label ───────────────────────
            // Conventional placement: buy arrows below bar (ArrowUp pointing up)
            //                         sell arrows above bar (ArrowDown pointing down)
            string arrowTag = "SweepArrow_" + CurrentBar;
            string textTag  = "SweepText_"  + CurrentBar;

            if (isBullishSweep)
            {
                // Spike down then closed back up = LONG reversal
                // Arrow pointing UP, drawn below the bar
                Draw.ArrowUp(this, arrowTag, false, 0,
                    barLow - atr * 0.4, arrowBrush);
                Draw.Text(this, textTag, arrowLabel, 0, barLow - atr * 0.9, arrowBrush);
            }
            else
            {
                // Spike up then closed back down = SHORT reversal
                // Arrow pointing DOWN, drawn above the bar
                Draw.ArrowDown(this, arrowTag, false, 0,
                    barHigh + atr * 0.4, arrowBrush);
                Draw.Text(this, textTag, arrowLabel, 0, barHigh + atr * 0.9, arrowBrush);
            }

            DrawSweepDebugPanel();
        }

        // ── Remove the active signal's horizontal level lines ──────────
        private void CleanupLevelLines()
        {
            if (!string.IsNullOrEmpty(_tagEntry))
            {
                RemoveDrawObject(_tagEntry);      RemoveDrawObject(_tagEntry + "Lbl");
                RemoveDrawObject(_tagT1);         RemoveDrawObject(_tagT1 + "Lbl");
                RemoveDrawObject(_tagT2);         RemoveDrawObject(_tagT2 + "Lbl");
                RemoveDrawObject(_tagStop);       RemoveDrawObject(_tagStop + "Lbl");
                _tagEntry = "";
            }
        }

        // ── Sweep debug panel at BottomLeft ───────────────────────────
        // Moved from TopRight to avoid overlapping with RegimeDetector (TopLeft)
        // and to make room for ReversalScorer's FDAX DEBUG panel at BottomRight.
        private void DrawSweepDebugPanel()
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

            string score10Str = _lastScore > 0 ? (_lastScore / 10.0).ToString("F1") : "—";
            string lastResult = _t1Hit ? "PASS" : (_setupState == SetupState.Failed ? "FAIL" : "PENDING");

            string panel = string.Format(
                "── SWEEP CLASSIFIER ─────────────────\n" +
                "Score         : {0} / 10\n" +
                "Band          : {1}\n" +
                "Setup State   : {2}\n" +
                "Direction     : {3}\n" +
                "Entry         : {4}\n" +
                "T1 +5pts      : {5}   [€125]\n" +
                "T2 +10pts     : {6}   [€250]\n" +
                "Stop -15pts   : {7}   [€375]\n" +
                "Bars Since    : {8}\n" +
                "Last Result   : {9}",
                score10Str,
                _lastBand,
                stateStr,
                _lastDirection,
                _signalBar >= 0 ? ((int)_entryPrice).ToString() : "N/A",
                _signalBar >= 0 ? ((int)_t1Price).ToString()    : "N/A",
                _signalBar >= 0 ? ((int)_t2Price).ToString()    : "N/A",
                _signalBar >= 0 ? ((int)_stopPrice).ToString()  : "N/A",
                barsSince >= 0  ? barsSince.ToString()          : "—",
                lastResult);

            Draw.TextFixed(this, "SweepDebug", panel,
                TextPosition.BottomLeft, Brushes.WhiteSmoke,
                new Gui.Tools.SimpleFont("Consolas", 10),
                Brushes.Black, Brushes.Black, 80);
        }
    }
}

/*
=================================================================
SIM VALIDATION PROTOCOL — SWEEP CLASSIFIER

WHAT TO LOOK FOR:
  An arrow fires on the bar that swept the level and closed back.
  Green  = LONG setup (bull sweep — spike down, close back up)
  Red    = SHORT setup (bear sweep — spike up, close back down)
  ★★ = Strong, ★ = Good, no star = Weak/Building

FILTERS — only take sweeps when:
  [ ] Regime Detector shows AUCTION (TopLeft panel)
  [ ] Prior level respected at least once before
  [ ] Session is NOT lunch (11:00–13:00 CET)
  [ ] Score >= 7.0 (Strong / ★★)

FDAX FIXED LEVELS:
  Entry : close of sweep bar (whole point)
  T1    : entry ± 5 pts   = €125/contract
  T2    : entry ± 10 pts  = €250/contract
  Stop  : entry ∓ 15 pts  = €375/contract
=================================================================
*/

