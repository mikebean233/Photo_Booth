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
            _frameManager = FrameManager.Instance;
            _queue = new ConcurrentQueue<ImageSource>();
            _sensor = KinectSensor.GetDefault();

            _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.BodyIndex)
                .MultiSourceFrameArrived += OnMultiSourceFrameArrived;

            _sensor.Open();
        }

        private void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
        {
            _frameManager.ProcessMultiSourceFrameEvent(multiSourceFrameArrivedEventArgs);
            _queue.Enqueue(_frameManager.BuildBitmapSourceFromFrame(SourceType.BODY_INDEX));
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

            public static Box FromFrameDescription(FrameDescription frameDescription)
            {
                return new Box(frameDescription.Width, frameDescription.Height);
            }

            public static Box With(int width, int height)
            {
                return new Box(width, height);
            }
        }

        private class SourceType
        {
            private readonly int _id;
            public int Id { get { return _id; } }

            private readonly String _name;
            public String Name { get { return _name; } }

            private static readonly HashSet<SourceType> _values = new HashSet<SourceType>();
            public static ISet<SourceType> Values { get { return _values; }}

            public static readonly SourceType COLOR      = new SourceType(0, "color");
            public static readonly SourceType DEPTH      = new SourceType(1, "depth");
            public static readonly SourceType INFRARED   = new SourceType(2, "infrared");
            public static readonly SourceType BODY_INDEX = new SourceType(3, "bodyindex");

            private SourceType(int id, String name)
            {
                _id = id;
                _name = name;
                _values.Add(this);
            }
            
            public static SourceType FromString(String name)
            {
                foreach (SourceType thisSourceType in _values)
                    if (thisSourceType.Name.Equals(name))
                        return thisSourceType;

                throw new InvalidOperationException(String.Format("Error converting \"{0}\" to SourceType", name));
            }

            public override bool Equals(object obj)
            {
                return obj != null && (obj is SourceType) && (obj as SourceType).Id == _id;
            }

            public override int GetHashCode()
            {
                return _id;
            }
        }

        private class FrameManager
        {
            private static readonly FrameManager _instance;
            private static readonly Dictionary<SourceType, Box> _frameResolutions = new Dictionary<SourceType, Box>();
            private static readonly Dictionary<SourceType, PixelFormat> _framePixelFormats = new Dictionary<SourceType, PixelFormat>();
            private static readonly Dictionary<int, int> _bodyIndexToColorMap = new Dictionary<int, int>();
            private static readonly Dictionary<int, int> _colorToBodyIndexMap = new Dictionary<int, int>();
            private static byte[] _colorPixels;
            private static byte[] _depthPixels;
            private static byte[] _infraredPixels;
            private static byte[] _bodyIndexPixels;
            
            private bool HaveAllFrames() { return _colorPixels != null && _depthPixels != null && _infraredPixels != null; }

            static FrameManager()
            {
                _instance = new FrameManager();

                _framePixelFormats[SourceType.COLOR] = PixelFormats.Bgr32;
                _framePixelFormats[SourceType.DEPTH] = PixelFormats.Gray16;
                _framePixelFormats[SourceType.INFRARED] = PixelFormats.Gray16;
                _framePixelFormats[SourceType.BODY_INDEX] = PixelFormats.Rgb24;

                _bodyIndexToColorMap.Add(0, 0xFF0000); // RED
                _bodyIndexToColorMap.Add(1, 0x00FF00); // GREEN
                _bodyIndexToColorMap.Add(2, 0x0000FF); // BLUE
                _bodyIndexToColorMap.Add(3, 0xFFFF00); // YELLOW
                _bodyIndexToColorMap.Add(4, 0xFF00FF); // PURPLE
                _bodyIndexToColorMap.Add(5, 0xFFFFFF); // WHITE

                foreach (int bodyIndex in _bodyIndexToColorMap.Keys)
                    _colorToBodyIndexMap.Add(_bodyIndexToColorMap[bodyIndex], bodyIndex);
            }
            private FrameManager() { }

            public static FrameManager Instance { get { return _instance; } }
        
            public BitmapSource BuildBitmapSourceFromFrame(SourceType frameSourceType)
            {
                if (_frameResolutions.ContainsKey(frameSourceType))
                {
                    Box dimensions = _frameResolutions[frameSourceType];
                    PixelFormat format = _framePixelFormats[frameSourceType];

                    byte[] outPixels = null;
                    if (frameSourceType == SourceType.COLOR)
                    {
                        outPixels = _colorPixels;
                    }
                    else if (frameSourceType == SourceType.DEPTH)
                    {
                        outPixels = _depthPixels;
                    }
                    else if (frameSourceType == SourceType.INFRARED)
                    {
                        outPixels = _infraredPixels;
                    }
                    else if (frameSourceType == SourceType.BODY_INDEX)
                    {
                        outPixels = _bodyIndexPixels;
                    }
                    if(outPixels != null)
                        return BitmapSource.Create(dimensions.Width, dimensions.Height, 96, 96, format, null, outPixels, dimensions.Width * format.BitsPerPixel / 8);
                }
                return null;
            }

            private void ProcessColorFrame(ColorFrame colorFrame)
            {
                if (colorFrame != null)
                {
                    if (!_frameResolutions.ContainsKey(SourceType.COLOR))
                    {
                        _frameResolutions[SourceType.COLOR] = Box.FromFrameDescription(colorFrame.FrameDescription);
                        _colorPixels = new byte[_frameResolutions[SourceType.COLOR].Area * 4];
                    }
                    
                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                        colorFrame.CopyRawFrameDataToArray(_colorPixels);
                    else
                        colorFrame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);
                    colorFrame?.Dispose();
                }
            }

            private void ProcessDepthFrame(DepthFrame depthFrame)
            {
                if (depthFrame != null)
                {
                    if (!_frameResolutions.ContainsKey(SourceType.DEPTH))
                    {
                        _frameResolutions[SourceType.DEPTH] = Box.FromFrameDescription(depthFrame.FrameDescription);
                        _depthPixels = new byte[_frameResolutions[SourceType.DEPTH].Area * (39 / 8)];
                    }
                    
                    UInt16[] tempPixels = new UInt16[_frameResolutions[SourceType.DEPTH].Area];

                    depthFrame.CopyFrameDataToArray(tempPixels);
                    
                    int colorIndex = 0;
                    for (int index = 0; index < tempPixels.Length; ++index)
                    {
                        UInt16 pixel = tempPixels[index];
                        byte intensity = (byte)(pixel);

                        colorIndex++;
                        _depthPixels[colorIndex++] = intensity;
                    }

                    depthFrame?.Dispose();
                }
            }

            private void ProcessInfraredFrame(InfraredFrame infraredFrame)
            {
                if (infraredFrame != null)
                {

                    if (!_frameResolutions.ContainsKey(SourceType.INFRARED))
                    {
                        _frameResolutions[SourceType.INFRARED] = Box.FromFrameDescription(infraredFrame.FrameDescription);
                        _infraredPixels = new byte[_frameResolutions[SourceType.INFRARED].Area * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];
                    }
                    UInt16[] rawInfraredPixels = new UInt16[_frameResolutions[SourceType.INFRARED].Area];

                    infraredFrame.CopyFrameDataToArray(rawInfraredPixels);
                    infraredFrame?.Dispose();
                    
                    int colorIndex = 0;
                    for (int index = 0; index < rawInfraredPixels.Length; ++index)
                    {
                        UInt16 pixel = rawInfraredPixels[index];
                        byte intensity = (byte)(pixel >> 8);

                        _infraredPixels[colorIndex++] = intensity;
                        _infraredPixels[colorIndex++] = intensity;
                    }

                }
            }

            private void ProcessBodyIndexFrame(BodyIndexFrame bodyIndexFrame)
            {
                if (bodyIndexFrame != null)
                {
                    if (!_frameResolutions.ContainsKey(SourceType.BODY_INDEX))
                    {
                        _frameResolutions[SourceType.BODY_INDEX] = Box.FromFrameDescription(bodyIndexFrame.FrameDescription);
                        _bodyIndexPixels = new byte[_frameResolutions[SourceType.BODY_INDEX].Area * _framePixelFormats[SourceType.BODY_INDEX].BitsPerPixel * 3];
                    }

                    byte[] rawBodyIndexPixels = new byte[_frameResolutions[SourceType.BODY_INDEX].Area];
                    bodyIndexFrame.CopyFrameDataToArray(rawBodyIndexPixels);
                    bodyIndexFrame.Dispose();

                    int outIndex = 0;
                    for (int inIndex = 0; inIndex < rawBodyIndexPixels.Length; ++inIndex)
                    {
                        int bodyIndex = rawBodyIndexPixels[inIndex];
                        int thisColor = 0;
                        _bodyIndexToColorMap.TryGetValue(bodyIndex, out thisColor);
                        _bodyIndexPixels[outIndex++] = (byte)((thisColor & 0xFF0000) >> 16); // Red channel
                        _bodyIndexPixels[outIndex++] = (byte)((thisColor & 0x00FF00) >> 8 ); // Green channel
                        _bodyIndexPixels[outIndex++] = (byte)((thisColor & 0x0000FF)      ); // Blue channel
                    }
                }
            }

            public Boolean ProcessMultiSourceFrameEvent(MultiSourceFrameArrivedEventArgs eventArgs)
            {
                ProcessColorFrame(eventArgs.FrameReference.AcquireFrame().ColorFrameReference.AcquireFrame());
                ProcessDepthFrame(eventArgs.FrameReference.AcquireFrame().DepthFrameReference.AcquireFrame());
                ProcessInfraredFrame(eventArgs.FrameReference.AcquireFrame().InfraredFrameReference.AcquireFrame());
                ProcessBodyIndexFrame(eventArgs.FrameReference.AcquireFrame().BodyIndexFrameReference.AcquireFrame());

                return HaveAllFrames();
            }
        }
        #endregion
    }
}
