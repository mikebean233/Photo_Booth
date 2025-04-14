using System;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Media;

namespace Printing
{
    internal abstract class PrintTemplate : ICloneable
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

        public abstract FixedPage Render();
        public abstract object Clone();
    }
}
