using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Printing;
using System.Threading;

namespace Printing
{
    internal class WidePrintTemplate : PrintTemplate
    {
        private WideTemplate page;
        public WidePrintTemplate(PrintQueue printQueue) : base(2, printQueue)
        {
            page = new WideTemplate();
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
