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
using NinjaTrader.NinjaScript.Indicators.ZTraderInd;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.AddOns;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class GISMI : GIndicatorBase
	{
		private int	range		= 5;
		private int	emaperiod1	= 3;
		private int	emaperiod2	= 5;
		private int smitmaperiod= 8;
		private int tmaperiod= 6;
		
		private Series<double>	sms;
		private Series<double>	hls;
		private Series<double>	smis;
		private Series<double>	tma;
		
		private GLastIndexRecorder<double> inflectionRecorder;
		private GLastIndexRecorder<double> crossoverRecorder;
		private Series<int> inflection;
		private Series<int> crossover;
		//The barNo of last inflection
		private int lastInflection = -1;
		private int lastCrossover = -1;
				
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Print("GISMI set defaults called....");
				Description									= @"SMI.";
				Name										= "GISMI";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= true;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
				MaximumBarsLookBack = MaximumBarsLookBack.Infinite;

				AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Line, "SMI");
				AddPlot(new Stroke(Brushes.Yellow, 4), PlotStyle.Line, "SMITMA");
				AddLine(Brushes.DarkGray, 1, "ZeroLine");
			}
			else if (State == State.Configure)
			{
				Print("TickSize, Pointvalue=" + TickSize + "," + Bars.Instrument.MasterInstrument.PointValue);
				//stochastic momentums
				sms		= new Series<double>(this);
				//high low diffs
				hls		= new Series<double>(this);
				//stochastic momentum indexes
				smis	= new Series<double>(this);
				
				//Time series MA for trend indentification
				tma	= new Series<double>(this);
				
			} else if(State == State.DataLoaded)
			{
				//Save the inflection bar;
				inflection = new Series<int>(this, MaximumBarsLookBack.Infinite);
				inflectionRecorder = new GLastIndexRecorder<double>(this);
				
				//Save the crossover bar;
				crossover = new Series<int>(this, MaximumBarsLookBack.Infinite);
				crossoverRecorder = new GLastIndexRecorder<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			//Print(CurrentBar.ToString() + " -- GISMI OnBarUpdate called");
			if (( CurrentBar < emaperiod2) || ( CurrentBar < emaperiod1)) 
			{
				return;
			}
			
			//inflectionRecorder.PrintRecords();
			//Stochastic Momentum = SM {distance of close - midpoint}
		 	sms[0] = (Close[0] - 0.5 * ((MAX(High, range)[0] + MIN(Low, range)[0])));
			
			//High low diffs
			hls[0] = (MAX(High, range)[0] - MIN(Low, range)[0]);

			//Stochastic Momentum Index = SMI
			double denom = 0.5*EMA(EMA(hls,emaperiod1),emaperiod2)[0];
 			smis[0] = (100*(EMA(EMA(sms,emaperiod1),emaperiod2))[0] / (denom ==0 ? 1 : denom  ));
			
			//Set the current SMI line value
			smi[0] = (smis[0]);
			
			//Set the line value for the SMIEMA by taking the EMA of the SMI
			//SMIEMA[0] = (EMA(smis, smiemaperiod)[0]);
			SMITMA[0] = TSF(smis, 3, smitmaperiod)[0];
			tma[0] = TSF(Close, 3, tmaperiod)[0];
			inflection[0] = 0;
			crossover[0] = 0;
			if (CurrentBar > BarsRequiredToPlot) {//BarsRequiredToPlot) {
				int tr = GetTrendByMA();
				int infl = GetInflection(SMITMA);
				
				if(tr > 0) PlotBrushes[1][0] = Brushes.Green;
				else if(tr < 0) PlotBrushes[1][0] = Brushes.Red;
				
				if(infl > 0) {
					inflection[1] = 1;
					LastInflection = CurrentBar - 1;
					inflectionRecorder.AddLastIndexRecord(new GLastIndexRecord<double>(LastInflection, LookbackBarType.Up));
					DrawDiamond(1, "res"+CurrentBar, (3*High[1]-Low[1])/2, 0, Brushes.Red);
				}
				else if (infl < 0) {
					inflection[1] = -1;
					LastInflection = CurrentBar - 1;
					inflectionRecorder.AddLastIndexRecord(new GLastIndexRecord<double>(LastInflection, LookbackBarType.Down));
					DrawDiamond(1, "spt"+CurrentBar, (3*Low[1]-High[1])/2, 0, Brushes.Aqua);
				}
				
				if(CrossAbove(SMITMA, smi, 1)) {
					crossover[0] = 1;
					LastCrossover = CurrentBar;
					crossoverRecorder.AddLastIndexRecord(new GLastIndexRecord<double>(LastCrossover, LookbackBarType.Up));
					Draw.Text(this, CurrentBar.ToString(), CurrentBar.ToString() + "\r\nX", 0, High[0]+5, Brushes.Black);
				} else if (CrossBelow(SMITMA, smi, 1)) {
					crossover[0] = -1;
					LastCrossover = CurrentBar;
					crossoverRecorder.AddLastIndexRecord(new GLastIndexRecord<double>(LastCrossover, LookbackBarType.Down));
					Draw.Text(this, CurrentBar.ToString(), CurrentBar.ToString() + "\r\nX", 0, Low[0]-5, Brushes.Black);
				}
			}
			if(CurrentBar > BarsRequiredToPlot && IsLastBarOnChart() > 0) {
				Print("BarsRequiredToPlot=" + BarsRequiredToPlot);
				for(int i=0; i<inflection.Count; i++){
					Print("Inflection[" + i + "]=" + inflection.GetValueAt(i) + " -- Crossover[" + i + "]=" + crossover.GetValueAt(i));
				}
			}
		}
		
		/**
			ma:= Mov(C,6,T);
			bullish:= ma >= Ref(ma,-5)*1.004;
			bearish:= ma <= Ref(ma,-5)*0.996;
		*/
		private int GetTrendByMA(){
			int tr = 0;			
			if (CurrentBar > 20) {// BarsRequiredToPlot) {
				if(tma[0] >= 1.004*tma[5])
					tr = 1;
			else if(tma[0] <= 0.996*tma[5])
					tr = -1;
			}
			return tr;
		}
		
		private int GetInflection(ISeries<double> d){
			int inft = 0;//inflection[0] = 0;

			if(d[1].ApproxCompare(d[0]) > 0 && d[1].ApproxCompare(d[2]) > 0) 
				inft = 1;//inflection[1] = 1;
			else if(d[1].ApproxCompare(d[0]) < 0 && d[1].ApproxCompare(d[2]) < 0)
				inft = -1;//inflection[1] = -1;
			//Print("inft=" + (CurrentBar-1).ToString() + "," + inft);
			return inft;//inflection[1];
		}
		
		public Series<int> GetInflection() {
			return inflection;
		}
		
		public bool IsNewInflection(TrendDirection trendDir) {
			return LastInflection == GetLastInflection(GetInflection(), CurrentBar, trendDir, BarIndexType.BarNO);
		}
		
		public Series<int> GetCrossover() {
			return crossover;
		}
		
		public override Direction GetDirection() {			
			//Print(CurrentBar.ToString() + " -- GISMI GetDirection called");			
			Direction dir = new Direction();
			if(GetTrendByMA() > 0) dir.TrendDir = TrendDirection.Up;
			else if (GetTrendByMA() < 0) dir.TrendDir = TrendDirection.Down;
			//Print(CurrentBar.ToString() + " -- GISMI GetTrendByMA(), GetDirection=" + GetTrendByMA() + "," + dir.TrendDir.ToString());
			return dir;
		}
		
		public override SupportResistanceBar GetSupportResistance(int barNo, SupportResistanceType srType) {
			SupportResistanceBar snr = new SupportResistanceBar();
			
			if(srType == SupportResistanceType.Resistance) {
				GLastIndexRecord<double> rec = this.crossoverRecorder.GetLastIndexRecord(barNo, LookbackBarType.Unknown);
				int lcrs = -1;
				if(rec != null)
					lcrs = rec.BarNumber;
				if(barNo > 17600 && barNo < 17650)
					Print(CurrentBar + "-LastCrossover, barNo=" + lcrs + "," + barNo);
			//isolate the last inflection 
				//LastInflection = GetLastInflection(GetInflection(), CurrentBar, TrendDirection.Down, BarIndexType.BarNO);
			
			//lookback to the crossover and if that candle is bearish we isolate the open as resistance;
			// if that candlestick is bullish we isolate the close as resistance
				//LastCrossover = GetLastCrossover(GetCrossover(), LastInflection, CrossoverType.Both, BarIndexType.BarsAgo);
				if(lcrs > 0) {
					double open_lcrs = Open.GetValueAt(lcrs);
					double close_lcrs = Close.GetValueAt(lcrs);
					snr.BarNo = lcrs;
					snr.SnRType = SupportResistanceType.Resistance;
					snr.SnRPriceType = open_lcrs > close_lcrs ? PriceSubtype.Open : PriceSubtype.Close;
					//snr.SetSptRstValue(Math.Max(open_lcrs,close_lcrs));
				}
			}
			return snr;
		}

		
		private void DrawDiamond(int barsBack, string tag, double prc, double offset, SolidColorBrush brush) {				
			// Instantiates a red diamond on the current bar 1 tick below the low
			Diamond myDiamond = Draw.Diamond(this, tag, false, barsBack, prc+offset, brush);
			 
			// Set the area fill color to Red
			myDiamond.AreaBrush = brush;//Brushes.Red;
		}
		
		#region Properties
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="EMAPeriod1", Description="1st ema smothing period. ( R )", Order=1, GroupName="Parameters")]
		public int EMAPeriod1
		{
			get { return emaperiod1;}
			set { emaperiod1 = Math.Max(1, value);}
		}

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="EMAPeriod2", Description="2nd ema smoothing period. ( S )", Order=2, GroupName="Parameters")]
		public int EMAPeriod2
		{
			get { return emaperiod2;}
			set { emaperiod2 = Math.Max(1, value);}
		}
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="Range", Description="Range for momentum Calculation ( Q )", Order=3, GroupName="Parameters")]
		public int Range
		{
			get { return range;}
			set { range = Math.Max(1, value);}
		}		

		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="SMITMAPeriod", Description="SMI TMA smoothing period", Order=4, GroupName="Parameters")]
		public int SMITMAPeriod
		{
			get { return smitmaperiod;}
			set { smitmaperiod = Math.Max(1, value);}
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> smi
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> SMITMA
		{
			get { return Values[1];}
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public int LastInflection
		{
			get { return lastInflection;}
			
			set {lastInflection = value;}
		}

		[Browsable(false)]
		[XmlIgnore]
		public GLastIndexRecorder<double> InflectionRecorder
		{
			get { return inflectionRecorder;}
			
			set {inflectionRecorder = value;}
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public int LastCrossover
		{
			get { return lastCrossover;}
			
			set {lastCrossover = value;}
		}
		
		#endregion		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GISMI[] cacheGISMI;
		public GISMI GISMI(int eMAPeriod1, int eMAPeriod2, int range, int sMITMAPeriod)
		{
			return GISMI(Input, eMAPeriod1, eMAPeriod2, range, sMITMAPeriod);
		}

		public GISMI GISMI(ISeries<double> input, int eMAPeriod1, int eMAPeriod2, int range, int sMITMAPeriod)
		{
			if (cacheGISMI != null)
				for (int idx = 0; idx < cacheGISMI.Length; idx++)
					if (cacheGISMI[idx] != null && cacheGISMI[idx].EMAPeriod1 == eMAPeriod1 && cacheGISMI[idx].EMAPeriod2 == eMAPeriod2 && cacheGISMI[idx].Range == range && cacheGISMI[idx].SMITMAPeriod == sMITMAPeriod && cacheGISMI[idx].EqualsInput(input))
						return cacheGISMI[idx];
			return CacheIndicator<GISMI>(new GISMI(){ EMAPeriod1 = eMAPeriod1, EMAPeriod2 = eMAPeriod2, Range = range, SMITMAPeriod = sMITMAPeriod }, input, ref cacheGISMI);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GISMI GISMI(int eMAPeriod1, int eMAPeriod2, int range, int sMITMAPeriod)
		{
			return indicator.GISMI(Input, eMAPeriod1, eMAPeriod2, range, sMITMAPeriod);
		}

		public Indicators.GISMI GISMI(ISeries<double> input , int eMAPeriod1, int eMAPeriod2, int range, int sMITMAPeriod)
		{
			return indicator.GISMI(input, eMAPeriod1, eMAPeriod2, range, sMITMAPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GISMI GISMI(int eMAPeriod1, int eMAPeriod2, int range, int sMITMAPeriod)
		{
			return indicator.GISMI(Input, eMAPeriod1, eMAPeriod2, range, sMITMAPeriod);
		}

		public Indicators.GISMI GISMI(ISeries<double> input , int eMAPeriod1, int eMAPeriod2, int range, int sMITMAPeriod)
		{
			return indicator.GISMI(input, eMAPeriod1, eMAPeriod2, range, sMITMAPeriod);
		}
	}
}

#endregion
