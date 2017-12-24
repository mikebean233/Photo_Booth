using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace GreenScreen
{
	class FrameManager
	{
		private static FrameManager instance;
		
		private FrameManager(){}

		public static FrameManager GetInstance()
		{
			if (instance == null)
				instance = new FrameManager();
			return instance;
		}

		//private Object colorPixelsLock = new System.Object();
		//private Object depthPixelsLock = new Object();
		//private Object infraredPixelsLock = new Object();

		private Dictionary<SourceType, Box> frameResolutions = new Dictionary<SourceType, Box>();
		private Dictionary<SourceType, PixelFormat> framePixelFormats = new Dictionary<SourceType, PixelFormat>();
		
		//private ImmutableList<FrameSourceTypes> compatableFrameSourceTypes = ImmutableList.Create<FrameSourceTypes>()
		//	.Add(FrameSourceTypes.Color)
		//	.Add(FrameSourceTypes.Depth)
		//	.Add(FrameSourceTypes.Infrared);
			
		private byte[] colorPixels { get; set; }
		private byte[] depthPixels { get; set; }
		private UInt16[] infraredPixels { get; set; }
		//private IBufferByteAccess bodIndexByteAccess;

		private int min, max;

		public Boolean HaveAllFrames() { return colorPixels != null && depthPixels != null && infraredPixels != null; }

		public BitmapSource buildBitmapSourceFromFrame(SourceType frameSourceType)
		{
			if (frameResolutions.ContainsKey(frameSourceType))
			{
				Box dimensions = frameResolutions[frameSourceType];
				PixelFormat format = framePixelFormats[frameSourceType];

				byte[] outPixels;
				if (frameSourceType == SourceType.COLOR)
				{
					outPixels = colorPixels;
				}
				else if (frameSourceType == SourceType.DEPTH)
				{
					outPixels = depthPixels;
				}
				else
				{
					outPixels = new byte[dimensions.Area * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];
					int colorIndex = 0;
					int minDepth = frameSourceType == SourceType.DEPTH ? min : 0;
					int maxDepth = frameSourceType == SourceType.DEPTH ? max : 0;
					UInt16[] thisPixelArray = infraredPixels;// frameSourceType == SourceType.DEPTH ? depthPixels : infraredPixels;

					for (int index = 0; index < thisPixelArray.Length; ++index)
					{
						UInt16 pixel = thisPixelArray[index];
						byte intensity = (byte)((frameSourceType == SourceType.INFRARED) ? (pixel >> 8) : (pixel >= minDepth && pixel <= maxDepth) ? pixel : 0);

						outPixels[colorIndex++] = intensity;
						outPixels[colorIndex++] = intensity;
					}
				}
				return BitmapSource.Create(dimensions.Width, dimensions.Height, 96, 96, format, null, outPixels, dimensions.Width * format.BitsPerPixel / 8);
			}
			else
			{
				//throw new Exception("There is no frame available for source type " + frameSourceType);
				return null;
			}
		}

		private void processColorFrame(ColorFrame colorFrame)
		{
			if (colorFrame != null)
			{
				Box dimensions = Box.With(colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height);
				frameResolutions[SourceType.COLOR] = dimensions;
				framePixelFormats[SourceType.COLOR] = PixelFormats.Bgr32;
				
				if (colorPixels == null)
					colorPixels = new byte[dimensions.Area * 4];

				if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
					colorFrame.CopyRawFrameDataToArray(colorPixels);
				else
					colorFrame.CopyConvertedFrameDataToArray(colorPixels, ColorImageFormat.Bgra);
				colorFrame?.Dispose();
			}
		}

		private void processDepthFrame(DepthFrame depthFrame)
		{
			if (depthFrame != null)
			{
				Box dimensions = Box.With(depthFrame.FrameDescription.Width, depthFrame.FrameDescription.Height);
				frameResolutions[SourceType.DEPTH] = Box.With(depthFrame.FrameDescription.Width, depthFrame.FrameDescription.Height);
				framePixelFormats[SourceType.DEPTH] = PixelFormats.Gray16;

				min = depthFrame.DepthMinReliableDistance;
				max = depthFrame.DepthMaxReliableDistance;

				if (depthPixels == null)
					depthPixels = new byte[dimensions.Area * (39/8)];

				UInt16[] tempPixels = new UInt16[dimensions.Area];

				depthFrame.CopyFrameDataToArray(tempPixels);


				int colorIndex = 0;
				for (int index = 0; index < tempPixels.Length; ++index)
				{
					UInt16 pixel = tempPixels[index];
					byte intensity = (byte)(pixel);/// ((pixel - min) % (max - min));//(byte)((pixel >= minDepth && pixel <= maxDepth) ? pixel : 0);

					//depthPixels[colorIndex++] = intensity;
					colorIndex++;
					depthPixels[colorIndex++] = intensity;
				}

				depthFrame?.Dispose();
			}
		}

		private void processInfraredFrame(InfraredFrame infraredFrame)
		{
			if (infraredFrame != null)
			{
				Box dimensions = Box.With(infraredFrame.FrameDescription.Width, infraredFrame.FrameDescription.Height);
				frameResolutions[SourceType.INFRARED] = dimensions;
				framePixelFormats[SourceType.INFRARED] = PixelFormats.Gray16;

				if (infraredPixels == null)
					infraredPixels = new UInt16[dimensions.Area];

				infraredFrame.CopyFrameDataToArray(infraredPixels);
				infraredFrame?.Dispose();
			}
		}

		private void processBodyIndexFrame(BodyIndexFrame bodyIndexFrame)
		{
			if (bodyIndexFrame != null)
			{
				Box dimensions = Box.With(bodyIndexFrame.FrameDescription.Width, bodyIndexFrame.FrameDescription.Height);
				//frameResolutions[SourceType.] = dimensions;
				framePixelFormats[SourceType.INFRARED] = PixelFormats.Gray16;

				

			}
		}

		public Boolean ProcessMultiSourceFrameEvent(MultiSourceFrameArrivedEventArgs eventArgs)
		{
			processColorFrame(eventArgs.FrameReference.AcquireFrame().ColorFrameReference.AcquireFrame());
			processDepthFrame(eventArgs.FrameReference.AcquireFrame().DepthFrameReference.AcquireFrame());
			processInfraredFrame(eventArgs.FrameReference.AcquireFrame().InfraredFrameReference.AcquireFrame());
			//processBodyIndexFrame(eventArgs.FrameReference.AcquireFrame().BodyIndexFrameReference.AcquireFrame());

			return HaveAllFrames();
		}
	}
}
