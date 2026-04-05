using System;
using System.Runtime.InteropServices;

namespace Printing
{
    /// <summary>
    /// Queries the HiTi P52x printer for real ribbon status via the Windows Bidi interface.
    /// Requires the P52xBidiLM.dll wrapper language monitor to be installed.
    /// Falls back gracefully (returns null) when Bidi is not available.
    /// </summary>
    public class RibbonMonitor : IDisposable
    {
        private readonly string _printerName;
        private dynamic _bidiSpl;
        private bool _available;

        public int LowRibbonThreshold { get; set; } = 10;

        public RibbonMonitor(string printerName)
        {
            _printerName = printerName;
            try
            {
                var bidiType = Type.GetTypeFromProgID("BidiSpl.BidiSpl");
                if (bidiType == null)
                {
                    _available = false;
                    return;
                }
                _bidiSpl = Activator.CreateInstance(bidiType);
                _bidiSpl.BindDevice(_printerName);
                _available = true;
            }
            catch
            {
                _available = false;
            }
        }

        /// <summary>
        /// Query the printer for actual remaining prints on the ribbon.
        /// Returns null if Bidi is not available or the query fails.
        /// </summary>
        public int? GetRemainingPrints()
        {
            if (!_available) return null;
            try
            {
                var reqType = Type.GetTypeFromProgID("BidiSpl.BidiRequest");
                if (reqType == null) return null;
                dynamic request = Activator.CreateInstance(reqType);
                request.SetSchema(@"\Printer.Consumables.Ribbon:Remaining");
                _bidiSpl.SendRecv(request);
                return (int)request.GetResult();
            }
            catch
            {
                return null;
            }
        }

        public bool IsLowOnRibbon()
        {
            int? remaining = GetRemainingPrints();
            return remaining.HasValue && remaining.Value <= LowRibbonThreshold;
        }

        public void Dispose()
        {
            try { _bidiSpl?.UnbindDevice(); } catch { }
            if (_bidiSpl != null)
            {
                try { Marshal.ReleaseComObject(_bidiSpl); } catch { }
            }
        }
    }
}
