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
        public Tester()
        {
            InitializeComponent();

            _imageProducer = ImageProducerFactory.GetImageProducer();
            _queue = _imageProducer.GetImageQueue();
            
            Thread consumer = new Thread(new ThreadStart(Consume));
            consumer.Start();
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
                        Dispatcher.Invoke(new Action(() => Image_preview.Source = thisImage));

                    Dispatcher.Invoke(new Action(() => Label_count.Content = _queue.Count + ""));
                }
                catch (Exception ex)
                {
                    done = true;
                }
            }
        }
    }
}
