using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Printing;

namespace Printing
{
    internal abstract class PrintTemplate
    {
        private int totalImages, addedImageCount;
        private List<ImageSource> imageSources = new List<ImageSource>();
        
        protected PrintTemplate(int totalImages, PrintQueue printQueue)
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

        public static PrintTemplate ofType(PrintTemplateType type, PrintQueue printQueue)
        {
            switch (type)
            {
                case PrintTemplateType.Wide:
                    return new WidePrintTemplate(printQueue);
                case PrintTemplateType.Standard:
                    return new StandardPrintTemplate(printQueue);
            }
            return null;
        }

        public abstract FixedPage Render();
    }
}
