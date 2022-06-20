using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Control.Services
{
    public interface IAppState
    {
        string Passphrase { get; }
        Guid DesktopId { get; }
        Uri ServerUrl { get; }
        int TimeoutSeconds { get; }
    }

    public class AppState : IAppState
    {
        public AppState(Uri serverUrl, Guid desktopId, string passphrase, int timeout)
        {
            ServerUrl = serverUrl;
            DesktopId = desktopId;
            Passphrase = passphrase;
            TimeoutSeconds = timeout;
        }

        public string Passphrase { get; } = string.Empty;
        public Uri ServerUrl { get; }
        public Guid DesktopId { get; } = Guid.NewGuid();

        public int TimeoutSeconds { get; } = -1;
    }
}
