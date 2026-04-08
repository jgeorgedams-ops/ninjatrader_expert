using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class TemplateIndicator : Indicator
    {
        private bool _debugMode = true;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Period", Order = 1, GroupName = "Parameters")]
        public int Period { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name        = "TemplateIndicator";
                Description = "Base indicator template for FDAX development";
                Calculate   = Calculate.OnBarClose;
                IsOverlay   = false;
                AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "Signal");
                Period = 14;
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                BarsRequiredToPlot = Period;
            }
            else if (State == State.Terminated)
            {
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar < BarsRequiredToPlot) return;

                Values[0][0] = SMA(Close, Period)[0];

                if (_debugMode)
                    Print(string.Format("[{0}] Bar={1} C={2:F2} Signal={3:F2}",
                        Name, CurrentBar, Close[0], Values[0][0]));
            }
            catch (Exception ex)
            {
                Print("ERROR in " + Name + ": " + ex.Message);
            }
        }
    }
}
