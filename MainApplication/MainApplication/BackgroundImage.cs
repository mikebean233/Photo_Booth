using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MainApplication
{
    public class BackgroundImage
    {
        public string Name { get; }
        public BitmapSource Image { get; }
        public BitmapSource FlippedImage { get; }

        public BackgroundImage(string name, BitmapSource image)
        {
            Name = name;
            Image = image;
            FlippedImage = flipImageHor(image);
        }

        private BitmapSource flipImageHor(BitmapSource inImage)
        {
            int width = inImage.PixelWidth;
            int height = inImage.PixelHeight;
            int pixelCount = width * height;

            PixelFormat pixelFormat = inImage.Format;
            int bytesPerPixel = pixelFormat.BitsPerPixel / 8;

            byte[] output = new byte[pixelCount * bytesPerPixel];
            byte[] input = new byte[pixelCount * bytesPerPixel];
            int stride = inImage.PixelWidth * (bytesPerPixel);

            inImage.CopyPixels(input, stride, 0);

            for (int pixelIndex = 0; pixelIndex < pixelCount; ++pixelIndex)
            {
                int inputBaseIndex = pixelIndex * bytesPerPixel;

                int row = pixelIndex / width;
                int col = pixelIndex % width;

                int outCol = width - col - 1;

                int outputBaseIndex = ((width * row) + outCol) * bytesPerPixel;

                for (int channelIndex = 0; channelIndex < bytesPerPixel; ++channelIndex)
                    output[outputBaseIndex + channelIndex] = input[inputBaseIndex + channelIndex];
            }
            BitmapSource outImage = BitmapSource.Create(width, height, inImage.DpiX, inImage.DpiY, pixelFormat, inImage.Palette, output, stride);
            outImage.Freeze();
            return outImage;
        }
    }
}
