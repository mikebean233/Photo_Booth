using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for PrinterErrorDialog.xaml
    /// </summary>
    public partial class PrinterErrorDialog : Window
    {
        private String _printErrors;
        private bool _needPrintCount;
        public bool WasOutOfPaper { get { return _needPrintCount; } }

        private int _printCount;
        public int PrintCount {get{return _printCount;} }
        
        public void ValidateInput()
        {
            Button_Ok.IsEnabled = _printCount > 0;
        }
        
        public PrinterErrorDialog(String printErrors)
        {
            InitializeComponent();
            Window.Topmost = true;
            _printErrors = printErrors ?? "";
            _needPrintCount = _printErrors.Contains("out of paper");
            TextBlock_errorMessages.Text = _printErrors;
            Button_Ok.IsEnabled = !_needPrintCount;
            TextBox_printCount.Visibility = _needPrintCount ? Visibility.Visible : Visibility.Collapsed;
            Label_printCount.Visibility = _needPrintCount ? Visibility.Visible : Visibility.Collapsed;
            Window.Title = _needPrintCount ? "OUT OF PAPER" : "Print Error";
            Window.Activate();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_printCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            String value = ((TextBox)sender).Text;
            bool parseSucceded = int.TryParse(value, out _printCount);
            ValidateInput();
        }
    }
}
