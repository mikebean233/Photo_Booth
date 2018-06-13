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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Imaging
{
    class KinectImageProducer : ImageProducer
    {
        private ConcurrentQueue<ImageSource> _queue;
        private KinectSensor _sensor;
        private FrameManager _frameManager;
        private int _maxQueueSize = 100;
        private MaxFrameRateMinder _frameRateMinder;
        private Thread _producer;
        
        private KinectImageProducer()
        {
            _queue = new ConcurrentQueue<ImageSource>();
            _producer = new Thread(new ThreadStart(Setup));
        }

        public void Start() { _producer.Start();}

        private void Setup()
        {
            _frameRateMinder = new MaxFrameRateMinder(60);
            _frameManager = FrameManager.Instance;
            _sensor = KinectSensor.GetDefault();
            _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth)
                .MultiSourceFrameArrived += OnMultiSourceFrameArrived;
            _frameManager.CoordinateMapper = _sensor.CoordinateMapper;
            _sensor.Open();

            try
            {
                while (true)
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _sensor.Close();
                System.Diagnostics.Debug.WriteLine("Kinect image producer thread shutting down...");
            }
        }


        public void Cleanup()
        {
            _sensor.Close();
            _producer.Interrupt();
            _producer.Join();
        }

        private void OnMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs multiSourceFrameArrivedEventArgs)
        {
            if (_queue.Count < _maxQueueSize && _frameRateMinder.canGrabNewFrame())
            {
                _frameManager.ProcessMultiSourceFrameEvent(multiSourceFrameArrivedEventArgs);
                _queue.Enqueue(_frameManager.BuildBitmapSourceFromFrame(SourceType.GREEN_SCREEN));
            }
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
            Action action = () =>
            {
                while (_frameManager == null)
                    Thread.Sleep(10);

                DoThingIfPresent<Dictionary<String, BitmapImage>>("loadBackgroundImages" , config, _frameManager.LoadBackgroudBuffer);
                DoThingIfPresent<String>                         ("selectBackgroundImage", config, _frameManager.SetBackgroundImage);
                DoThingIfPresent<float>                          ("depthThreshold"       , config, _frameManager.SetDepthThreshold);
            };

            if (_frameManager == null)
                (new Thread(new ThreadStart(action))).Start();
            else
                action.Invoke();
        }
        
        private void DoThingIfPresent<T>(String key, ImageProducerConfiguration config, Action<T> action)
        {
            if (config.ContainsKey(key))
            {
                Object value = config.Get(key);
                if(value is T)
                    action.BeginInvoke((T)value, null, null);
            }
        }

        #region Implementation Details

        private class MaxFrameRateMinder
        {
            double _minMilliSecondsPerFrame;
            int _lastAllowedFrameTime = -1;
            public MaxFrameRateMinder(int maxFrameRate)
            {
                _minMilliSecondsPerFrame = 1000.0 / maxFrameRate;
            }

            public Boolean canGrabNewFrame()
            {
                int currentTime = Environment.TickCount;
                if (currentTime - _lastAllowedFrameTime > _minMilliSecondsPerFrame)
                {
                    _lastAllowedFrameTime = currentTime;

                    return true;
                }
                else {
                    return false;
                }
            }
        }

        private class Box
        {
            public static Box S_512_424 = With(512, 424);
            public static Box S_1920_1080 = With(1920, 1080);

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
            public static ISet<SourceType> Values { get { return _values; } }
            
            public static readonly SourceType COLOR = new SourceType(0, "color");
            public static readonly SourceType DEPTH = new SourceType(1, "depth");
            public static readonly SourceType INFRARED = new SourceType(2, "infrared");
            public static readonly SourceType BODY_INDEX = new SourceType(3, "bodyindex");
            public static readonly SourceType GREEN_SCREEN = new SourceType(4, "greenscreen");
            public static readonly SourceType BACKGROUND = new SourceType(5, "background");

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
            private static CoordinateMapper _coordinateMapper;
            private static readonly Dictionary<SourceType, Box> _frameResolutions = new Dictionary<SourceType, Box>();
            private static readonly Dictionary<SourceType, PixelFormat> _framePixelFormats = new Dictionary<SourceType, PixelFormat>();
            private static readonly Dictionary<int, UInt32> _bodyIndexToColorMap = new Dictionary<int, UInt32>();
            private static readonly Dictionary<UInt32, int> _colorToBodyIndexMap = new Dictionary<UInt32, int>();
            private static readonly Dictionary<SourceType, byte[]> _displayableBuffers = new Dictionary<SourceType, byte[]>();
            private static Dictionary<String, byte[]> _backgroundImages = new Dictionary<string, byte[]>();
            private static float _depthThresholdInMeters;

            private static readonly PixelFormat _outputPixelFormat = PixelFormats.Bgra32;
            private static KinectSensor _sensor;

            private static readonly ushort[] _rawDepthPixels;
            private static readonly ushort[] _rawInfraredPixels;
            private static readonly byte[] _rawBodyIndexPixels;
            private static readonly CameraSpacePoint[] _cameraSpacePoints;

            static FrameManager()
            {
                _instance = new FrameManager();
                _sensor = KinectSensor.GetDefault();
                _frameResolutions[SourceType.COLOR] = Box.S_1920_1080;
                _frameResolutions[SourceType.GREEN_SCREEN] = Box.S_1920_1080;
                _frameResolutions[SourceType.DEPTH] = Box.S_512_424;
                _frameResolutions[SourceType.INFRARED] = Box.S_512_424;
                _frameResolutions[SourceType.BODY_INDEX] = Box.S_512_424;
                _frameResolutions[SourceType.BACKGROUND] = _frameResolutions[SourceType.GREEN_SCREEN];

                foreach (SourceType thisSourceType in _frameResolutions.Keys)
                    _displayableBuffers[thisSourceType] = new byte[_frameResolutions[thisSourceType].Area * (_outputPixelFormat.BitsPerPixel / 8)];

                _bodyIndexToColorMap.Add(0, 0xFF0000FF); // BLUE
                _bodyIndexToColorMap.Add(1, 0x00FF00FF); // GREEN
                _bodyIndexToColorMap.Add(2, 0x0000FFFF); // RED
                _bodyIndexToColorMap.Add(3, 0x00FFFFFF); // YELLOW
                _bodyIndexToColorMap.Add(4, 0xFF00FFFF); // PURPLE
                _bodyIndexToColorMap.Add(5, 0xFFFFFFFF); // WHITE

                foreach (int bodyIndex in _bodyIndexToColorMap.Keys)
                    _colorToBodyIndexMap.Add(_bodyIndexToColorMap[bodyIndex], bodyIndex);

                _instance.InitializeBackgroundImage();

                _rawDepthPixels = new UInt16[_frameResolutions[SourceType.DEPTH].Area];
                _rawInfraredPixels = new UInt16[_frameResolutions[SourceType.INFRARED].Area];
                _rawBodyIndexPixels = new byte[_frameResolutions[SourceType.BODY_INDEX].Area];
                _cameraSpacePoints = new CameraSpacePoint[Box.S_1920_1080.Area];
            }
            private FrameManager() { }
            public static FrameManager Instance { get { return _instance; } }
            
            public CoordinateMapper CoordinateMapper
            {
                set
                {
                    if (value != null)
                        _coordinateMapper = value;
                }
                get
                {
                    return _coordinateMapper;
                }
            }

            public BitmapSource BuildBitmapSourceFromFrame(SourceType frameSourceType)
            {
                if (_frameResolutions.ContainsKey(frameSourceType))
                {
                    Box dimensions = _frameResolutions[frameSourceType];
                    byte[] outPixels = _displayableBuffers[frameSourceType];
                    BitmapSource output =  BitmapSource.Create(dimensions.Width, dimensions.Height, 96, 96, _outputPixelFormat, null, outPixels, dimensions.Width * _outputPixelFormat.BitsPerPixel / 8);
                    output.Freeze();
                    return output;
                }
                return null;
            }

            private void InitializeBackgroundImage()
            {
                uint color = 0xFF0000FF; // BLUE
                byte[] backgroundBuffer = _displayableBuffers[SourceType.BACKGROUND];
                int bytesPerPixel = _outputPixelFormat.BitsPerPixel / 8;

                for (int pixelIndex = 0; pixelIndex < _frameResolutions[SourceType.BACKGROUND].Area; pixelIndex++)
                {
                    for (byte bitIndex = 0; bitIndex < bytesPerPixel; bitIndex++)
                    {
                        byte shiftBits = (byte)(8 * bitIndex);
                        uint mask = (uint)(0xFF << shiftBits);
                        backgroundBuffer[(pixelIndex* bytesPerPixel) + bitIndex] = (byte)((mask & color) >> shiftBits); 
                    }
                }
            }

            public void SetBackgroundImage(String imageName)
            {
                if (_backgroundImages.ContainsKey(imageName))
                    _displayableBuffers[SourceType.BACKGROUND] = _backgroundImages[imageName];
            }

            public void SetDepthThreshold(float value)
            {
                if (value >= 0.5f && value <= 8.0f)
                    _depthThresholdInMeters = value;
            }

            public void LoadBackgroudBuffer(Dictionary<String, BitmapImage> inputMap)
            {
                if (inputMap != null)
                {
                    Dictionary<String, byte[]> outputMap = new Dictionary<String, byte[]>();

                    foreach (var entry in inputMap)
                    {
                        String key = entry.Key;
                        byte[] buffer = BuildBackgroundBuffer(entry.Value);
                        if (buffer != null)
                            outputMap.Add(key, buffer);
                    }
                    if (outputMap.Keys.Count > 0)
                        _backgroundImages = outputMap;
                }
            }

            public byte[] BuildBackgroundBuffer(BitmapImage image)
            {
                int pixelCount = _frameResolutions[SourceType.BACKGROUND].Area;
                int outputBytesPerPixel = _outputPixelFormat.BitsPerPixel / 8;

                byte[] output = null;

                PixelFormat inputFormat = image.Format;
                if (image != null && 
                    image.PixelHeight == _frameResolutions[SourceType.BACKGROUND].Height && 
                    image.PixelWidth  == _frameResolutions[SourceType.BACKGROUND].Width)
                {
                    output = new byte[pixelCount * (_outputPixelFormat.BitsPerPixel / 8)];
                    int inputBytesPerPixel = inputFormat.BitsPerPixel / 8;
                    int stride = image.PixelWidth * (inputBytesPerPixel);
                    byte[] inputBuffer = new byte[(int)image.PixelHeight * stride];
                    image.CopyPixels(inputBuffer, stride, 0);
                    
                    for (int pixelIndex = 0; pixelIndex < _frameResolutions[SourceType.BACKGROUND].Area; ++pixelIndex)
                    {
                        int inputBaseIndex = pixelIndex * inputBytesPerPixel;
                        int outputBaseIndex = pixelIndex * outputBytesPerPixel;

                        // TODO: Add support for more pixel formats
                        output[inputBaseIndex + 0] = inputBuffer[outputBaseIndex + 0];
                        output[inputBaseIndex + 1] = inputBuffer[outputBaseIndex + 1];
                        output[inputBaseIndex + 2] = inputBuffer[outputBaseIndex + 2];
                        output[inputBaseIndex + 3] = 0xff;
                    }
                }
                return output;
            }
            
            private void ProcessColorFrame(ColorFrame colorFrame)
            { 
                if (colorFrame != null)
                {
                    colorFrame.CopyConvertedFrameDataToArray(_displayableBuffers[SourceType.COLOR], ColorImageFormat.Bgra);
                }
            }

            private void ProcessDepthFrame(DepthFrame depthFrame)
            {
                if (depthFrame != null)
                {
                    depthFrame.CopyFrameDataToArray(_rawDepthPixels);
                    
                    int outIndex = 0;
                    for (int index = 0; index < _rawDepthPixels.Length; ++index)
                    {
                        UInt16 pixel = _rawDepthPixels[index];
                        byte highByte = (byte)((pixel & 0xFF00) >> 8);
                        byte lowByte = (byte)((pixel & 0x00FF));
                        byte downScaled = (byte)((1 - ((float)pixel / (float)UInt16.MaxValue)) * (float)Byte.MaxValue);

                        _displayableBuffers[SourceType.DEPTH][outIndex++] = downScaled;
                        _displayableBuffers[SourceType.DEPTH][outIndex++] = downScaled;
                        _displayableBuffers[SourceType.DEPTH][outIndex++] = downScaled;
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
                        outBuffer[outIndex++] = (byte)((thisColor & 0x0000FF00) >> 8); // Red channel
                        outBuffer[outIndex++] = (byte)((thisColor & 0x000000FF)); // Alpha channel 
                    }
                }
            }

            private void ProcessGreenScreenFrame()
            {
                if (_coordinateMapper != null && _rawDepthPixels != null && _cameraSpacePoints != null)
                {
                    _coordinateMapper.MapColorFrameToCameraSpace(_rawDepthPixels, _cameraSpacePoints);

                    byte[] colorBuffer = _displayableBuffers[SourceType.COLOR];
                    byte[] greenScreenBuffer = _displayableBuffers[SourceType.GREEN_SCREEN];
                    byte[] backgroundBuffer = _displayableBuffers[SourceType.BACKGROUND];
                    int outputBytesPerPixel = _outputPixelFormat.BitsPerPixel / 8;

                    int colorPixelIndex = 0;
                    foreach (CameraSpacePoint thisPoint in _cameraSpacePoints)
                    {
                        int colorBufferIndex = outputBytesPerPixel * colorPixelIndex;
                        float depthValue = thisPoint.Z;

                        // Determine where this pixel will come from
                        byte[] pixelSourceArray = (float.IsNegativeInfinity(depthValue) || depthValue > _depthThresholdInMeters) ? backgroundBuffer : colorBuffer;

                        for (int i = 0; i < outputBytesPerPixel; ++i)
                            greenScreenBuffer[colorBufferIndex + i] = pixelSourceArray[colorBufferIndex + i];
                    
                        ++colorPixelIndex;
                    }
                }
            }

            public void ProcessMultiSourceFrameEvent(MultiSourceFrameArrivedEventArgs eventArgs)
            {
                MultiSourceFrame multiSourceFrame = eventArgs.FrameReference.AcquireFrame();
                using (var colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        using (var depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                        {
                            ProcessColorFrame(colorFrame);
                            ProcessDepthFrame(depthFrame);
                            ProcessGreenScreenFrame();
                        }
                    }
            }
            #endregion
        }
    }
}
