using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Printing;

namespace Printing
{
    class WidePrintTemplate : PrintTemplate
    {
        private Printing.Page1 page = new Printing.Page1();

        public WidePrintTemplate(PrintQueue printQueue) : base(4, printQueue)
        {
            page.BeginInit();
            page.InitializeComponent();
            page.EndInit();
        }

        public override FixedPage Render()
        {
            return page.Page1_FixedPage;
        }
    }
}
