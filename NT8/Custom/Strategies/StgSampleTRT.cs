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
using NinjaTrader.NinjaScript.AddOns;
using NinjaTrader.NinjaScript.Indicators.ZTraderInd;
using NinjaTrader.NinjaScript.Indicators.PriceActions;
using NinjaTrader.NinjaScript.Indicators.ZTraderPattern;
using NinjaTrader.NinjaScript.Strategies.ZTraderStg;

#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	/// <summary>
	/// Sample strategy fro GSZTrader:
	/// 1) OnStateChange();
	/// 2) OnBarUpdate();
	/// 3) GetIndicatorSignal();
	/// 4) GetTradeSignal();
	/// 5) CheckNewEntryTrade();
	/// 6) PutTrade();
	/// Indicator Combination:
	/// * SnR: daily high/low
	/// * Breakout: morning breakout of the SnR, big bar cross the SnR
	/// * Reversal Pivot: 9:00-11 AM morning session high/low
	/// * Pullback Pivot: left 20+, right 5+, i.e. (20+, 5+)
	/// * Trending pivot: breakout the pullback pivot, create a new (5+, 5+) pivot
	/// Long/Short rules:
	/// * KAMA indicates trend;
	/// * Long: cyan diamond, Short: red diamond;
	/// * Trend-following long/short entry;
	/// * Stop loss: last n (five?) bars hi/lo;
	/// * Profit Target: next support/resistance, cyan/red diamond;
	/// * Breakeven, or KAMA/EMA did not moving towards target in a period of time, exit?
	/// * Use cyan/red diamond find key reversal: 
	/// 	look back n bars, find the highest/lowest bar as KR; It's leading key reversal;
	/// 
	/// </summary>
	public class StgSampleTRT : GStrategyBase
	{
		private GISMI giSMI;
		private GIAwesomeOscillator awOscillator;
		private GIKAMA giKAMA;
		
		private double c0 = 0, hi3 = Double.MaxValue, lo3 = Double.MinValue;
		
		protected override void OnStateChange()
		{
			base.OnStateChange();
			if (State == State.SetDefaults)
			{
				Print(this.Name + " set defaults called....");
				Description									= @"The sample strategy for GSZTrader.";
				Name										= "StgSampleTRT";
				Calculate									= Calculate.OnBarClose;
				IsFillLimitOnTouch							= false;
				TraceOrders									= false;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.DataLoaded)
			{
				Print(this.Name + " set DataLoaded called....");				
				AddChartIndicator(indicatorProxy);
				SetPrintOut(1);
				indicatorProxy.LoadSpvPRList(SpvDailyPatternES.spvPRDayES);
				indicatorProxy.AddPriceActionTypeAllowed(PriceActionType.DnWide);
				
				giSMI = GISMI(EMAPeriod1, EMAPeriod2, Range, SMITMAPeriod, SMICrossLevel);//(3, 5, 5, 8);
				awOscillator = GIAwesomeOscillator(FastPeriod, SlowPeriod, Smooth, MovingAvgType.SMA, false);//(5, 34, 5, MovingAvgType.SMA);
				giKAMA = GIKAMA(FastKAMA, PeriodKAMA, SlowKAMA);
				
				AddChartIndicator(giSMI);
				AddChartIndicator(awOscillator);
				AddChartIndicator(giKAMA);
				Print("GISMI called:" + "EMAPeriod1=" + EMAPeriod1 + "EMAPeriod2=" + EMAPeriod2 + "Range=" + Range + "SMITMAPeriod=" + SMITMAPeriod);
			}
			else if (State == State.Configure)
			{
				Print(this.Name + " set Configure called.... CurrentTrade=" + CurrentTrade);
//				if(CurrentTrade == null)
//					CurrentTrade = new CurrentTrade(this);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void OnBarUpdate()
		{
			try {
			base.OnBarUpdate();
			indicatorProxy.TraceMessage(this.Name, PrintOut);
			} catch (Exception ex) {
				indicatorProxy.Log2Disk = true;
				indicatorProxy.PrintLog(true, true, "Exception: " + ex.StackTrace);
			}
		}
		
		public override void CheckIndicatorSignals(){
			giSMI.Update();
			Print(CurrentBar + ":CheckIndicatorSignals called -----------" + giSMI.LastInflection);
			
			IndicatorSignal indSig = giSMI.GetLastIndicatorSignalByName(CurrentBar, giSMI.SignalName_Inflection);
			
			if(indSig != null && indSig.SignalAction != null)
				Print(CurrentBar + ":stg-Last " + giSMI.SignalName_Inflection + "=" + indSig.BarNo + "," + indSig.SignalAction.SignalActionType.ToString());

			IndicatorSignal indSigCrs = giSMI.GetLastIndicatorSignalByName(CurrentBar, giSMI.SignalName_LineCross);
			
			if(indSigCrs != null && indSigCrs.SignalAction != null)
				Print(CurrentBar + ":stg-Last " + giSMI.SignalName_LineCross + "=" + indSigCrs.BarNo + "," + indSigCrs.SignalAction.SignalActionType.ToString());
		}
		
		public override bool CheckTradeSignals() {
			indicatorProxy.TraceMessage(this.Name, PrintOut);
			List<TradeSignal> sigList = new List<TradeSignal>();
			TradeSignal trdSignal = new TradeSignal();
			Direction dir = giKAMA.GetDirection();// new Direction();
			PatternMatched();
			c0 = Close[0];
			
			Print(CurrentBar + ":"
			+ ";c0=" + c0
			+ ";hi3=" + hi3
			+ ";lo3=" + lo3
			+ ";BarsLookback=" + BarsLookback);
			
//			if(c0 > hi3)
//				dir.TrendDir = TrendDirection.Up;

//			if(c0 < lo3)
//				dir.TrendDir = TrendDirection.Down;
//			trdSignal.TrendDir = dir;
			
//			this.AddTradeSignal(CurrentBar, trdSignal);
			hi3 = GetHighestPrice(BarsLookback);
			lo3 = GetLowestPrice(BarsLookback);
			
			return false;
		}
		
		public override void SetTradeAction() {
			CheckIndicatorSignals();
		}
				
		protected override bool PatternMatched()
		{
			//Print("CurrentBar, barsMaxLastCross, barsAgoMaxPbSAREn,=" + CurrentBar + "," + barsAgoMaxPbSAREn + "," + barsSinceLastCross);
//			if (giParabSAR.IsSpvAllowed4PAT(curBarPriceAction.paType) && barsSinceLastCross < barsAgoMaxPbSAREn) 
//				return true;
//			else return false;
			PriceAction pa = indicatorProxy.GetPriceAction(Time[0]);
			indicatorProxy.PrintLog(true, IsLiveTrading(), CurrentBar + ":"
				+ ";ToShortDateString=" + Time[0].ToString()
				+ ";paType=" + pa.paType.ToString()
				+ ";maxDownTicks=" + pa.maxDownTicks
				);
			return false;
			//barsAgoMaxPbSAREn Bars Since PbSAR reversal. Enter the amount of the bars ago maximum for PbSAR entry allowed
		}
		
		public override CurrentTrade CheckNewEntryTrade() {
			indicatorProxy.PrintLog(true, IsLiveTrading(), "===========CheckNewEntryTrade()===" + this.Name);
			indicatorProxy.TraceMessage(this.Name, PrintOut);
			CurrentTrade.InitNewEntryTrade();
			SetTradeAction();
//			if(GetTradeSignal(CurrentBar) != null) {
//				if(GetTradeSignal(CurrentBar).TrendDir.TrendDir == TrendDirection.Down)
//				{
//					indicatorProxy.TraceMessage(this.Name, PrintOut);
//					CurrentTrade.tradeDirection = TradingDirection.Down;
//				}
//				else if(GetTradeSignal(CurrentBar).TrendDir.TrendDir == TrendDirection.Up)
//				{
//					indicatorProxy.TraceMessage(this.Name, PrintOut);
//					CurrentTrade.tradeDirection = TradingDirection.Up;
//				}
				
//				CurrentTrade.tradeStyle = TradingStyle.TrendFollowing;
				
//			} else {
//				CurrentTrade.CurrentTradeType = TradeType.NoTrade);
//			}
			return CurrentTrade;
		}
		
//		public override void PutTrade(){
//			indicatorProxy.TraceMessage(this.Name, PrintOut);
//			if(CurrentTrade.CurrentTradeType == TradeType.Entry) {
//				indicatorProxy.PrintLog(true, IsLiveTrading(), "PutTrade CurrentTrade.stopLossAmt=" + CurrentTrade.stopLossAmt + "," + MM_StopLossAmt);
//				if(CurrentTrade.tradeDirection == TradingDirection.Down) {
//					indicatorProxy.PrintLog(true, IsLiveTrading(), "PutTrade Down OrderSignalName=" + CurrentTrade.TradeAction.EntrySignal.SignalName);
//					CurrentTrade.TradeAction.EntryPrice = GetTypicalPrice(0);
//					NewShortLimitOrderUM(OrderSignalName.EntryShortLmt.ToString());
//				}
//				else if(CurrentTrade.tradeDirection == TradingDirection.Up) {
//					indicatorProxy.PrintLog(true, IsLiveTrading(), "PutTrade Up OrderSignalName=" + CurrentTrade.TradeAction.EntrySignal.SignalName);
//					CurrentTrade.TradeAction.EntryPrice = GetTypicalPrice(0);
//					NewLongLimitOrderUM(OrderSignalName.EntryLongLmt.ToString());
//				}				
//			}
//		}

        #region Custom Properties
		private const int ODG_EnBarsBeforeInflection = 1;
		private const int ODG_BarsLookback = 2;
		private const int ODG_EMAPeriod1 = 3;
		private const int ODG_EMAPeriod2 = 4;
		private const int ODG_Range = 5;
		private const int ODG_SMITMAPeriod = 6;
		private const int ODG_SMICrossLevel = 7;
		private const int ODG_FastPeriod = 8;
		private const int ODG_SlowPeriod = 9;
		private const int ODG_Smooth = 10;
		private const int ODG_FastKAMA = 11;
		private const int ODG_SlowKAMA = 12;
		private const int ODG_PeriodKAMA = 13;
		
        [Description("Bars count before inflection for entry")]
 		[Range(0, double.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "EnBarsBeforeInflection", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_EnBarsBeforeInflection)]
        public int EnBarsBeforeInflection
        {
            get { return cp_EnBarsBeforeInflection; }
            set { cp_EnBarsBeforeInflection = Math.Max(1, value); }
        }
		
		[Description("Bars lookback period")]
 		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "BarsLookback", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_BarsLookback)]
        public int BarsLookback
        {
            get { return barsLookback;}
            set { barsLookback = Math.Max(1, value);}
        }

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="EMAPeriod1(SMI)", Description="1st ema smothing period. ( R )", Order=ODG_EMAPeriod1, GroupName=GPS_CUSTOM_PARAMS)]
		public int EMAPeriod1
		{
			get { return emaperiod1;}
			set { emaperiod1 = Math.Max(1, value);}
		}

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="EMAPeriod2(SMI)", Description="2nd ema smoothing period. ( S )", Order=ODG_EMAPeriod2, GroupName=GPS_CUSTOM_PARAMS)]
		public int EMAPeriod2
		{
			get { return emaperiod2;}
			set { emaperiod2 = Math.Max(1, value);}
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Range(SMI)", Description="Range for momentum Calculation ( Q )", Order=ODG_Range, GroupName=GPS_CUSTOM_PARAMS)]
		public int Range
		{
			get { return range;}
			set { range = Math.Max(1, value);}
		}		

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="SMITMAPeriod", Description="SMI TMA smoothing period", Order=ODG_SMITMAPeriod, GroupName=GPS_CUSTOM_PARAMS)]
		public int SMITMAPeriod
		{
			get { return smitmaperiod;}
			set { smitmaperiod = Math.Max(1, value);}
		}

		[Range(0, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="SMICrossLevel", Description="SMI&TMA Cross Level", Order=ODG_SMICrossLevel, GroupName=GPS_CUSTOM_PARAMS)]
		public int SMICrossLevel
		{
			get { return smiCrossLevel;}
			set { smiCrossLevel = Math.Max(0, value);}
		}
		
		/// <summary>
		/// </summary>
		//[Description("Period for fast EMA")]
		//[Category("Parameters")]
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "FastPeriod(AO)", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_FastPeriod)]		
		public int FastPeriod
		{
			//get;set;
			get { return fastPeriod; }
			set { fastPeriod = Math.Max(1, value); }
		}

		/// <summary>
		/// </summary>
		//[Description("Period for slow EMA")]
		//[Category("Parameters")]
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "SlowPeriod(AO)", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_SlowPeriod)]
		public int SlowPeriod
		{
			get { return slowPeriod; }
			set { slowPeriod = Math.Max(1, value); }
		}

		/// <summary>
		/// </summary>
//		[Description("Period for Smoothing of Signal Line")]
//		[Category("Parameters")]
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth(AO)", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_Smooth)]		
		public int Smooth
		{
			get { return smooth; }
			set { smooth = Math.Max(1, value); }
		}

		[Range(1, 125), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Fast(KAMA)", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_FastKAMA)]
		public int FastKAMA
		{ 
			get {return fastKAMA;}
			set {fastKAMA = Math.Max(1, value);}
		}

		[Range(5, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period(KAMA)", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_PeriodKAMA)]
		public int PeriodKAMA
		{ 
			get {return periodKAMA;}
			set {periodKAMA = Math.Max(5, value);}
		}

		[Range(1, 125), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Slow(KAMA)", GroupName = GPS_CUSTOM_PARAMS, Order = ODG_SlowKAMA)]
		public int SlowKAMA
		{ 
			get {return slowKAMA;}
			set {slowKAMA = Math.Max(1, value);}
		}
		
		private int cp_EnBarsBeforeInflection = 2;
				
		private int barsLookback = 1;// 15;
		
		//SMI parameters
		private int	range		= 5;
		private int	emaperiod1	= 3;
		private int	emaperiod2	= 5;
		private int smitmaperiod= 8;
		private int tmaperiod= 6;
		private int smiCrossLevel = 50;
		
		//AWO parameters
		private int fastPeriod 			= 5;
        private int slowPeriod 			= 34;
		private int smooth		 		= 5;
		
		//KAMA parameters
		private int fastKAMA 			= 2;
        private int slowKAMA 			= 10;
		private int periodKAMA	 		= 30;

		#endregion
	}
}
