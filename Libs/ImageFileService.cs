using System.Globalization;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Libs
{
    public static class ImageFileService
    {
        public static string WriteImageFile(string prefix, FixedPage fixedPage, Config config)
        {
            var dpi = 600;
            var renderTarget = new RenderTargetBitmap(
                (int)((fixedPage.ActualWidth) * dpi / 96),
                (int)((fixedPage.ActualHeight) * dpi / 96),
                dpi,
                dpi,
                PixelFormats.Pbgra32);

            renderTarget.Render(fixedPage);

            return WriteImageFile(prefix, renderTarget, config);
        }

        public static string WriteImageFile(string prefix, BitmapSource imageSource, Config config)
        {
            var timestampString = BuildTimestampString();
            var filename = $"{prefix}_{timestampString}";
            var filePath = BuildAbsoluteFilePath(config.OutputDir, filename);
            WriteImageFile(imageSource, filePath);
            return timestampString;
        }

        private static string BuildTimestampString()
        {
            return DateTime.Now.ToString("MMM_dd_yyyy_hh_mm_ss", CultureInfo.InvariantCulture);
        }

        private static string BuildAbsoluteFilePath(string path, string filename)
        {
            return $"{path}\\{filename}.bmp";
        }

        private static void WriteImageFile(BitmapSource imageSource, string filename)
        {
            var stream = new FileStream(filename, FileMode.Create);
            var encoder = new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource));
            encoder.Save(stream);
        }
    }
}
