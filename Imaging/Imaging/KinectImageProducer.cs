using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
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

            _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Infrared)
                .MultiSourceFrameArrived += OnMultiSourceFrameArrived;
            
            _sensor.Open();
        }

        public void Cleanup()
        {
            _sensor.Close();
        }

        private void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
        {
            _frameManager.ProcessMultiSourceFrameEvent(multiSourceFrameArrivedEventArgs);
            _queue.Enqueue(_frameManager.BuildBitmapSourceFromFrame(SourceType.INFRARED));
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
            private static readonly Dictionary<int, UInt32> _bodyIndexToColorMap = new Dictionary<int, UInt32>();
            private static readonly Dictionary<UInt32, int> _colorToBodyIndexMap = new Dictionary<UInt32, int>();
            private static readonly Dictionary<SourceType, byte[]> _displayableBuffers = new Dictionary<SourceType, byte[]>();
            private static readonly PixelFormat _outputPixelFormat = PixelFormats.Bgra32;

            private static readonly UInt16[] _rawDepthPixels;
            private static readonly UInt16[] _rawInfraredPixels;
            private static readonly byte[]   _rawBodyIndexPixels;
            
            static FrameManager()
            {
                _instance = new FrameManager();

                Box Box_512_424 = Box.With(512, 424);

                _frameResolutions[SourceType.COLOR] = Box.With(1920, 1080);
                _frameResolutions[SourceType.DEPTH] = Box_512_424;
                _frameResolutions[SourceType.INFRARED] = Box_512_424;
                _frameResolutions[SourceType.BODY_INDEX] = Box_512_424;

                foreach (SourceType thisSourceType in _frameResolutions.Keys)
                    _displayableBuffers[thisSourceType] = new byte[ _frameResolutions[thisSourceType].Area * (_outputPixelFormat.BitsPerPixel / 8) ];
                
                _bodyIndexToColorMap.Add(0, 0xFF0000FF); // BLUE
                _bodyIndexToColorMap.Add(1, 0x00FF00FF); // GREEN
                _bodyIndexToColorMap.Add(2, 0x0000FFFF); // RED
                _bodyIndexToColorMap.Add(3, 0x00FFFFFF); // YELLOW
                _bodyIndexToColorMap.Add(4, 0xFF00FFFF); // PURPLE
                _bodyIndexToColorMap.Add(5, 0xFFFFFFFF); // WHITE

                foreach (int bodyIndex in _bodyIndexToColorMap.Keys)
                    _colorToBodyIndexMap.Add(_bodyIndexToColorMap[bodyIndex], bodyIndex);

                _rawDepthPixels = new UInt16[_frameResolutions[SourceType.DEPTH].Area];
                _rawInfraredPixels = new UInt16[_frameResolutions[SourceType.INFRARED].Area];
                _rawBodyIndexPixels  = new byte[_frameResolutions[SourceType.BODY_INDEX].Area];
            }
            private FrameManager() { }

            public static FrameManager Instance { get { return _instance; } }
        
            public BitmapSource BuildBitmapSourceFromFrame(SourceType frameSourceType)
            {
                if (_frameResolutions.ContainsKey(frameSourceType))
                {
                    Box dimensions = _frameResolutions[frameSourceType];
                    byte[] outPixels = _displayableBuffers[frameSourceType];
                    return BitmapSource.Create(dimensions.Width, dimensions.Height, 96, 96, _outputPixelFormat, null, outPixels, dimensions.Width * _outputPixelFormat.BitsPerPixel / 8);
                }
                return null;
            }

            private void ProcessColorFrame(ColorFrame colorFrame)
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyConvertedFrameDataToArray(_displayableBuffers[SourceType.COLOR] , ColorImageFormat.Bgra);
                    colorFrame?.Dispose();
                }
            }    

            private void ProcessDepthFrame(DepthFrame depthFrame)
            {
                if (depthFrame != null)
                {
                    depthFrame.CopyFrameDataToArray(_rawDepthPixels);
                    depthFrame?.Dispose();

                    int outIndex = 0;
                    for (int index = 0; index < _rawDepthPixels.Length; ++index)
                    {
                        UInt16 pixel = _rawDepthPixels[index];
                        byte intensity = (byte)(pixel);

                        _displayableBuffers[SourceType.DEPTH][outIndex++] = intensity;
                        _displayableBuffers[SourceType.DEPTH][outIndex++] = intensity;
                        _displayableBuffers[SourceType.DEPTH][outIndex++] = intensity;
                        _displayableBuffers[SourceType.DEPTH][outIndex++] = 0xFF;
                    }
                }
            }

            private void ProcessInfraredFrame(InfraredFrame infraredFrame)
            {
                if (infraredFrame != null)
                {
                    infraredFrame.CopyFrameDataToArray(_rawInfraredPixels);
                    infraredFrame?.Dispose();
                    
                    int outIndex = 0;
                    for (int index = 0; index < _rawInfraredPixels.Length; ++index)
                    {
                        UInt16 pixel = _rawInfraredPixels[index];
                        byte intensity = (byte)(pixel >> 8);

                        _displayableBuffers[SourceType.INFRARED][outIndex++] = intensity;
                        _displayableBuffers[SourceType.INFRARED][outIndex++] = intensity;
                        _displayableBuffers[SourceType.INFRARED][outIndex++] = intensity;
                        _displayableBuffers[SourceType.INFRARED][outIndex++] = 0xFF;
                    }
                }
            }

            private void ProcessBodyIndexFrame(BodyIndexFrame bodyIndexFrame)
            {
                if (bodyIndexFrame != null)
                {
                    byte[] outBuffer = _displayableBuffers[SourceType.BODY_INDEX];
                    bodyIndexFrame.CopyFrameDataToArray(_rawBodyIndexPixels);
                    bodyIndexFrame.Dispose();
                    
                    int outIndex = 0;
                    for (int inIndex = 0; inIndex < _rawBodyIndexPixels.Length; ++inIndex)
                    {
                        int bodyIndex = _rawBodyIndexPixels[inIndex];
                        UInt32 thisColor = 0;
                        _bodyIndexToColorMap.TryGetValue(bodyIndex, out thisColor);
                        outBuffer[outIndex++] = (byte)((thisColor & 0xFF000000) >> 32); // Blue channel
                        outBuffer[outIndex++] = (byte)((thisColor & 0x00FF0000) >> 16); // Green channel
                        outBuffer[outIndex++] = (byte)((thisColor & 0x0000FF00) >>  8); // Red channel
                        outBuffer[outIndex++] = (byte)((thisColor & 0x000000FF)      ); // Alpha channel 
                    }
                }
            }

            public void ProcessMultiSourceFrameEvent(MultiSourceFrameArrivedEventArgs eventArgs)
            {
                ProcessColorFrame(eventArgs.FrameReference.AcquireFrame().ColorFrameReference.AcquireFrame());
                ProcessDepthFrame(eventArgs.FrameReference.AcquireFrame().DepthFrameReference.AcquireFrame());
                ProcessInfraredFrame(eventArgs.FrameReference.AcquireFrame().InfraredFrameReference.AcquireFrame());
                ProcessBodyIndexFrame(eventArgs.FrameReference.AcquireFrame().BodyIndexFrameReference.AcquireFrame());
            }
        }
        #endregion
    }
}
