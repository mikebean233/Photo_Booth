using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Controls;

namespace Printing
{
    class NullPrintTemplate : PrintTemplate
    {
        public static NullPrintTemplate Instance { get; } = new NullPrintTemplate();

        private NullPrintTemplate() : base(0){}
        public new Boolean CanAddMoreImages(){return false;}
        public new Boolean AddImage(ImageSource imageSource) {throw new NotImplementedException(); }
        public override FixedPage Render(){throw new NotImplementedException();}

        public override object Clone()
        {
            return Instance;
        }
    }
}
