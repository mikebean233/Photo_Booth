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
        private ImageProducer _imageProducer;
        private bool _readyForCapture = false;
        private Thread _consumer;


        public MainWindow()
        {
            InitializeComponent();
            
            StartDialog startDialog = new StartDialog();
            startDialog.ShowDialog();
            _printManager = PrintManager.GetInstance(startDialog.Name, startDialog.PrintCount);
            _printManager.SetPrintErrorInformer(HandlePrintError);
            _currentBatch = _printManager.startNewBatch(PrintTemplateType.Wide);

            _imageProducer = ImageProducerFactory.GetImageProducer();
            _imageProducer.Start();
            BitmapImage backgroundImage = new BitmapImage(new Uri("pack://application:,,,/enterprise-D-bridge.bmp", UriKind.RelativeOrAbsolute));
            backgroundImage.Freeze();

            ImageProducerConfiguration config = ImageProducerConfiguration.Simple("backgroundImage", backgroundImage);
            _imageProducer.SetConfiguration(config);

            _queue = _imageProducer.GetImageQueue();

            _consumer = new Thread(new ThreadStart(Consume));
            _consumer.Start();
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
            try
            {
                while (true)
                {
                    ImageSource thisImage = null;
                    if (_queue.TryDequeue(out thisImage))
                    {

                        Dispatcher.Invoke(new Action(() =>
                        {
                            Image_preview.Source = thisImage;
                            if (_readyForCapture)
                            {
                                _readyForCapture = false;
                                _currentBatch.AddImage(thisImage);
                                if (_currentBatch.RemainingImageCount == 0)
                                    _currentBatch.CompleteBatch(1);

                                UpdateStatus();
                            }
                        }));
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void HandlePrintError(String errorMessages)
        {
            System.Diagnostics.Debug.WriteLine(errorMessages);            
        }

        private void Button_takePicture_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBatch.RemainingImageCount > 0)
                _readyForCapture = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _imageProducer.Cleanup();
            _consumer.Interrupt();
        }
    }
}
