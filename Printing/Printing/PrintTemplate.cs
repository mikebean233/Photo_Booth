using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Printing;
using System.Windows.Threading;

namespace Printing
{
    internal abstract class PrintTemplate
    {
        private int _imageCapacity, _addedImageCount = 0;
        public int ImageCapacity { get { return _imageCapacity; } }

        protected List<ImageSource> imageSources = new List<ImageSource>();

        protected PrintTemplate(int imageCapacity)
        {
            _imageCapacity = imageCapacity;
        }
        
        public Boolean CanAddMoreImages()
        {
            return _addedImageCount < _imageCapacity;
        }

        public Boolean AddImage(ImageSource imageSource)
        {
            if (imageSource != null && CanAddMoreImages())
            {
                imageSources.Add(imageSource);
                ++_addedImageCount;
                return true;
            }
            return false;
        }

        public static PrintTemplate OfType(PrintTemplateType type)
        {
            switch (type)
            {
                case PrintTemplateType.Wide:
                    return new WidePrintTemplate();
                case PrintTemplateType.Standard:
                    return new StandardPrintTemplate();
            }
            return null;
        }

        public abstract FixedPage Render();
    }
}
