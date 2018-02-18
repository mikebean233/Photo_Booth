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
        private int totalImages, addedImageCount;
        protected List<ImageSource> imageSources = new List<ImageSource>();

        protected PrintTemplate(int totalImages)
        {
            this.totalImages = totalImages;
            
        }
        
        public int ImageCapacity()
        {
            return totalImages;
        }

        public Boolean CanAddMoreImages()
        {
            return addedImageCount < totalImages;
        }

        public Boolean AddImage(ImageSource imageSource)
        {
            Boolean canAddMoreImages = CanAddMoreImages();
            if (imageSource != null && canAddMoreImages)
            {
                imageSources.Add(imageSource);
                ++addedImageCount;
            }
            return canAddMoreImages;
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
