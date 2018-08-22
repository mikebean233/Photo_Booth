using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MainApplication
{
    public class BackgroundImage
    {
        private String _name;
        public String Name { get { return _name; } }

        private BitmapSource _image;
        public BitmapSource Image { get { return _image; } }

        public BackgroundImage(String name, BitmapSource image)
        {
            _name = name;
            _image = image;
        }
    }
}
