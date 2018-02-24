using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;

namespace Imaging
{
    class KinectImageProducer : ImageProducer
    {
        private ConcurrentQueue<ImageSource> _queue;
        private KinectSensor _sensor;
        private FrameManager _frameManager;

        private KinectImageProducer()
        {
            _frameManager = FrameManager.GetInstance();
            _queue = new ConcurrentQueue<ImageSource>();
            _sensor = KinectSensor.GetDefault();

            _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared)
                .MultiSourceFrameArrived += OnMultiSourceFrameArrived;

            _sensor.Open();
        }

        private void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
        {
            _frameManager.ProcessMultiSourceFrameEvent(multiSourceFrameArrivedEventArgs);
            _queue.Enqueue(_frameManager.buildBitmapSourceFromFrame(SourceType.COLOR));
        }

        public static KinectImageProducer GetInstance()
        {
            return new KinectImageProducer();
        }

        public ConcurrentQueue<ImageSource> GetImageQueue()
        {
            return _queue;
        }

        public void SetConfiguration(ImageProducerConfiguration config)
        {
            // do nothing
        }

        #region Implementation Details

        private class Box
        {
            private int _width, _height;
            public int Width { get { return _width; } }
            public int Height { get { return _height; } }
            public int Area { get { return Width * Height; } }

            private Box(int width, int height)
            {
                _width = width;
                _height = height;
            }

            public static Box With(int width, int height)
            {
                return new Box(width, height);
            }
        }

        private class SourceType
        {
            public static SourceType COLOR = new SourceType(0);
            public static SourceType DEPTH = new SourceType(1);
            public static SourceType INFRARED = new SourceType(2);
            public static SourceType BODY_INDEX = new SourceType(3);

            //private FrameSourceTypes _type;
            //public FrameSourceTypes Type { get { return _type; } }
            //private SourceType(FrameSourceTypes type) { _type = type; }
            private SourceType(int value) { _value = value; }
            private int Value { get { return _value; } }
            private int _value;

            public static SourceType fromString(String value)
            {
                if (!String.IsNullOrEmpty(value) && !String.IsNullOrWhiteSpace(value))
                {
                    if (value.ToLower().Equals("color"))
                        return COLOR;
                    else if (value.ToLower().Equals("depth"))
                        return DEPTH;
                    else if (value.ToLower().Equals("infrared"))
                        return INFRARED;
                    else if (value.ToLower().Equals("bodyindex"))
                        return BODY_INDEX;
                }
                throw new InvalidOperationException("Error converting \"" + value + "\" to SourceType");
            }

            public override bool Equals(object obj)
            {
                return obj != null && (obj is SourceType) && (obj as SourceType).Value == Value;
            }

            public override int GetHashCode()
            {
                return Value;
            }
        }

        private class FrameManager
        {
            private static FrameManager instance;

            private FrameManager() { }

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
                        depthPixels = new byte[dimensions.Area * (39 / 8)];

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
        #endregion
    }
}
