using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Imaging
{
    public class ImageCapture
    {
        private readonly CaptureType _captureType;
        public CaptureType CaptureType { get{ return _captureType;} }
    
        private readonly BitmapSource _image;
        public BitmapSource Image { get { return _image; } }

        private ImageCapture(BitmapSource image, CaptureType captureType)
        {
            _image = image;
            _captureType = captureType;
        }

        public static ImageCapture Build(BitmapSource image, CaptureType captureType)
        {
            return new ImageCapture(image, captureType);
        }
    }

    public enum CaptureType
    {
        PREVIEW,
        PRINT
    }
}
