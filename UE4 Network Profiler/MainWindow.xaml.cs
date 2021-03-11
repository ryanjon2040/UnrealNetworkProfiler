/************************************************************************/
/* 
 * This is a WPF rewrite of Network Profiler by Epic Games.
 * Original repository can be found here: https://github.com/EpicGames/UnrealEngine/tree/master/Engine/Source/Programs/NetworkProfiler 
 * 
 * Pull requests are welcome. 
 * =============================================
 * 
 * Written in WPF by Satheesh (ryanjon2040) 
 * Github :		https://github.com/ryanjon2040 
 * Twitter:		https://twitter.com/ryanjon2040
 * Facebook:	https://facebook.com/ryanjon2040
 * Discord:		ryanjon2040#5319
 */
/************************************************************************/

#define ENABLE_EXTRAS
#undef ENABLE_EXTRAS

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows;
using ScottPlot.Plottable;
using GameAnalyticsSDK.Net;
using System.Reflection;
using UE4_Network_Profiler.UserControls;
using HandyControl.Controls;

namespace NetworkProfiler
{
	public enum SeriesType : int
	{
		OutgoingBandwidthSize = 0,
		OutgoingBandwidthSizeSec,
		OutgoingBandwidthSizeAvgSec,
		ActorCount,
		PropertySize,
		PropertySizeSec,
		RPCSize,
		RPCSizeSec,
		Events,
		ActorCountSec,
		PropertyCount,
		PropertyCountSec,
		RPCCount,
		RPCCountSec,
		ExportBunchCount,
		ExportBunchSize,
		MustBeMappedGuidsCount,
		MustBeMappedGuidsSize,
		SendAckCount,
		SendAckCountSec,
		SendAckSize,
		SendAckSizeSec,
		ContentBlockHeaderSize,
		ContentBlockFooterSize,
		PropertyHandleSize,
		SendBunchCount,
		SendBunchCountSec,
		SendBunchSize,
		SendBunchSizeSec,
		SendBunchHeaderSize,
		GameSocketSendSize,
		GameSocketSendSizeSec,
		GameSocketSendCount,
		GameSocketSendCountSec,
		ActorReplicateTimeInMS,
#if ENABLE_EXTRAS
		MiscSocketSendSize,
		MiscSocketSendSizeSec,
		MiscSocketSendCount,
		MiscSocketSendCountSec,
#endif
	};

	public partial class MainWindow
	{
		// Please DO NOT change this. If you don't want Analytics, set both GAME_KEY and SECRET_KEY to null ///////////////////
		private static readonly string GAME_KEY = "81ebb126baf40ca75b9ce26e2f3e7ad2"; // null
		private static readonly string SECRET_KEY = "18f49fbf8da84dadaff7ca0ad76ab01f78771725"; // null
		///////////////////////////////////////////////////////////////////////////////////////////////

		/** Currently selected frame on chart			*/
		//int CurrentFrame = 0;

		///** Marks last selected range on chart			*/
		//int RangeSelectStart = -1;
		//int RangeSelectEnd = -1;

		/** Current network stream.						*/
		NetworkStream CurrentNetworkStream = null;

		PartialNetworkStream CurrentStreamSelection = null;

		FilterValues CurrentFilterValues = new FilterValues();

		Thread LoadThread = null;
		Thread SelectRangeThread = null;

		private Dialog aboutDialog = null;

		/** If non 0, we will early out of loading in this many minutes worth of profile time */
		int MaxProfileMinutes = 0;

		public Dictionary<int, SeriesTypeValues> DefaultSeriesTypes = new Dictionary<int, SeriesTypeValues>();

		public MainWindow()
		{
			InitializeComponent();
			SetDefaultLineView();
			NetworkChart.Plot.Legend(true, ScottPlot.Alignment.UpperLeft);
			DataContext = this;

			if (GAME_KEY != null && SECRET_KEY != null)
			{
				GameAnalytics.ConfigureBuild($"Unreal Network Profiler v{GetProductVersionString()}");
				GameAnalytics.Initialize(GAME_KEY, SECRET_KEY);

#if DEBUG
				GameAnalytics.AddDesignEvent("Program:Start:Debug");
#else
				GameAnalytics.AddDesignEvent("Program:Start:Release");
#endif
			}
		}

		private static string GetProductVersionString()
		{
			Version ProductVersion = Assembly.GetEntryAssembly().GetName().Version;
			string ReturnValue = $"{ProductVersion.Major}.{ProductVersion.Minor}";

			if (ProductVersion.Build > 0)
			{
				ReturnValue += $".{ProductVersion.Build}";
			}

			if (ProductVersion.Revision > 0)
			{
				ReturnValue += $".{ProductVersion.Revision}";
			}

			return ReturnValue;
		}

		private void SetDefaultLineView()
		{
			NetworkChart.Plot.Clear();
			ChartListBox.Items.Clear();
			DefaultSeriesTypes.Clear();

			RegisterChartSeries(SeriesType.OutgoingBandwidthSize, "Outgoing Bandwidth Bytes", false);
			RegisterChartSeries(SeriesType.OutgoingBandwidthSizeSec, "Outgoing Bandwidth Bytes/s", true);
			RegisterChartSeries(SeriesType.OutgoingBandwidthSizeAvgSec, "Outgoing Bandwidth Avg/s", true);
			RegisterChartSeries(SeriesType.ActorCount, "Actor Count", false);
			RegisterChartSeries(SeriesType.PropertySize, "Property Bytes", false);
			RegisterChartSeries(SeriesType.PropertySizeSec, "Property Bytes/s", true);
			RegisterChartSeries(SeriesType.RPCSize, "RPC Bytes", false);
			RegisterChartSeries(SeriesType.RPCSizeSec, "RPC Bytes/s", true);
			RegisterChartSeries(SeriesType.Events, "Events", false);

			RegisterChartSeries(SeriesType.ActorCountSec, "Actor Count/s", false);
			RegisterChartSeries(SeriesType.PropertyCount, "Property Count", false);
			RegisterChartSeries(SeriesType.PropertyCountSec, "Property Count/s", false);
			RegisterChartSeries(SeriesType.RPCCount, "RPC Count", false);
			RegisterChartSeries(SeriesType.RPCCountSec, "RPC Count/s", false);
			RegisterChartSeries(SeriesType.ExportBunchCount, "Export Bunch Count", false);
			RegisterChartSeries(SeriesType.ExportBunchSize, "Export Bunch Count/s", false);
			RegisterChartSeries(SeriesType.MustBeMappedGuidsCount, "Must Be Mapped Guid Count", false);
			RegisterChartSeries(SeriesType.MustBeMappedGuidsSize, "Must Be Mapped Guid Bytes", false);
			RegisterChartSeries(SeriesType.SendAckCount, "Send Ack Count", false);
			RegisterChartSeries(SeriesType.SendAckCountSec, "Send Ack Count/s", false);
			RegisterChartSeries(SeriesType.SendAckSize, "Send Ack Bytes", false);
			RegisterChartSeries(SeriesType.SendAckSizeSec, "Send Ack Bytes/s", false);
			RegisterChartSeries(SeriesType.ContentBlockHeaderSize, "Content Block Header Bytes", false);
			RegisterChartSeries(SeriesType.ContentBlockFooterSize, "Content Block Footer Bytes", false);
			RegisterChartSeries(SeriesType.PropertyHandleSize, "Property Handle Bytes", false);
			RegisterChartSeries(SeriesType.SendBunchCount, "Send Bunch Count", false);
			RegisterChartSeries(SeriesType.SendBunchCountSec, "Send Bunch Count/s", false);
			RegisterChartSeries(SeriesType.SendBunchSize, "Send Bunch Bytes", false);
			RegisterChartSeries(SeriesType.SendBunchSizeSec, "Send Bunch Bytes/s", false);
			RegisterChartSeries(SeriesType.SendBunchHeaderSize, "Send Bunch Header Bytes", false);
			RegisterChartSeries(SeriesType.GameSocketSendSize, "Game Socket Send Bytes", false);
			RegisterChartSeries(SeriesType.GameSocketSendSizeSec, "Game Socket Send Bytes/s", false);
			RegisterChartSeries(SeriesType.GameSocketSendCount, "Game Socket Send Count", false);
			RegisterChartSeries(SeriesType.GameSocketSendCountSec, "Game Socket Send Count/s", false);
			RegisterChartSeries(SeriesType.ActorReplicateTimeInMS, "Actor Replicate Time In MS", false);

#if ENABLE_EXTRAS
			RegisterChartSeries(SeriesType.MiscSocketSendSize, "Misc Socket Send Bytes", false);
			RegisterChartSeries(SeriesType.MiscSocketSendSizeSec, "Misc Socket Send Bytes/s", false);
			RegisterChartSeries(SeriesType.MiscSocketSendCount, "Misc Socket Send Count", false);
			RegisterChartSeries(SeriesType.MiscSocketSendCountSec, "Misc Socket Send Count/s", false);
#endif
		}

		protected void RegisterChartSeries(SeriesType Type, string FriendlyName, bool bEnabled)
		{
			CheckBox ChartListBoxItemCheckbox = new CheckBox();
			ChartListBoxItemCheckbox.Content = FriendlyName;
			ChartListBoxItemCheckbox.IsChecked = bEnabled;
			ChartListBoxItemCheckbox.Checked += ChartListBoxItemChecked;
			ChartListBoxItemCheckbox.Unchecked += ChartListBoxItemUnChecked;
			ChartListBox.Items.Add(ChartListBoxItemCheckbox);

			DefaultSeriesTypes.Add((int)Type, new SeriesTypeValues(Type, 0, 0, bEnabled));
		}
		
		private void ChartListBoxItemChecked(object sender, RoutedEventArgs e)
		{
			ChartListBoxItemToggle(true);
		}

		private void ChartListBoxItemUnChecked(object sender, RoutedEventArgs e)
		{
			ChartListBoxItemToggle(false);
		}

		private void ChartListBoxItemToggle(bool bCheck)
		{
			IPlottable[] Plots = NetworkChart.Plot.GetPlottables();

			for (int i = 0; i < ChartListBox.Items.Count; i++)
			{				
				DefaultSeriesTypes[i].bVisible = (bool)((CheckBox)ChartListBox.Items[i]).IsChecked;
				Plots[i].IsVisible = DefaultSeriesTypes[i].bVisible;
			}

			NetworkChart.Render();
		}

		public void AddChartPoint(SeriesType Type, double X, double Y)
		{
			DefaultSeriesTypes[(int)Type].Add(X, Y);
		}

		public void UpdateNetworkChart()
		{
			foreach (var T in DefaultSeriesTypes)
			{
				SignalPlotXY plotXY = NetworkChart.Plot.AddSignalXY(T.Value.GetArrayX(), T.Value.GetArrayY(), null, T.Value.ToString());
				plotXY.IsVisible = T.Value.bVisible;
			}
		}

		public void UpdateProgress(int Value)
		{
			if (Dispatcher.CheckAccess())
			{
				CurrentProgress.Value = Math.Max(0, Math.Min(100, Value));
				return;
			}

			Dispatcher.Invoke(new Action(() => UpdateProgress(Value)));
		}

		public void ShowProgress(bool bShow)
		{
			if (Dispatcher.CheckAccess())
			{
				CurrentProgress.Visibility = bShow ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
				CurrentProgress.Value = 0;
				return;
			}

			Dispatcher.Invoke(new Action(() => ShowProgress(bShow)));
		}

		private void ChangeNetworkStreamWorker(string Filename)
		{
			using (FileStream ParserStream = File.OpenRead(Filename))
			{
				try
				{
					CurrentNetworkStream = StreamParser.Parse(this, ParserStream);
					ParseStreamForListViews();
					ChartParser.ParseStreamIntoChart(this, CurrentNetworkStream, NetworkChart, CurrentFilterValues);
				}
				catch (System.Threading.ThreadAbortException)
				{

				}
				catch (System.Exception se)
				{
					Console.Out.WriteLine(se.StackTrace);
					ClearStreamAndChart();
				}
			}

			LoadThread = null;
		}

		private void CancelLoadThread()
		{
			if (LoadThread != null)
			{
				LoadThread.Abort();				
				LoadThread = null;
			}
		}

		private void ChangeNetworkStream(string Filename)
		{
			CancelLoadThread();

			LoadThread = new Thread(() => ChangeNetworkStreamWorker(Filename));
			LoadThread.Start();
		}

		public void ClearStreamAndChart()
		{
			if (Dispatcher.CheckAccess())
			{
				CurrentNetworkStream = null;
				NetworkChart.Plot.Clear();
				return;
			}

			Dispatcher.Invoke(new Action(() => ClearStreamAndChart()));
		}

		public void ParseStreamForListViews()
		{
			if (Dispatcher.CheckAccess() == false)
			{
				Dispatcher.Invoke(new Action(() => ParseStreamForListViews()));
				return;
			}

			StreamParser.ParseStreamIntoListView(CurrentNetworkStream, CurrentNetworkStream.ActorNameToSummary, ActorListView);
			StreamParser.ParseStreamIntoListView(CurrentNetworkStream, CurrentNetworkStream.PropertyNameToSummary, PropertyListView);
			StreamParser.ParseStreamIntoListView(CurrentNetworkStream, CurrentNetworkStream.RPCNameToSummary, RPCListView);
			StreamParser.ParseStreamIntoReplicationListView(CurrentNetworkStream, CurrentNetworkStream.ObjectNameToReplicationSummary, ObjectReplicationListView);

			ActorFilterBox.Items.Clear();
			ActorFilterBox.Items.Add("");

			PropertyFilterBox.Items.Clear();
			PropertyFilterBox.Items.Add("");

			RPCFilterBox.Items.Clear();
			RPCFilterBox.Items.Add("");

			foreach (var SummaryEntry in CurrentNetworkStream.ActorNameToSummary)
			{
				ActorFilterBox.Items.Add(CurrentNetworkStream.GetName(SummaryEntry.Key));
			}

			foreach (var SummaryEntry in CurrentNetworkStream.PropertyNameToSummary)
			{
				PropertyFilterBox.Items.Add(CurrentNetworkStream.GetName(SummaryEntry.Key));
			}

			foreach (var SummaryEntry in CurrentNetworkStream.RPCNameToSummary)
			{
				RPCFilterBox.Items.Add(CurrentNetworkStream.GetName(SummaryEntry.Key));
			}

			ConnectionListBox.Items.Clear();

			int NumberOfAddresses = (CurrentNetworkStream.GetVersion() < 12) ? CurrentNetworkStream.AddressArray.Count : CurrentNetworkStream.StringAddressArray.Count;
			for (int i = 0; i < NumberOfAddresses; i++)
			{
				CheckBox ConnectionCheckbox = new CheckBox();
				ConnectionCheckbox.Content = CurrentNetworkStream.GetIpString(i, CurrentNetworkStream.GetVersion());
				ConnectionCheckbox.IsChecked = true;
				ConnectionCheckbox.Checked += ConnectionListBoxItemChecked;
				ConnectionCheckbox.Unchecked += ConnectionListBoxItemUnChecked;
				ConnectionListBox.Items.Add(ConnectionCheckbox);
			}
		}

		private void ConnectionListBoxItemChecked(object sender, RoutedEventArgs e)
		{
			UpdateConnectionFilter();
		}

		private void ConnectionListBoxItemUnChecked(object sender, RoutedEventArgs e)
		{
			UpdateConnectionFilter();
		}

		private void OpenNetworkProfileBtn_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// Create a file open dialog for selecting the .nprof file.
			OpenFileDialog OpenDialog = new OpenFileDialog();
			OpenDialog.Title = "Open the profile data file from the game's 'Profiling' folder";
			OpenDialog.Filter = "Profiling Data (*.nprof)|*.nprof";
			OpenDialog.RestoreDirectory = false;			
			
			// Parse it if user didn't cancel.
			if (OpenDialog.ShowDialog() == true)
			{
				// Create binary reader and file info object from filename.
				ChangeNetworkStream(OpenDialog.FileName);
			}
		}

		public void SetCurrentStreamSelection(NetworkStream NetworkStream, PartialNetworkStream Selection, bool bSingleSelect)
		{
			if (Dispatcher.CheckAccess() == false)
			{
				Dispatcher.Invoke(new Action(() => SetCurrentStreamSelection(NetworkStream, Selection, bSingleSelect)));
				return;
			}

			ActorPerfPropsDetailsListView.Items.Clear();

			Selection.ToActorSummaryView(NetworkStream, ActorSummaryView);
			Selection.ToActorPerformanceView(NetworkStream, ActorPerfPropsListView, ActorPerfPropsDetailsListView, CurrentFilterValues);

			// Below is way too slow for range select right now, so we just do this for single frame selection
			if (bSingleSelect)
			{
				Selection.ToDetailedTreeView(TokenDetailsView.Items, CurrentFilterValues);
			}

			CurrentStreamSelection = Selection;
		}

		public int GetMaxProfileMinutes()
		{
			return MaxProfileMinutes;
		}

		public static void WriteToConsole(string Msg)
		{
			Console.WriteLine(Msg);
		}

		private void ReloadChartWorker()
		{
			ChartParser.ParseStreamIntoChart(this, CurrentNetworkStream, NetworkChart, CurrentFilterValues);
			CancelSelectRangeThread();
			SelectRangeThread = new Thread(() => SelectRangeWorker(0, CurrentNetworkStream.Frames.Count));
			SelectRangeThread.Start();
			LoadThread = null;
		}
		private void ResetFiltersButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			CurrentFilterValues.ActorFilter = "";
			CurrentFilterValues.PropertyFilter = "";
			CurrentFilterValues.RPCFilter = "";

			ActorFilterBox.Text = PropertyFilterBox.Text = RPCFilterBox.Text = null;
		}

		private void ApplyFiltersButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			CurrentFilterValues.ActorFilter = ActorFilterBox.Text != null ? ActorFilterBox.Text : "";
			CurrentFilterValues.PropertyFilter = PropertyFilterBox.Text != null ? PropertyFilterBox.Text : "";
			CurrentFilterValues.RPCFilter = RPCFilterBox.Text != null ? RPCFilterBox.Text : "";

			UpdateConnectionFilter();

			CancelLoadThread();

			LoadThread = new Thread(() => ReloadChartWorker());
			LoadThread.Start();
		}

		private void SelectRangeWorker(int SelectionStart, int SelectionEnd)
		{
			// Create a partial network stream with the new selection to get the summary.
			PartialNetworkStream Selection = new PartialNetworkStream(
													this,
													CurrentNetworkStream.Frames,
													SelectionStart,
													SelectionEnd,
													CurrentNetworkStream.NameIndexUnreal,
													CurrentFilterValues,
													1 / 30.0f
												);

			SetCurrentStreamSelection(CurrentNetworkStream, Selection, false);
			SelectRangeThread = null;
		}

		private void CancelSelectRangeThread()
		{
			if (SelectRangeThread != null)
			{
				SelectRangeThread.Abort();
				SelectRangeThread = null;
			}
		}

		private void UpdateConnectionFilter()
		{
			CurrentFilterValues.ConnectionMask = new HashSet<int>();

			for (int i = 0; i < ConnectionListBox.Items.Count; i++)
			{
				if (((CheckBox)ConnectionListBox.Items[i]).IsChecked == true)
				{
					CurrentFilterValues.ConnectionMask.Add(i);
				}
			}
		}

		private void PlotStyleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			NetworkChart.Plot.Style((ScottPlot.Style)PlotStyleSelector.SelectedIndex);
			NetworkChart.Render();
		}

		private void CheckAllConnectionsCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			CheckAllConnectionsCheckBoxToggle(true);
		}

		private void CheckAllConnectionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			CheckAllConnectionsCheckBoxToggle(false);
		}

		private void CheckAllConnectionsCheckBoxToggle(bool bCheck)
		{
			if (ConnectionListBox != null)
			{
				for (int i = 0; i < ConnectionListBox.Items.Count; i++)
				{
					((CheckBox)ConnectionListBox.Items[i]).IsChecked = bCheck;
				}
			}
		}

		private void ChartShowLegendCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			NetworkChart.Plot.Legend(true, ScottPlot.Alignment.UpperLeft);
			NetworkChart.Render();
		}

		private void ChartShowLegendCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			NetworkChart.Plot.Legend(false);
			NetworkChart.Render();
		}

		private void ActorPerfPropsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ActorPerfPropsDetailsListView.Items.Clear();

			if (LoadThread != null || SelectRangeThread != null || CurrentStreamSelection == null || ActorPerfPropsListView.SelectedItems.Count == 0)
			{
				return;
			}

			NetworkListViewItem networkListViewItem = (NetworkListViewItem)ActorPerfPropsListView.SelectedItems[0];
			CurrentStreamSelection.ToPropertyPerformanceView(CurrentNetworkStream, networkListViewItem.Header1, ActorPerfPropsDetailsListView, CurrentFilterValues);
		}

		private void MaxProfileMinutesTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (MaxProfileMinutesTextBox.Text == "")
			{
				MaxProfileMinutes = 0;
			}
			else try
			{
				MaxProfileMinutes = int.Parse(MaxProfileMinutesTextBox.Text);
			}
			catch
			{
				MaxProfileMinutes = 0;
				MaxProfileMinutesTextBox.Text = "";
			}
		}

		private void UnrealNetworkProfilerMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			GameAnalytics.EndSession();
		}
		private void AboutBtn_Click(object sender, RoutedEventArgs e)
		{
			GameAnalytics.AddDesignEvent("AboutDialogOpen");
			aboutDialog = Dialog.Show(new AboutDialog(this));
		}

		public void CloseAboutDialog()
		{
			GameAnalytics.AddDesignEvent("AboutDialogClose");
			aboutDialog.Close();
		}
	}

	public class FilterValues
	{
		public string ActorFilter = "";
		public string PropertyFilter = "";
		public string RPCFilter = "";

		public HashSet<int> ConnectionMask = null;
	}

	public class SeriesTypeValues
	{
		SeriesType Type;
		List<double> ValuesX = new List<double>();
		List<double> ValuesY = new List<double>();
		public bool bVisible = false;

		public SeriesTypeValues(SeriesType InType, double X, double Y, bool bInVisible)
		{
			Type = InType;
			ValuesX.Add(X);
			ValuesY.Add(Y);
			bVisible = bInVisible;
		}

		public void Add(double X, double Y)
		{
			ValuesX.Add(X);
			ValuesY.Add(Y);
		}

		public void Reset()
		{
			ValuesX.Clear();
			ValuesY.Clear();
		}

		public double[] GetArrayX()
		{
			if (ValuesX.Count == 0)
			{
				ValuesX.Add(0);
			}
			
			return ValuesX.ToArray();
		}

		public double[] GetArrayY()
		{
			if (ValuesY.Count == 0)
			{
				ValuesY.Add(0);
			}

			return ValuesY.ToArray();
		}

		public override string ToString()
		{
			return Type.ToString();
		}
	}
}
