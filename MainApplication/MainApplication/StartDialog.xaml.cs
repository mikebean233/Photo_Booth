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
    /// Interaction logic for StartDialog.xaml
    /// </summary>
    public partial class StartDialog : Window
    {
        private String _name = "";
        public String Name { get { return _name; } }

        private int _printCount;
        public int PrintCount  { get { return _printCount; } }

        private bool _haveValidInput = false;


        private void ValidateInput()
        {
            bool isValid = !String.IsNullOrEmpty(_name) && !String.IsNullOrWhiteSpace(_name) && _printCount > 0;
            Button_Ok.IsEnabled = isValid;
        }

        public StartDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextBox_name_TextChanged(object sender, TextChangedEventArgs e)
        {
            _name = ((TextBox) sender).Text;
            ValidateInput();
        }

        private void TextBox_printCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool parseSucceded = int.TryParse( ((TextBox) sender).Text , out _printCount);
            _printCount = parseSucceded ? _printCount : -1;
            ValidateInput();
        }
    }
}
