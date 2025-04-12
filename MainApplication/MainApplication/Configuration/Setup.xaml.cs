using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;


namespace MainApplication.Configuration
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : Window
    {
        public Config Input { get; } = new Config();
        public Boolean SubmitClicked { get; internal set; } = false;

        public Setup(Config input)
        {
            Input = input;
            InitializeComponent();
            this.DataContext = Input;
        }

        private void PickerButtonClick(object sender, RoutedEventArgs e)
        {
            var button = (e.Source as Button);
            if(ButtonSubmit.Equals(button)){
                SubmitClicked = true;
                this.Close();
                return;
            }

            using CommonDialog dialog = e.Source.Equals(ButtonPrintTemplatePath) ? new OpenFileDialog() : new FolderBrowserDialog();
            var tag = button?.Tag as string;

            var result = ButtonPrintTemplatePath.Equals(button) ? GetFile(button.Tag as string) : GetDir(button.Tag as string);

            if (result.Item1 == System.Windows.Forms.DialogResult.OK)
            {
                var path = result.Item2;
                var buttonActionMap = new Dictionary<Button, Action>
                {
                    { ButtonOutputDirectory, () => TextBoxOutputDirectory.Text = path },
                    { ButtonBackgroundImagesDirectory, () => TextBoxBackgroundImagesDirectory.Text = path },
                    { ButtonPrintTemplatePath, () => TextBoxPrintTemplatePath.Text = path },
                };

                buttonActionMap[button]();
            }
        }

        private (DialogResult, string) GetDir(string title)
        {
            using var dialog = new FolderBrowserDialog()
            {
                RootFolder = Environment.SpecialFolder.UserProfile
            };
            var result = dialog.ShowDialog();
            return (result, dialog.SelectedPath);
        }

        private (DialogResult, string) GetFile(string title)
        {
            using var dialog = new OpenFileDialog()
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Multiselect = false,
                Title = title,
                Filter = "*.png;*.gif"
            };
            
            var result = dialog.ShowDialog();
            return (result, dialog.FileName);
        }
    }
} 
