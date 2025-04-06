using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Imaging.Properties;

namespace Imaging
{
    class TestImageProducer : ImageProducer
    {
        private static TestImageProducer _instance;
        public static TestImageProducer Instance { get { return GetInstance(); } }
        private static List<BitmapSource> _images = new List<BitmapSource>();
        private static DispatcherTimer _timer;
        private bool _sendingFrames = true;
        private bool _sendHighQualityImage = false;

        private ConcurrentQueue<ImageCapture> _queue = new ConcurrentQueue<ImageCapture>();
        private int _curIndex = -1;

        static TestImageProducer()
        {
            _images.Add(Resources.nerds_1.ToBitmapSource());
            _images.Add(Resources.nerds_2.ToBitmapSource());
            _images.Add(Resources.nerds_3.ToBitmapSource());

            _instance =  new TestImageProducer();
        }

        private static TestImageProducer GetInstance() {
            if (_instance == null)
                _instance = new TestImageProducer();
            return _instance;
        }

        private TestImageProducer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            _timer.Tick += (sender, e) =>
            {
                if (_sendingFrames)
                {
                    _queue.Enqueue(ImageCapture.Build(_images[_curIndex = (_curIndex + 1) % _images.Count],
                        _sendHighQualityImage ? CaptureType.PRINT : CaptureType.PREVIEW));

                    _sendHighQualityImage = false;
                }
            };
        }

        public void Start() { _timer.Start(); }

        public void Cleanup()
        {
            _timer.Stop();
            _images.Clear();
            _images = null;
            _timer = null;
        }

        public ConcurrentQueue<ImageCapture> GetImageQueue()
        {
            return _queue; 
        }
        
        public void SetConfiguration(IDictionary<string, object> config)
        {
            object captureHighQuality = false;
            config.TryGetValue("captureHighQuality", out captureHighQuality);

            if (captureHighQuality is bool && ((captureHighQuality as bool?) == true))
                _sendHighQualityImage = true;
        }

        public void Pause()
        {
            _sendingFrames = false;
        }

        public void Continue()
        {
            _sendingFrames = true;
        }
    }

    static class Extensions
    {
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                // Save the System.Drawing.Bitmap to a memory stream
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);

                memory.Position = 0;

                // Create a BitmapImage (which is a BitmapSource) from the stream
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // necessary for closing the stream after load
                bitmapImage.StreamSource = memory;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // make it cross-thread accessible

                return bitmapImage;
            }
        }
    }
}
