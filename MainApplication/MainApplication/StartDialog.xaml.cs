using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Globalization;
using Microsoft.WindowsAPICodePack.Dialogs;

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
        public int PrintCount { get { return _printCount; } }

        private int _copyCount;
        public int CopyCount { get { return _copyCount; } }

        private String _imageSavePath = "";
        public String ImageSavePath { get { return _imageSavePath; } }

        private bool _debugEnabled = false;
        public bool DebugEnabled { get { return _debugEnabled; } }

        private void ValidateInput()
        {
            bool isValid = true;
            isValid &= IsUsableString(_name);
            isValid &= IsUsableString(_imageSavePath)

                && Directory.Exists(_imageSavePath);
            isValid &= _printCount > 0;

            Button_Ok.IsEnabled = isValid;

        }

        private bool IsUsableString(String input)
        {
            return !String.IsNullOrEmpty(input) && !String.IsNullOrWhiteSpace(input);
        }

        public StartDialog()
        {
            InitializeComponent();
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextBox_name_TextChanged(object sender, TextChangedEventArgs e)
        {
            _name = ((TextBox)sender).Text;
            ValidateInput();
        }

        private void TextBox_printCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            bool parseSucceded = int.TryParse(((TextBox)sender).Text, out _printCount);
            _printCount = parseSucceded ? _printCount : -1;
            ValidateInput();
        }

        /*
        private void TextBox_SaveLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            String value = (sender as TextBox).Text;
            _imageSavePath = value;
            ValidateInput();
        }*/

        private void Button_ChooseDirectory_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                TextBox_SaveLocation.Text = dialog.FileName;
        }

        private void TextBox_SaveLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            String value = ((TextBox)sender).Text;
            _imageSavePath = value;
            ValidateInput();
        }

        private void TextBox_CopyCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            int copyCount = 3;
            string textValue = ((TextBox)sender).Text;
            int.TryParse(textValue, out copyCount);
            _copyCount = copyCount;
        }

        private void CheckBox_DebugEnabled_Changed(object sender, RoutedEventArgs e)
        {
            _debugEnabled = (sender as CheckBox).IsChecked == true;
        }
    }
}
