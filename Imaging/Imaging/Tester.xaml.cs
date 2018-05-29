using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
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
using System.Collections.Concurrent;
using System.Threading;

namespace Imaging
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Tester : Window
    {
        ImageProducer _imageProducer;
        ConcurrentQueue<ImageSource> _queue;
        Thread _consumer;

        public Tester()
        {
            BitmapImage backgroundImage = new BitmapImage(new Uri("pack://application:,,,/enterprise-D-bridge.bmp", UriKind.RelativeOrAbsolute));
            backgroundImage.Freeze();
            ImageProducerConfiguration config = ImageProducerConfiguration.Simple("backgroundImage", backgroundImage);


            //InitializeComponent();
            _imageProducer = ImageProducerFactory.GetImageProducer();
            _imageProducer.Start();
            _imageProducer.SetConfiguration(config);
            _queue = _imageProducer.GetImageQueue();
            
            _consumer = new Thread(new ThreadStart(Consume));
            _consumer.Start();
            this.Closing += Tester_Closing;
        }

        private void Tester_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _consumer.Interrupt();
            _imageProducer.Cleanup();
            _consumer.Join();
            _consumer = null;
       }

        private void Consume()
        {
                try
                {
                    while (true)
                    {
                        Thread.Sleep(10);
                        ImageSource thisImage = null;
                        if (_queue.TryDequeue(out thisImage))
                            Dispatcher.Invoke(new Action(() => Image_preview.Source = thisImage));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Tester image consumer thread shutting down...");
                }
        }
    }
}
