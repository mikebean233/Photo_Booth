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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Printing;
using Imaging;
using System.Threading;
using System.Collections.Concurrent;

namespace MainApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PrintManager _printManager;
        private ConcurrentQueue<ImageSource> _queue;
        private PrintManager.PrintBatchHandler _currentBatch;
        private bool _readyForCapture = false;

        public MainWindow()
        {
            InitializeComponent();
            
            StartDialog startDialog = new StartDialog();
            startDialog.ShowDialog();
            _printManager = PrintManager.GetInstance(startDialog.Name, startDialog.PrintCount);
            _printManager.SetPrintErrorInformer(HandlePrintError);
            _currentBatch = _printManager.startNewBatch(PrintTemplateType.Wide);

            ImageProducer imageProducer = ImageProducerFactory.GetImageProducer();
            _queue = imageProducer.GetImageQueue();

            Thread consumer = new Thread(new ThreadStart(Consume));
            consumer.Start();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            String statusText = "";
            if (_currentBatch.BatchFinishedPrinting)
                statusText = "session finished, images sent to printer";
            else
                statusText = String.Format("{0}/{1}", _currentBatch.AddedImageCount, _currentBatch.TemplateImageCapacity);

            Dispatcher.Invoke(new Action(() => Label_remCount.Content = statusText));
        }

        private void Consume()
        {
            bool done = false;

            while (!done)
            {
                try
                {
                    ImageSource thisImage = null;
                    if (_queue.TryDequeue(out thisImage))
                    {
                        Dispatcher.Invoke(new Action(() => Image_preview.Source = thisImage));

                        if (_readyForCapture)
                        {
                            _readyForCapture = false;
                            _currentBatch.AddImage(thisImage);
                            if (_currentBatch.RemainingImageCount == 0)
                                _currentBatch.CompleteBatch(1);

                            UpdateStatus();
                        }
                    }
                }
                catch (Exception ex)
                {
                    done = true;
                }
            }
        }

        private void TestErrorDialog(String errorMessages)
        {
            PrinterErrorDialog dialog = new PrinterErrorDialog(errorMessages);
            dialog.ShowDialog();
            System.Diagnostics.Debug.WriteLine("PrintCount: {0}",dialog.PrintCount);
        }

        private void HandlePrintError(String errorMessages)
        {
                

        }

        private void Button_takePicture_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBatch.RemainingImageCount > 0)
                _readyForCapture = true;
        }
    }
}
