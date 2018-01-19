using System;
using System.Windows.Documents;
using System.Printing;

namespace Printing
{
    internal class StandardPrintTemplate : PrintTemplate
    {
        public StandardPrintTemplate(PrintQueue printQueue) : base(2, printQueue)
        {
        }
            
        public override FixedPage Render()
        {
            throw new NotImplementedException();
        }
    }
}
