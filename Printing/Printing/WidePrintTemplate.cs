using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Printing;
using System.Threading;
using System.Windows.Threading;

namespace Printing
{
    internal class WidePrintTemplate : PrintTemplate
    {
        private WideTemplate page;
        public WidePrintTemplate() : base(2)
        {
            page = new WideTemplate();
            //page.Dispatcher.UnhandledException += errorHandler;
            page.BeginInit();
            page.InitializeComponent();
            page.EndInit();
        }

        public override FixedPage Render()
        {
            page.ImageTop.Source = imageSources[0];
            page.ImageCenter.Source = imageSources[1];
            return page.FixedPage;
        }
    }
}

