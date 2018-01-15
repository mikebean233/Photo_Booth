using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Printing;
     
namespace Printing
{
    class StandardPrintTemplate : PrintTemplate
    {
        public StandardPrintTemplate(PrintQueue printQueue) : base(4, printQueue)
        {
        }
            
        public override FixedPage Render()
        {
            throw new NotImplementedException();
        }
    }
}
