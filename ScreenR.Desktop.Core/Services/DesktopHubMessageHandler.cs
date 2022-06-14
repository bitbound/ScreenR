using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Core.Services
{
    internal class DesktopHubMessageHandler
    {
        private readonly IDesktopHubConnection _hubConnection;
        private readonly IAppState _appState;
        private readonly IDesktopStreamer _desktopStreamer;
        private readonly ILogger<DesktopHubMessageHandler> _logger;

        public DesktopHubMessageHandler(
            IDesktopHubConnection hubConnection, 
            IAppState appState,
            IDesktopStreamer desktopStreamer,
            ILogger<DesktopHubMessageHandler> logger)
        {
            _hubConnection = hubConnection;
            _appState = appState;
            _desktopStreamer = desktopStreamer;
            _logger = logger;

            _hubConnection.Connection.On<StreamToken, string>("StartDesktopStream", StartDesktopStream);
        }

        private async Task StartDesktopStream(StreamToken streamToken, string passphrase)
        {
            if (passphrase != _appState.Passphrase)
            {
                _logger.LogWarning("Invalid passphrase supplied: {passphrase}", passphrase);
                return;
            }

            // TODO: Cancellation token.
            // TODO: Throttle sender.
            await _hubConnection.Connection.SendAsync("SendDesktopStream", streamToken, _desktopStreamer.GetDesktopStream());
        }
    }
}
