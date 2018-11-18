// 
// Copyright (C) 2018, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
#endregion

//This namespace holds Market Analyzer columns in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public class NextYearsEarningsPerShare : MarketAnalyzerColumn
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description				= NinjaTrader.Custom.Resource.NinjaScriptMarketAnalyzerColumnDescriptionNextYearsEarningsPerShare;
				Name					= NinjaTrader.Custom.Resource.NinjaScriptMarketAnalyzerColumnNameNextYearsEarningsPerShare;
				IsDataSeriesRequired	= false;
			}
			else if (State == State.Realtime)
			{
				if (Instrument != null && Instrument.FundamentalData != null && Instrument.FundamentalData.NextYearsEarningsPerShare != null)
					CurrentValue = Instrument.FundamentalData.NextYearsEarningsPerShare.Value;
			}
		}

		protected override void OnFundamentalData(Data.FundamentalDataEventArgs fundamentalDataUpdate)
		{
			if (fundamentalDataUpdate.FundamentalDataType == Data.FundamentalDataType.NextYearsEarningsPerShare)
				CurrentValue = fundamentalDataUpdate.DoubleValue;
		}
	}
}
