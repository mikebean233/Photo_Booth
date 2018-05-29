using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Imaging
{
    public interface ImageProducer
    {
        ConcurrentQueue<ImageSource> GetImageQueue();
        void SetConfiguration(ImageProducerConfiguration config);
        void Start();
        void Cleanup();
    }
}
