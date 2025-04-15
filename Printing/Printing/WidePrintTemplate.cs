using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Printing;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;

namespace Printing
{
    internal class WidePrintTemplate : PrintTemplate
    {
        private WideTemplate page;

        public ImageSource TemplateImage { get; private set; }

        public WidePrintTemplate(ImageSource templateImage) : base(3)
        {
            page = new WideTemplate();
            TemplateImage = templateImage;
            page.Background.Source = templateImage;
            page.BeginInit();
            page.InitializeComponent();
            page.EndInit();
        }

        public override FixedPage Render()
        {
            page.Dispatcher.Invoke(new Action(()=>
            {
                page.Image1.Source = imageSources[0];
                page.Image2.Source = imageSources[1];
                page.Image3.Source = imageSources[2];
            }));
            return page.FixedPage;
        }

        public override object Clone()
        {
            PrintTemplate clone = new WidePrintTemplate(TemplateImage);
            foreach (ImageSource thisImage in imageSources)
                clone.AddImage(thisImage);

            return clone;
        }
    }
}

