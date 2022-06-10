using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Core.Services
{
    public interface IAppState
    {
        string Passphrase { get; }
        Guid SessionId { get; }
        Uri ServerUrl { get; }
        int TimeoutSeconds { get; }
    }

    public class AppState : IAppState
    {
        public AppState(Uri serverUrl, Guid sessionId, string passphrase, int timeout)
        {
            ServerUrl = serverUrl;
            SessionId = sessionId;
            Passphrase = passphrase;
            TimeoutSeconds = timeout;
        }

        public string Passphrase { get; } = string.Empty;
        public Uri ServerUrl { get; }
        public Guid SessionId { get; } = Guid.NewGuid();

        public int TimeoutSeconds { get; } = -1;
    }
}
