using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Imaging
{
    public interface ImageProducer
    {
        ConcurrentQueue<ImageCapture> GetImageQueue();
        void SetConfiguration(ImageProducerConfiguration config);
        void Start();
        void Pause();
        void Continue();
        void Cleanup();
    }
}
