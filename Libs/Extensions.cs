using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Libs
{
    public static class Extensions
    {
        public static Bitmap ToBMP(this FixedPage page, Config config)
        {
            page.Measure(new System.Windows.Size(page.Width, page.Height));
            page.Arrange(new System.Windows.Rect(0, 0, page.Width, page.Height));
            page.UpdateLayout();

            int pixelWidth = (int)(page.Width * config.PrintDPI / 96.0);
            int pixelHeight = (int)(page.Height * config.PrintDPI / 96.0);

            var rtb = new RenderTargetBitmap(pixelWidth,pixelHeight,config.PrintDPI,config.PrintDPI,PixelFormats.Pbgra32);

            rtb.Render(page);


            var encoder = new PngBitmapEncoder(); // lossless
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            return new Bitmap(ms);
        }
    }
}
