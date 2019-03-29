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
using NinjaTrader.NinjaScript.Indicators.ZTraderInd;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Strategies.ZTraderStg;
using NinjaTrader.NinjaScript.AddOns;

#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class StgSnR : GSZTraderBase
	{
		private GISMI giSMI;
		private GISnR giSnR;
		
		protected override void OnStateChange()
		{
			base.OnStateChange();
			if (State == State.SetDefaults)
			{
				Description									= @"Strategy based on support and resistance, combined with fibonacci ratios.";
				Name										= "StgSnR";
				Calculate									= Calculate.OnBarClose;
//				EntriesPerDirection							= 1;
//				EntryHandling								= EntryHandling.AllEntries;
//				IsExitOnSessionCloseStrategy				= true;
//				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
//				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite;
//				OrderFillResolution							= OrderFillResolution.Standard;
//				Slippage									= 0;
//				StartBehavior								= StartBehavior.WaitUntilFlat;
//				TimeInForce									= TimeInForce.Day;
				TraceOrders									= false;
//				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
//				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
//				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
//				IsInstantiatedOnEachOptimizationIteration	= true;
				ShowLastDayHL								= false;
				ShowOpenHL									= true;
				ShowOvernightHL								= false;
				//AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Hash, "Spt");
				//AddPlot(new Stroke(Brushes.Tomato, 2), PlotStyle.Hash, "Rst");
			}
			else if (State == State.DataLoaded)
			{
				//indicatorProxy = new GIndicatorBase();
				//indicatorProxy = GIndicatorProxy(1);
				giSMI = GISMI(3, 5, 5, 8);
				giSnR = GISnR(ShowOvernightHL, ShowOpenHL, ShowLastDayHL);
				//awOscillator = GIAwesomeOscillator(5, 34, 5, MovingAvgType.SMA);

				AddChartIndicator(giSMI);
				AddChartIndicator(giSnR);
				AddChartIndicator(indicatorProxy);
			}			
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Day, 1);
				//IncludeCommission = true;
				tradeObj = new TradeObj(this);
				tradeObj.barsSincePTSL = TM_BarsSincePTSL;
			}			
		}

		protected override void OnBarUpdate()
		{	
			if (BarsInProgress != 0) return;
			
			//Print(CurrentBar.ToString() + " -- StgTRT - Add your custom strategy logic here.");
			indicatorProxy.Update();
			
			indicatorSignal = GetIndicatorSignal();

			base.OnBarUpdate();
			//SetStopLoss(CalculationMode.Price, tradeObj.stopLossPrice);
			//CheckExitTrade();
			//CheckEntryTrade();
			//PutTrade();
		}

		public override Direction GetDirection(GIndicatorBase ind){
			return ind.GetDirection();
		}
		
		public override IndicatorSignal GetIndicatorSignal() {
			giSMI.Update();
			giSnR.Update();
			IndicatorSignal indSignal = null; //= new IndicatorSignal();
			
			if(CurrentBar <= BarsRequiredToTrade) return indSignal;
			
			int infl = giSMI.GetInflection(giSMI.SMITMA);
			
			if(infl != 0) {
				//Check divergence for CurrentBar with the last infl bar;
				DivergenceType dvType = CheckDivergence(giSMI);
				if(infl < 0 && dvType == DivergenceType.Divergent) {
					indSignal = new IndicatorSignal();
					indSignal.BarNo = CurrentBar;
					indSignal.ReversalDir = Reversal.Down;
					indSignal.SnR = giSMI.GetSupportResistance(CurrentBar-1);
				}//entry short by reversal
				else if(infl > 0 && dvType == DivergenceType.Convergent) {
					indSignal = new IndicatorSignal();
					indSignal.BarNo = CurrentBar;
					Direction dir = new Direction();
					dir.TrendDir = TrendDirection.Down;
					indSignal.TrendDir = dir;
					indSignal.SnR = giSMI.GetSupportResistance(CurrentBar-1);
				}//entry short following down trend
				else if(infl < 0 && dvType == DivergenceType.Convergent) {
					indSignal = new IndicatorSignal();
					indSignal.BarNo = CurrentBar;
					Direction dir = new Direction();
					dir.TrendDir = TrendDirection.Up;
					indSignal.TrendDir = dir;
					indSignal.SnR = giSMI.GetSupportResistance(CurrentBar-1);
				}//entry long following up trend
				else if(infl > 0 && dvType == DivergenceType.Divergent) {
					indSignal = new IndicatorSignal();
					indSignal.BarNo = CurrentBar;
					indSignal.ReversalDir = Reversal.Up;
					indSignal.SnR = giSMI.GetSupportResistance(CurrentBar-1);
				}//entry long by reversal
			}		
			return indSignal;
		}
		
		public override TradeObj CheckNewEntryTrade() {
			if(NewOrderAllowed() && indicatorSignal != null) { //|| Position.Quantity == 0 giSMI.IsNewInflection(TrendDirection.Down) && 
				if(indicatorSignal.TrendDir != null && indicatorSignal.TrendDir.TrendDir == TrendDirection.Down
					&& indicatorProxy.GetResistance(indicatorSignal.SnR.Resistance) > High[0]) {
					tradeObj.tradeDirection = TradingDirection.Down;
					tradeObj.tradeStyle = TradingStyle.TrendFollowing;
					tradeObj.SetTradeType(TradeType.Entry);
					tradeObj.BracketOrder.EnOrderType = OrderType.StopMarket;
					tradeObj.PTCalculationMode = CalculationMode.Currency;
					tradeObj.profitTargetAmt = MM_ProfitTargetAmt;
					tradeObj.SLCalculationMode = CalculationMode.Price;
					tradeObj.stopLossPrice = indicatorProxy.GetResistance(indicatorSignal.SnR.Resistance);
					tradeObj.barsSincePTSL = TM_BarsSincePTSL;
					tradeObj.profitFactor = MM_ProfitFactor;
				}
				if(indicatorSignal != null && indicatorSignal.TrendDir.TrendDir == TrendDirection.Down) {
					//Print(CurrentBar + ": GetResistance=" + indicatorProxy.GetResistance(indicatorSignal.SnR) + ", SnR.BarNo=" + indicatorSignal.SnR.BarNo + ", SnRPriceType=" + indicatorSignal.SnR.SnRPriceType);
				}
			} else {
				tradeObj.SetTradeType(TradeType.NoTrade);
			}
			return tradeObj;
		}
		
		public override void PutTrade() {
			Print(CurrentBar + ":" + this.ToString());
			if(tradeObj.GetTradeType() == TradeType.Entry) {
				if(tradeObj.tradeDirection == TradingDirection.Up)
					NewEntryLongOrder();
			}
		}
		
		public override DivergenceType CheckDivergence(GIndicatorBase indicator) {
			
			return indicator.CheckDivergence();
		}
		

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="ShowOvernightHL", Description="Show the overnight High/Low", Order=1, GroupName="CustomParams")]
		public bool ShowOvernightHL
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ShowOpenHL", Description="Show Opening High/Low of today", Order=2, GroupName="CustomParams")]
		public bool ShowOpenHL
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ShowLastDayHL", Description="Show High/Low of last day", Order=3, GroupName="CustomParams")]
		public bool ShowLastDayHL
		{ get; set; }
		
        [Description("Bars count before inflection for entry")]
 		[Range(0, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "EnBarsBeforeInflection", GroupName = "CustomParams", Order = 4)]
        public int CP_EnBarsBeforeInflection
        {
            get { return cp_EnBarsBeforeInflection; }
            set { cp_EnBarsBeforeInflection = Math.Max(1, value); }
        }
		
		private int cp_EnBarsBeforeInflection = 2;		

//		[Browsable(false)]
//		[XmlIgnore]
//		public Series<double> Spt
//		{
//			get { return Values[0]; }
//		}

//		[Browsable(false)]
//		[XmlIgnore]
//		public Series<double> Rst
//		{
//			get { return Values[1]; }
//		}
		#endregion

	}
}
