using System;
using System.Windows.Documents;
using System.Printing;
using System.Windows.Threading;

namespace Printing
{
    internal class StandardPrintTemplate : PrintTemplate
    {
        public StandardPrintTemplate() : base(2)
        {
        }
            
        public override FixedPage Render()
        {
            throw new NotImplementedException();
        }
    }
}
