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

using System;
using ScottPlot;

namespace NetworkProfiler
{
	class ChartParser
	{
		public static void ParseStreamIntoChart(MainWindow InMainWindow, NetworkStream InStream, WpfPlot NetworkChart, FilterValues InFilterValues)
		{
			var StartTime = DateTime.UtcNow;

			InMainWindow.ShowProgress(true);
			NetworkChart.Plot.Clear();

			for (int i = 0; i < InMainWindow.DefaultSeriesTypes.Count; i++)
			{
				float Percent = (float)i / (float)InMainWindow.DefaultSeriesTypes.Count;
				InMainWindow.UpdateProgress((int)(Percent * 100));
				InMainWindow.DefaultSeriesTypes[i].Reset();
			}

			int FrameCounter = 0;
			foreach (PartialNetworkStream RawFrame in InStream.Frames)
			{
				if (FrameCounter % 1000 == 0)
				{
					float Percent = (float)FrameCounter / (float)InStream.Frames.Count;
					InMainWindow.UpdateProgress((int)(Percent * 100));
				}

				PartialNetworkStream Frame = RawFrame.Filter(InFilterValues);
				if (Frame.EndTime == Frame.StartTime)
				{
					throw new InvalidOperationException("End time and Start time cannot be same.");
				}

				float OneOverDeltaTime = 1 / (Frame.EndTime - Frame.StartTime);
				int OutgoingBandwidth = Frame.UnrealSocketSize + Frame.OtherSocketSize + NetworkStream.PacketOverhead * (Frame.UnrealSocketCount + Frame.OtherSocketCount);

				InMainWindow.AddChartPoint(SeriesType.OutgoingBandwidthSize, FrameCounter, OutgoingBandwidth);
				InMainWindow.AddChartPoint(SeriesType.OutgoingBandwidthSizeSec, FrameCounter, OutgoingBandwidth * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.ActorCount, FrameCounter, Frame.ActorCount);
				InMainWindow.AddChartPoint(SeriesType.PropertySize, FrameCounter, Frame.ReplicatedSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.PropertySizeSec, FrameCounter, Frame.ReplicatedSizeBits / 8 * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.RPCSize, FrameCounter, Frame.RPCSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.RPCSizeSec, FrameCounter, Frame.RPCSizeBits / 8 * OneOverDeltaTime);

#if true
				InMainWindow.AddChartPoint(SeriesType.ActorCountSec, FrameCounter, Frame.ActorCount * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.PropertyCount, FrameCounter, Frame.PropertyCount);
				InMainWindow.AddChartPoint(SeriesType.PropertyCountSec, FrameCounter, Frame.PropertyCount * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.RPCCount, FrameCounter, Frame.RPCCount);
				InMainWindow.AddChartPoint(SeriesType.RPCCountSec, FrameCounter, Frame.RPCCount * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.ExportBunchCount, FrameCounter, Frame.ExportBunchCount);
				InMainWindow.AddChartPoint(SeriesType.ExportBunchSize, FrameCounter, Frame.ExportBunchSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.MustBeMappedGuidsCount, FrameCounter, Frame.MustBeMappedGuidCount / 8);
				InMainWindow.AddChartPoint(SeriesType.MustBeMappedGuidsSize, FrameCounter, Frame.MustBeMappedGuidSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.SendAckCount, FrameCounter, Frame.SendAckCount);
				InMainWindow.AddChartPoint(SeriesType.SendAckCountSec, FrameCounter, Frame.SendAckCount * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.SendAckSize, FrameCounter, Frame.SendAckSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.SendAckSizeSec, FrameCounter, Frame.SendAckSizeBits / 8 * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.ContentBlockHeaderSize, FrameCounter, Frame.ContentBlockHeaderSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.ContentBlockFooterSize, FrameCounter, Frame.ContentBlockFooterSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.PropertyHandleSize, FrameCounter, Frame.PropertyHandleSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.SendBunchCount, FrameCounter, Frame.SendBunchCount);
				InMainWindow.AddChartPoint(SeriesType.SendBunchCountSec, FrameCounter, Frame.SendBunchCount * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.SendBunchSize, FrameCounter, Frame.SendBunchSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.SendBunchSizeSec, FrameCounter, Frame.SendBunchSizeBits / 8 * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.SendBunchHeaderSize, FrameCounter, Frame.SendBunchHeaderSizeBits / 8);
				InMainWindow.AddChartPoint(SeriesType.GameSocketSendSize, FrameCounter, Frame.UnrealSocketSize);
				InMainWindow.AddChartPoint(SeriesType.GameSocketSendSizeSec, FrameCounter, Frame.UnrealSocketSize * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.GameSocketSendCount, FrameCounter, Frame.UnrealSocketCount);
				InMainWindow.AddChartPoint(SeriesType.GameSocketSendCountSec, FrameCounter, Frame.UnrealSocketCount * OneOverDeltaTime);
				InMainWindow.AddChartPoint(SeriesType.ActorReplicateTimeInMS, FrameCounter, Frame.ActorReplicateTimeInMS);
#endif

#if false
				InMainWindow.AddChartPoint( SeriesType.MiscSocketSendSize,			FrameCounter, Frame.OtherSocketSize );
				InMainWindow.AddChartPoint( SeriesType.MiscSocketSendSizeSec,		FrameCounter, Frame.OtherSocketSize * OneOverDeltaTime );
				InMainWindow.AddChartPoint( SeriesType.MiscSocketSendCount,			FrameCounter, Frame.OtherSocketCount );
				InMainWindow.AddChartPoint( SeriesType.MiscSocketSendCountSec,		FrameCounter, Frame.OtherSocketCount * OneOverDeltaTime );								
#endif

				if (Frame.NumEvents > 0)
				{
					InMainWindow.AddChartPoint(SeriesType.Events, FrameCounter, 0);
				}

				FrameCounter++;
			}

			InMainWindow.ShowProgress(false);
			Console.WriteLine("Adding data to chart took {0} seconds", (DateTime.UtcNow - StartTime).TotalSeconds);
			InMainWindow.Dispatcher.Invoke(new Action(() => InMainWindow.UpdateNetworkChart()));
		}
	}
}
