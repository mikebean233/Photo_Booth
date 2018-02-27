using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Imaging
{
    class TestImageProducer : ImageProducer
    {
        private static TestImageProducer _instance;
        public static TestImageProducer Instance { get { return _instance; } }
        private static List<ImageSource> _images = new List<ImageSource>();

        private ConcurrentQueue<ImageSource> _queue = new ConcurrentQueue<ImageSource>();
        private int _curIndex = -1;

        static TestImageProducer()
        {
            _images.Add(new BitmapImage(new Uri("pack://application:,,,/Kinect_Leaning.bmp", UriKind.RelativeOrAbsolute)));
            _images.Add(new BitmapImage(new Uri("pack://application:,,,/Kinect_Standing.bmp", UriKind.RelativeOrAbsolute)));
        
            _instance =  new TestImageProducer();
        }

        private TestImageProducer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += (sender, e) => _queue.Enqueue(_images[_curIndex = (_curIndex + 1) % _images.Count]);
            timer.Start();
        }

        public void Cleanup()
        {
            // Do nothing;
        }

        public ConcurrentQueue<ImageSource> GetImageQueue()
        {
            return _queue; 
        }
        
        public void SetConfiguration(ImageProducerConfiguration config)
        {
            // do nothing
        }
    }
}
