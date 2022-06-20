using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared
{
    public sealed class DisposalToken : IDisposable
    {
        private readonly Action _disposeCallback;

        public DisposalToken(Action disposeCallback)
        {
            _disposeCallback = disposeCallback;
        }

        public void Dispose()
        {
            try
            {
                _disposeCallback();
            }
            catch { }
        }
    }
}
