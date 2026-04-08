using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class TemplateStrategy : Strategy
    {
        private bool _debugMode = true;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Points", Order = 1, GroupName = "Risk")]
        public int StopPoints { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 1 Points", Order = 2, GroupName = "Risk")]
        public int Target1Points { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Target 2 Points", Order = 3, GroupName = "Risk")]
        public int Target2Points { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "TemplateStrategy";
                Description = "Base strategy template for FDAX development";
                Calculate   = Calculate.OnBarClose;
                StopPoints    = 15;
                Target1Points = 5;
                Target2Points = 10;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds    = 30;
            }
            else if (State == State.DataLoaded)
            {
                BarsRequiredToPlot  = 20;
                BarsRequiredToTrade = 20;
                SetStopLoss(CalculationMode.Ticks, StopPoints * 2);
                SetProfitTarget("T1", CalculationMode.Ticks, Target1Points * 2);
                SetProfitTarget("T2", CalculationMode.Ticks, Target2Points * 2);
            }
            else if (State == State.Terminated)
            {
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar < BarsRequiredToTrade) return;
                if (Position.MarketPosition != MarketPosition.Flat) return;

                int now = ToTime(Time[0]);
                if (now < 70000 || now > 170000) return;

                bool longSignal  = false;
                bool shortSignal = false;

                if (longSignal)
                {
                    EnterLong(1, "Long");
                    if (_debugMode) Print(string.Format("[{0}] LONG @ {1:F2} Bar={2}", Name, Close[0], CurrentBar));
                }
                else if (shortSignal)
                {
                    EnterShort(1, "Short");
                    if (_debugMode) Print(string.Format("[{0}] SHORT @ {1:F2} Bar={2}", Name, Close[0], CurrentBar));
                }
            }
            catch (Exception ex)
            {
                Print("ERROR in " + Name + ": " + ex.Message);
            }
        }
    }
}
