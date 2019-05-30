#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;

using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Indicators.ZTraderInd;
using NinjaTrader.NinjaScript.Strategies.ZTraderStg;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	/// <summary>
	/// This file defined the interfaces talking with indicators;
	/// This interface needs to work with multiple indicators in the strategy
	/// to generate the signal or get the value of the functions 
	/// </summary>
	public partial class GSZTraderBase : Strategy
	{
		private List<Indicator> listIndicator = new List<Indicator>();
		protected TradeSignal tradeSignal;
		
		/// <summary>
		/// The key=BarNo that holds the signal set
		/// value=the set of signals
		/// </summary>
		private Dictionary<int, List<IndicatorSignal>> indicatorSignals = 
			new Dictionary<int, List<IndicatorSignal>>();
		
		protected GIndicatorProxy indicatorProxy;
		//private IndicatorSignal indSignal;
		
		/// <summary>
		/// Volatility measurement is for target, stop loss, etc.
		/// </summary>
		/// <returns></returns>
		public Volatility GetVolatility() {return null;}
		
		/// <summary>
		/// MarketCycle is for 
		/// </summary>
		/// <returns></returns>
		public MarketCycle GetMarketCycle() {return null;}
		
		/// <summary>
		/// Direction is to tell up/down, buy or sell;
		/// </summary>
		/// <returns></returns>
		public virtual Direction GetDirection(GIndicatorBase indicator) {return null;}
		
		/// <summary>
		/// Support and resistance is to define entry/exit level, target and stop loss
		/// </summary>
		/// <returns></returns>
		public virtual SupportResistanceBar GetSupport(){return null;}
		public virtual SupportResistanceBar GetResistance(){return null;}
		public virtual SupportResistanceBar GetSptRest(int barNo) {return null;}
		
		/// <summary>
		/// Check if divergence occurs in this indicator;
		/// </summary>
		/// <returns></returns>
		public virtual DivergenceType CheckDivergence(GIndicatorBase indicator) {
			return DivergenceType.UnKnown;
		}		
		
		/// <summary>
		/// The indicator signal is to trigger entry/exit, 
		/// or modify existing orders for extry/exit;
		/// </summary>
		/// <returns></returns>
		//public virtual IndicatorSignal GetIndicatorSignal() {return null;}

		/// <summary>
		/// The trade signal is to trigger entry/exit, 
		/// or modify existing orders for extry/exit;
		/// </summary>
		/// <returns></returns>
		public virtual TradeSignal GetTradeSignal() {return null;}
		
		/// <summary>
		/// Detect if the market condition has changed or not since last signal
		/// </summary>
		/// <returns></returns>
		public bool HasMarketContextChanged() {return false;}
		
		
		#region Variables
        // User defined variables (add any user defined variables below)
        //private int startH = 9; // Default setting for StartH
       // private int startM = 5; // Default setting for StartM
        //private int endH = 11; // Default setting for EndH
       // private int endM = 5; // Default setting for EndM
		//private string accName = ""; //account name from strategy, extracting simply string for print/log;
		
		#endregion
		
		#region Methods
//		public string GetAccName() {
//			return accName;
//		}		
		public void AddIndicator(Indicator i) {
			this.listIndicator.Add(i);
		}
		
		/// <summary>
		/// Add the signal to the list of the bar with barNo
		/// </summary>
		/// <param name="barNo"></param>
		/// <param name="signal"></param>
		public void AddIndicatorSignals(int barNo, IndicatorSignal signal) {
			List<IndicatorSignal> list_signal;
			if(!this.indicatorSignals.TryGetValue(barNo, out list_signal)) {				
				list_signal = new List<IndicatorSignal>();
			}
			list_signal.Add(signal);
			this.indicatorSignals[barNo] = list_signal;
		}
		
		/// <summary>
		/// Get the signal from bar with barNo and the signal_name
		/// </summary>
		/// <param name="barNo"></param>
		/// <param name="signal_name"></param>
		/// <returns></returns>
		public IndicatorSignal GetIndicatorSignalAtBar(int barNo, string signal_name) {
			
			if(this.indicatorSignals.ContainsKey(barNo)) {
				List<IndicatorSignal> list_signal = this.indicatorSignals[barNo];
				foreach(IndicatorSignal sig in list_signal) {
					if(signal_name.Equals(sig.SignalName))
						return sig;
				}
			}
			
			return null;			
		}

		#endregion

        #region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public Series<double> StartHM
        {
            get { return Values[0]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public Series<double> EndHM
        {
            get { return Values[1]; }
        }

//        [Description("Hour of start trading")]
//        //[GridCategory("Parameters")]
//        public int StartH
//        {
//            get { return startH; }
//            set { startH = Math.Max(0, value); }
//        }

//        [Description("Min of start trading")]
//        //[GridCategory("Parameters")]
//        public int StartM
//        {
//            get { return startM; }
//            set { startM = Math.Max(0, value); }
//        }

//        [Description("Hour of end trading")]
//        //[GridCategory("Parameters")]
//        public int EndH
//        {
//            get { return endH; }
//            set { endH = Math.Max(0, value); }
//        }

//        [Description("Min of end trading")]
//        //[GridCategory("Parameters")]
//        public int EndM
//        {
//            get { return endM; }
//            set { endM = Math.Max(0, value); }
//        }
		
//		[NinjaScriptProperty]
//		[XmlIgnore]
//		[Display(Name="CustomColor1", Description="Color-1", Order=1, GroupName="Parameters")]
//		public Brush CustomColor1
//		{ get; set; }

//		[Browsable(false)]
//		public string CustomColor1Serializable
//		{
//			get { return Serialize.BrushToString(CustomColor1); }
//			set { CustomColor1 = Serialize.StringToBrush(value); }
//		}			

//		[NinjaScriptProperty]
//		[Range(1, double.MaxValue)]
//		[Display(Name="CustomPrc1", Description="CustomPrc-1", Order=2, GroupName="Parameters")]
//		public double CustomPrc1
//		{ get; set; }

//		[NinjaScriptProperty]
//		[Display(Name="CustomStr1", Description="CustomStr-1", Order=3, GroupName="Parameters")]
//		public string CustomStr1
//		{ get; set; }

//		[NinjaScriptProperty]
//		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
//		[Display(Name="CustomTime1", Description="CustomTime-1", Order=4, GroupName="Parameters")]
//		public DateTime CustomTime1
//		{ get; set; }		
        #endregion		
	}
}
