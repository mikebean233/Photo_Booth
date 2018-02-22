using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imaging
{
    class ImageProducerFactory
    {
        public static ImageProducer GetImageProducer()
        {
            #if DEBUG
                return TestImageProducer.Instance;
            #else
                return KinectImageProducer.GetInstance();
            #endif
        }
    }
}
