using NetworkProfiler;
using System.Windows;
using System.Windows.Controls;

namespace UE4_Network_Profiler.UserControls
{
	public partial class AboutDialog : UserControl
	{
		private MainWindow _mainWindow;

		public AboutDialog(MainWindow InMainWindow)
		{
			InitializeComponent();
			_mainWindow = InMainWindow;
		}

		private void CloseBtn_Click(object sender, RoutedEventArgs e)
		{
			_mainWindow.CloseAboutDialog();
		}
	}
}
