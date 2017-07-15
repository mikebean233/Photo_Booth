using System;
using System.Collections.Generic;
using System.IO;
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
using System.Drawing;
using System.Windows.Interop;

namespace WpfApp1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window

	{
		private KinectSensor sensor;
		private BitmapSource infraredSource;
		private BitmapSource depthSource;
		private BitmapSource visibleSource;

		public MainWindow()
		{
			sensor = KinectSensor.GetDefault();
			if (sensor != null)
			{
				MultiSourceFrameReader reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared);
				reader.MultiSourceFrameArrived += ReaderOnMultiSourceFrameArrived;

				//sensor.InfraredFrameSource.OpenReader().FrameArrived += OnInfraredArrived;
				//sensor.DepthFrameSource.OpenReader().FrameArrived += onDepthFrameArrived;
				//sensor.ColorFrameSource.OpenReader().FrameArrived += OnVisibleFrameArrived;
				//sensor.Open();



				InitializeComponent();
			}
		}

		private void ReaderOnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
		{
				
		}

		private void OnVisibleFrameArrived(object sender, ColorFrameArrivedEventArgs colorFrameArrivedEventArgs)
		{
			Byte[] array = null;

			ColorFrame frame = colorFrameArrivedEventArgs.FrameReference.AcquireFrame();
			if (frame != null)
			{
				int width = frame.FrameDescription.Width;
				int height = frame.FrameDescription.Height;
				PixelFormat format = PixelFormats.Bgr32;

				byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

				if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
				{
					frame.CopyRawFrameDataToArray(pixels);
				}
				else
				{
					frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
				}

				int stride = width * format.BitsPerPixel / 8;

				visibleSource = BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
				visiblePreview.Source = visibleSource;
				frame.Dispose();

			}

		}


		private void onDepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
		{

			ushort[] array = null;

			DepthFrame frame = e.FrameReference.AcquireFrame();
			if (frame != null)
			{
				FrameDescription frameDescription = frame.FrameDescription;

				if (frameDescription != null)
				{
					array = new ushort[frameDescription.Width * frameDescription.Height];
					frame.CopyFrameDataToArray(array);

					// Copy the damn thing
					byte[] arrayCopy = new byte[array.Length * 2];
					int outIndex = 0;
					for (int i = 0; i < array.Length; ++i)
					{
						arrayCopy[outIndex++] = (byte)(array[i] & 0x00ff);
						arrayCopy[outIndex++] = (byte)((array[i] & 0xff00) >> 8);
					}

					int width = frameDescription.Width;
					int height = frameDescription.Height;

					BitmapPalette myPalette = BitmapPalettes.Gray16; //;new BitmapPalette(colors);

					depthSource = BitmapSource.Create(
						width,
						height,
						96,
						96,
						PixelFormats.Gray16,
						myPalette,
						arrayCopy,
						width * 2);
					depthPreview.Source = depthSource;
				}
				frame.Dispose();
			}

		}

		private void OnInfraredArrived(object sender, InfraredFrameArrivedEventArgs infraredFrameArrivedEventArgs)
		{
			ushort[] array = null;

			InfraredFrame frame = infraredFrameArrivedEventArgs.FrameReference.AcquireFrame();
			if (frame != null)
			{
				FrameDescription frameDescription = frame.FrameDescription;

				if (frameDescription != null)
				{
					array = new ushort[frameDescription.Width * frameDescription.Height];
					frame.CopyFrameDataToArray(array);

					// Copy the damn thing
					byte[] arrayCopy = new byte[array.Length * 2];
					int outIndex = 0;
					for (int i = 0; i < array.Length; ++i)
					{
						arrayCopy[outIndex++] = (byte)(array[i] & 0x00ff);
						arrayCopy[outIndex++] = (byte)((array[i] & 0xff00) >> 8);
					}

					int width = frameDescription.Width;
					int height = frameDescription.Height;

					BitmapPalette myPalette = BitmapPalettes.Gray16; //;new BitmapPalette(colors);

					infraredSource = BitmapSource.Create(
						width,
						height,
						96,
						96,
						PixelFormats.Gray16,
						myPalette,
						arrayCopy,
						width * 2);
					colorPreview.Source = infraredSource;
				}
				frame.Dispose();
			}
		}

		private void InfraredSave_Click(object sender, RoutedEventArgs e)
		{
			if (colorPreview.Source != null)
			{
				SaveBmpFromImageSource(colorPreview.Source, "Infrared.bmp");
			}
		}

		private void SaveBmpFromImageSource(ImageSource source, String filename)
		{
			Console.WriteLine(infraredSource);
			Image outImage = new Image();
			outImage.Width = infraredSource.Width;
			outImage.Height = infraredSource.Height;
			outImage.Source = infraredSource;

			FileStream stream = new FileStream(filename, FileMode.Create);
			BmpBitmapEncoder encoder = new BmpBitmapEncoder();
			TextBlock myTextBlock = new TextBlock();
			myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
			encoder.Frames.Add(BitmapFrame.Create(infraredSource));
			encoder.Save(stream);
		}

		private void DepthSave_OnClick(object sender, RoutedEventArgs e)
		{
			if (depthPreview.Source != null)
			{
				SaveBmpFromImageSource(depthPreview.Source, "Depth.bmp");
			}
		}

		private void ColorSave_OnClick(object sender, RoutedEventArgs e)
		{
			if (colorPreview.Source != null)
			{
				SaveBmpFromImageSource(colorPreview.Source, "Infrared.bmp");
			}
		}
		private void Close_Click(object sender, RoutedEventArgs e)
		{
			sensor.Close();
			Application.Current.Shutdown();
		}

	}

}
