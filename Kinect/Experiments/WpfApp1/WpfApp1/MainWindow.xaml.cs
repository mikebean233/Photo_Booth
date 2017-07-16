using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Security.Permissions;
using System.Windows.Interop;

namespace WpfApp1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window

	{
		private KinectSensor sensor;
		
		public MainWindow()
		{
			sensor = KinectSensor.GetDefault();
			if (sensor != null)
			{
				MultiSourceFrameReader reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared);
				reader.MultiSourceFrameArrived += ReaderOnMultiSourceFrameArrived;
				sensor.Open();
				InitializeComponent();
			}
		}

		private void ReaderOnMultiSourceFrameArrived(object sender,
			MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
		{
			if (multiSourceFrameArrivedEventArgs == null)
				return;

			ColorFrame colorFrame = multiSourceFrameArrivedEventArgs.FrameReference.AcquireFrame().ColorFrameReference
				.AcquireFrame();
			InfraredFrame infraredFrame = multiSourceFrameArrivedEventArgs.FrameReference.AcquireFrame().InfraredFrameReference
				.AcquireFrame();
			DepthFrame depthFrame = multiSourceFrameArrivedEventArgs.FrameReference.AcquireFrame().DepthFrameReference
				.AcquireFrame();
			
			// Color
			if (colorFrame != null)
			{
				colorPreview.Source = GetBitmapSourceFromFrame(colorFrame);
				colorFrame.Dispose();
			}
			// Infrared
			if (infraredFrame != null)
			{
				infraredPreview.Source = GetBitmapSourceFromFrame(infraredFrame);
				infraredFrame.Dispose();
			}
			
			// Depth
			if (depthFrame != null)
			{
				depthPreview.Source = GetBitmapSourceFromFrame(depthFrame);
				depthFrame.Dispose();
			}
		}

		private ImageSource GetBitmapSourceFromFrame(IDisposable frame)
		{
			KinectFrame kFrame = new KinectFrame(frame);

			int width = kFrame.FrameDescription().Width;
			int height = kFrame.FrameDescription().Height;
			ushort[] shortPixels = new ushort[width * height];
			byte[] bytePixels = kFrame.getPixelArray();

			if (frame is ColorFrame)
			{
				kFrame.CopyRawFrameToArray(bytePixels);
			}
			else
			{
				kFrame.CopyFrameDataToArray(shortPixels);
				int outIndex = 0;
				for (int i = 0; i < shortPixels.Length; ++i)
				{
					bytePixels[outIndex++] = (byte) (shortPixels[i] & 0x00ff);
					bytePixels[outIndex++] = (byte) ((shortPixels[i] & 0xff00) >> 8);
				}
			}

			return BitmapSource.Create(width, height, 96, 96, kFrame.PixelFormat() , null, bytePixels, kFrame.Stride());
		}

		class KinectFrame
		{
			private readonly IDisposable _thisFrame;
			private readonly Collection<Type> _supportedFrames = new Collection<Type>(
				new Type[]
				{
					typeof(ColorFrame),
					typeof(InfraredFrame),
					typeof(DepthFrame)
				}
			);

			public KinectFrame(IDisposable frame)
			{
				if(frame == null)
					throw new NullReferenceException();

				if(!_supportedFrames.Contains(frame.GetType()))
					throw new NotSupportedException(frame.GetType() + " is not a supported frame type");

				_thisFrame = frame;
			}

			public FrameDescription FrameDescription()
			{
				if(_thisFrame is ColorFrame)
					return (_thisFrame as ColorFrame).FrameDescription;
				else if (_thisFrame is DepthFrame)
					return (_thisFrame as DepthFrame).FrameDescription;
				else
					return (_thisFrame as InfraredFrame).FrameDescription;
			}

			public ColorImageFormat RawColorImageFormat()
			{
				if (!(_thisFrame is ColorFrame))
					throw new NotSupportedException("RawColorImageFormat() is not supported for frame of type " + _thisFrame.GetType());

				return ((ColorFrame) _thisFrame).RawColorImageFormat;
			}

			public void CopyRawFrameToArray(byte[] pixels)
			{
				if (!(_thisFrame is ColorFrame))
					throw new NotSupportedException("CopyRawFrameToArray() is not supported for frame of type " + _thisFrame.GetType());

				ColorFrame frame = _thisFrame as ColorFrame;

				if(frame.RawColorImageFormat == ColorImageFormat.Bgra)
					frame.CopyRawFrameDataToArray(pixels);
				else
					frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
			}

			public void CopyConvertedFrameDataToArray(byte[] pixels, ColorImageFormat colorImageFormat)
			{
				if (!(_thisFrame is ColorFrame))
					throw new NotSupportedException("CopyConvertedFrameDataToArray() is not supported for frame of type " + _thisFrame.GetType());

				((ColorFrame)_thisFrame).CopyConvertedFrameDataToArray(pixels, colorImageFormat);
			}

			public void Dispose()
			{
				_thisFrame.Dispose();
			}

			public void CopyFrameDataToArray(ushort[] pixels)
			{
				if(_thisFrame is ColorFrame )
					throw new NotSupportedException("CopyFrameDataToArray() is not supported for frame of type " + _thisFrame.GetType());
				else if(_thisFrame is DepthFrame)
					(_thisFrame as DepthFrame).CopyFrameDataToArray(pixels);
				else 
					(_thisFrame as InfraredFrame).CopyFrameDataToArray(pixels);
			}

			public PixelFormat PixelFormat()
			{
				return (_thisFrame is ColorFrame) ? PixelFormats.Bgr32 : PixelFormats.Gray16  ;
			}

			public int Stride()
			{
				return FrameDescription().Width * ((_thisFrame is ColorFrame) ? PixelFormat().BitsPerPixel / 8 : 2);
			}

			public byte[] getPixelArray()
			{
				int width = FrameDescription().Width;
				int height = FrameDescription().Height;

				if (_thisFrame is ColorFrame)
					return new byte[width * height * ((PixelFormat().BitsPerPixel + 7) / 8)];
				else
					return new byte[width * height * 2];
			}

		}

		private void InfraredSave_Click(object sender, RoutedEventArgs e)
		{
			if (infraredPreview.Source != null)
			{
				SaveBmpFromImageSource(infraredPreview.Source, "Infrared.tiff");
			}
		}

		private void DepthSave_OnClick(object sender, RoutedEventArgs e)
		{
			if (depthPreview.Source != null)
			{
				SaveBmpFromImageSource(depthPreview.Source, "Depth.tiff");
			}
		}

		private void ColorSave_OnClick(object sender, RoutedEventArgs e)
		{
			if (colorPreview.Source != null)
			{
				SaveBmpFromImageSource(colorPreview.Source, "Color.tiff");
			}
		}

		private void SaveBmpFromImageSource(ImageSource source, String filename)
		{
			Console.WriteLine(source);
			Image outImage = new Image();
			outImage.Width = source.Width;
			outImage.Height = source.Height;
			outImage.Source = source;

			FileStream stream = new FileStream(filename, FileMode.Create);
			TiffBitmapEncoder encoder = new TiffBitmapEncoder();
			TextBlock myTextBlock = new TextBlock();
			myTextBlock.Text = "Codec Author is: " + encoder.CodecInfo.Author.ToString();
			encoder.Frames.Add(BitmapFrame.Create((BitmapSource)source));
			encoder.Save(stream);
		}

		private void Close_Click(object sender, RoutedEventArgs e)
		{
			sensor.Close();
			Application.Current.Shutdown();
		}

	}

}
