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
        public string Name { get; private set; } = "";

        private int _printCount;
        public int PrintCount => _printCount;

        public int CopyCount { get; private set; }

        public string ImageSavePath { get; private set; } = "";

        public string BackgroundImagesPath { get; private set; } = "";

        public bool DebugEnabled { get; private set; } = false;

        private void ValidateInput()
        {
            var isValid = true;
            isValid &= IsUsableString(Name);
            isValid &= IsUsableString(ImageSavePath)

                && Directory.Exists(ImageSavePath);
            isValid &= _printCount > 0;

            Button_Ok.IsEnabled = isValid;

        }

        private bool IsUsableString(string input)
        {
            return !string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input);
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
            Name = ((TextBox)sender).Text;
            ValidateInput();
        }

        private void TextBox_printCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            var parseSucceded = int.TryParse(((TextBox)sender).Text, out _printCount);
            _printCount = parseSucceded ? _printCount : -1;
            ValidateInput();
        }

        private void Button_Output_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                TextBox_Output.Text = dialog.FileName;
        }

        private void TextBox_Output_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = ((TextBox)sender).Text;
            ImageSavePath = value;
            ValidateInput();
        }

        private void TextBox_CopyCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textValue = ((TextBox)sender).Text;
            int.TryParse(textValue, out var copyCount);
            CopyCount = copyCount;
        }

        private void CheckBox_DebugEnabled_Changed(object sender, RoutedEventArgs e)
        {
            DebugEnabled = (sender as CheckBox).IsChecked == true;
        }

        private void Button_BackgroundImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                TextBox_BackgroundImages.Text = dialog.FileName;
        }

        private void TextBox_BackgroundImages_TextChanged(object sender, TextChangedEventArgs e)
        {
            var value = ((TextBox)sender).Text;
            BackgroundImagesPath = value;
            ValidateInput();
        }
    }
}
