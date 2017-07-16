using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace GreenScreen
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private KinectSensor sensor;
		private CoordinateMapper coordinateMapper;

		public MainWindow()
		{
			InitializeComponent();
		}


		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			int width = 100;
			int height = 100;
			byte[] pixels = new byte[width * height * 3];
			int outIndex = 0;
			for (int i = 0; i < width * height; ++i)
			{
				pixels[outIndex++] = 0xFF;
				pixels[outIndex++] = 0x00;
				pixels[outIndex++] = 0x00;
			}

			MyImage.Source = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgr24, null, pixels, width * 3);

			sensor = KinectSensor.GetDefault();
			if (sensor == null || !sensor.IsAvailable)
			{
				System.Windows.MessageBox.Show(this, "There was a problem Communicating with the Kinect Sensor",
					"Can't connect to sensor", MessageBoxButton.OK);
				shutdown();
			}
			else
			{
				coordinateMapper = sensor.CoordinateMapper;
				sensor.
			}
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
	}
}
