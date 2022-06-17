using ScreenR.Shared.Reactive;
using ScreenR.Web.Client.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ScreenR.Web.Client.Services
{
    public interface IAppState
    {
        event PropertyChangedEventHandler? PropertyChanged;
    }

    public class AppState : ObservableObject, IAppState
    {
        public ObservableCollection<RemoteSession> RemoteSessions { get; } = new();
    }
}
