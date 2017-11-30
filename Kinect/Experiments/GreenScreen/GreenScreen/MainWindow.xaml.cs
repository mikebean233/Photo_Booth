using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace GreenScreen
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private SourceType selectedSource;
		private KinectSensor sensor;
		private FrameManager frameManager;

		public MainWindow()
		{
			InitializeComponent();
		}
		
		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			frameManager = FrameManager.GetInstance();
			sensor = KinectSensor.GetDefault();
			selectedSource = SourceType.COLOR;
			sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared)
				.MultiSourceFrameArrived += OnMultiSourceFrameArrived;
				sensor.Open(); 
		}
		
		private void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
		{
			frameManager.ProcessMultiSourceFrameEvent(multiSourceFrameArrivedEventArgs);
			MyImage.Source = frameManager.buildBitmapSourceFromFrame(selectedSource);
		}

		private void Exit_OnClick(object sender, RoutedEventArgs e)
		{
			shutdown();
		}

		private void shutdown()
		{
			sensor?.Close();
			this.Close();
		}

		private void SourceRadioGroup_OnChecked(object sender, RoutedEventArgs e)
		{
			//Enum.TryParse((String) (sender as RadioButton).Content, out selectedSource);
			RadioButton radioButton = sender as RadioButton;
			String tagString = radioButton.Tag.ToString();
			selectedSource = SourceType.fromString(tagString);
		}
	}
}
 