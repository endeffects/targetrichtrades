//
// Copyright (C) 2019, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Displays net change on the chart.
	/// </summary>
	public class GINetChg : GIndicatorBase
	{
		private Cbi.Account 	account;
		private Cbi.Instrument 	instrument;
		private double 			currentValue;
		private double 			lastValue;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionNetChangeDisplay;
				Name						= "GINetChg";
				Calculate					= Calculate.OnPriceChange;
				IsOverlay					= true;
				DrawOnPricePanel			= true;
				IsSuspendedWhileInactive	= true;
				Unit 						= Cbi.PerformanceUnit.Currency;
				PositiveBrush 				= Brushes.LimeGreen;
				NegativeBrush 				= Brushes.Red;
				Location 					= NetChgPosition.TopRight;
				Font 						= new SimpleFont("Arial", 18);
			}
			else if (State == State.Configure)
			{
				instrument = Instruments[0];
			}
		}

		private TextPosition GetTextPosition(NetChgPosition ncp)
		{
			switch(ncp)
			{
				case NetChgPosition.BottomLeft:
					return TextPosition.BottomLeft;
				case NetChgPosition.BottomRight:
					return TextPosition.BottomRight;
				case NetChgPosition.TopLeft:
					return TextPosition.TopLeft;
				case NetChgPosition.TopRight:
					return TextPosition.TopRight;
			}

			return TextPosition.TopRight;
		}

		protected override void OnBarUpdate() { }
		
		protected override void OnConnectionStatusUpdate(Cbi.ConnectionStatusEventArgs connectionStatusUpdate)
		{
			if (connectionStatusUpdate.PriceStatus == Cbi.ConnectionStatus.Connected && connectionStatusUpdate.PreviousStatus == Cbi.ConnectionStatus.Connecting
					&& connectionStatusUpdate.Connection.Accounts.Count > 0
					&& account == null)
				account = connectionStatusUpdate.Connection.Accounts[0];
			else if (connectionStatusUpdate.Status == Cbi.ConnectionStatus.Disconnected && connectionStatusUpdate.PreviousStatus == Cbi.ConnectionStatus.Disconnecting
					&& account != null && account.Connection == connectionStatusUpdate.Connection)
				account = null;
		}

		protected override void OnMarketData(Data.MarketDataEventArgs marketDataUpdate)
		{
			if (marketDataUpdate.IsReset)
			{
				currentValue = double.MinValue;
				
				if (lastValue != currentValue)
				{
					Draw.TextFixed(this, "NinjaScriptInfo", FormatValue(currentValue), GetTextPosition(Location), currentValue >= 0 ? PositiveBrush : NegativeBrush, Font, Brushes.Transparent, Brushes.Transparent, 0);
					lastValue = currentValue;
				}
				return;
			}

			if (marketDataUpdate.MarketDataType != Data.MarketDataType.Last || marketDataUpdate.Instrument.MarketData.LastClose == null)
				return;

			bool 	tryAgainLater 	= false;
			double 	rate 			= 0;
			
			if (account != null)
				rate = marketDataUpdate.Instrument.GetConversionRate(Data.MarketDataType.Bid, account.Denomination, out tryAgainLater);

			switch (Unit)
			{
				case Cbi.PerformanceUnit.Percent:
					currentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / marketDataUpdate.Instrument.MarketData.LastClose.Price;
					break;
				case Cbi.PerformanceUnit.Pips:
					currentValue = ((marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / Instrument.MasterInstrument.TickSize) * (Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.Forex ? 0.1 : 1);
					break;
				case Cbi.PerformanceUnit.Ticks:
					currentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) / Instrument.MasterInstrument.TickSize;
					break;
				case Cbi.PerformanceUnit.Currency:
					currentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price) * Instrument.MasterInstrument.PointValue * rate * (Instrument.MasterInstrument.InstrumentType == Cbi.InstrumentType.Forex ? (account != null ? account.ForexLotSize : Cbi.Account.DefaultLotSize) : 1);
					break;
				case Cbi.PerformanceUnit.Points:
					currentValue = (marketDataUpdate.Price - marketDataUpdate.Instrument.MarketData.LastClose.Price);
					break;
			}
			
			if (lastValue != currentValue)
			{
				Draw.TextFixed(this, "NinjaScriptInfo", FormatValue(currentValue), GetTextPosition(Location), currentValue >= 0 ? PositiveBrush : NegativeBrush, Font, Brushes.Transparent, Brushes.Transparent, 0);
				lastValue = currentValue;
			}
		}
		
		public string FormatValue(double value)
		{
			if (value == double.MinValue)
				return string.Empty;

			switch (Unit)
			{
				case Cbi.PerformanceUnit.Currency:
					{
                        Cbi.Currency formatCurrency;
                        if (account != null)
                            formatCurrency = account.Denomination;
                        else
                            formatCurrency = Instrument.MasterInstrument.Currency;
                        return Core.Globals.FormatCurrency(value, formatCurrency);
                    }
				case Cbi.PerformanceUnit.Points: return value.ToString(Core.Globals.GetTickFormatString(Instrument.MasterInstrument.TickSize), Core.Globals.GeneralOptions.CurrentCulture);
				case Cbi.PerformanceUnit.Percent: return (value).ToString("P", Core.Globals.GeneralOptions.CurrentCulture);
				case Cbi.PerformanceUnit.Pips:
					{
						System.Globalization.CultureInfo forexCulture = Core.Globals.GeneralOptions.CurrentCulture.Clone() as System.Globalization.CultureInfo;
						if (forexCulture != null)
							forexCulture.NumberFormat.NumberDecimalSeparator = "'";
						return (Math.Round(value * 10) / 10.0).ToString("0.0", forexCulture);
					}
				case Cbi.PerformanceUnit.Ticks: return Math.Round(value).ToString(Core.Globals.GeneralOptions.CurrentCulture);
				default: return "0";
			}
		}
		
		#region Properties
		[XmlIgnore]
		[Browsable(false)]
		public double NetChange {  get { return currentValue; } }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Unit", GroupName = "NinjaScriptParameters", Order = 0)]
		public Cbi.PerformanceUnit Unit { get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "PositiveColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1810)]
		public Brush PositiveBrush { get; set; }
		
		[Browsable(false)]
		public string PositiveBrushSerialize
		{
			get { return Serialize.BrushToString(PositiveBrush); }
		   	set { PositiveBrush = Serialize.StringToBrush(value); }
		}
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "NegativeColor", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1820)]
		public Brush NegativeBrush { get; set; }
		
		[Browsable(false)]
		public string NegativeBrushSerialize
		{
			get { return Serialize.BrushToString(NegativeBrush); }
		   	set { NegativeBrush = Serialize.StringToBrush(value); }
		}
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Location", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1830)]
		public NetChgPosition Location { get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Font", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 1800)]
		public SimpleFont Font { get; set; }
		#endregion
	}
}

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum NetChgPosition
{
	BottomLeft,
	BottomRight,
	TopLeft,
	TopRight,
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private GINetChg[] cacheGINetChg;
		public GINetChg GINetChg(Cbi.PerformanceUnit unit, NetChgPosition location)
		{
			return GINetChg(Input, unit, location);
		}

		public GINetChg GINetChg(ISeries<double> input, Cbi.PerformanceUnit unit, NetChgPosition location)
		{
			if (cacheGINetChg != null)
				for (int idx = 0; idx < cacheGINetChg.Length; idx++)
					if (cacheGINetChg[idx] != null && cacheGINetChg[idx].Unit == unit && cacheGINetChg[idx].Location == location && cacheGINetChg[idx].EqualsInput(input))
						return cacheGINetChg[idx];
			return CacheIndicator<GINetChg>(new GINetChg(){ Unit = unit, Location = location }, input, ref cacheGINetChg);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.GINetChg GINetChg(Cbi.PerformanceUnit unit, NetChgPosition location)
		{
			return indicator.GINetChg(Input, unit, location);
		}

		public Indicators.GINetChg GINetChg(ISeries<double> input , Cbi.PerformanceUnit unit, NetChgPosition location)
		{
			return indicator.GINetChg(input, unit, location);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.GINetChg GINetChg(Cbi.PerformanceUnit unit, NetChgPosition location)
		{
			return indicator.GINetChg(Input, unit, location);
		}

		public Indicators.GINetChg GINetChg(ISeries<double> input , Cbi.PerformanceUnit unit, NetChgPosition location)
		{
			return indicator.GINetChg(input, unit, location);
		}
	}
}

#endregion
