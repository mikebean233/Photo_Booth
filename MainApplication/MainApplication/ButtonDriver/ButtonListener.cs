using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ButtonDriver
{
    class ButtonListener
    {
        private Action<HashSet<int>> _handler;
        private byte _lastState = 0;
        private Thread _listener;
        private SerialPort _port;

        public ButtonListener(Action<HashSet<int>> handler)
        {
            _handler = handler;
            _listener = new Thread(() => 
                {
                    try
                    {
                        while (true)
                        {
                            while (_port == null)
                                _port = getArduinoPort();

                            try
                            {
                                _port.Open();
                                while (_port.IsOpen)
                                {
                                    byte value = (byte)_port.ReadByte();
                                    var newButtonPresses = GetNewButtonPresses(value);
                                    if (newButtonPresses.Count > 0)
                                        _handler.BeginInvoke(newButtonPresses, null, null);
                                }
                            }
                            catch (System.IO.IOException ex) { }
                            _port = null;
                        }
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        if (_port != null)
                            _port.Close();
                    }
                }
             );
            _listener.Start();
        }

        private HashSet<int> GetNewButtonPresses(byte newState)
        {
            var returnValue = new HashSet<int>();
            if (newState != _lastState)
            {
                for (int i = 0; i < 8; ++i)
                {
                    int oldBit = (1 << i & _lastState) == 0 ? 0 : 1;
                    int newBit = (1 << i &   newState) == 0 ? 0 : 1;
                    if (oldBit == 0 && newBit == 1)
                        returnValue.Add(i);
                }
                _lastState = newState;
            }

            return returnValue;
        }

        private static SerialPort getArduinoPort()
        {
            String query = "SELECT DeviceID FROM Win32_SerialPort WHERE Description LIKE '%Arduino%'";
            var searcher = new ManagementObjectSearcher(new ManagementScope(), new SelectQuery(query));

            try
            {
                var enumerator = searcher.Get().GetEnumerator();
                if (enumerator.MoveNext())
                    return new SerialPort(enumerator.Current["DeviceID"].ToString(), 9600, Parity.None, 8, StopBits.One);

            }
            catch (Exception ex) { }

            return null;
        }

    }
}

