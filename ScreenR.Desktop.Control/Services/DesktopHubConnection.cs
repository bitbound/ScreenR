﻿using MessagePack;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Control.Interfaces;
using ScreenR.Desktop.Control.Native.Windows;
using ScreenR.Desktop.Shared.Native.Windows;
using ScreenR.Desktop.Shared.Services;
using ScreenR.Shared.Dtos;
using ScreenR.Shared.Enums;
using ScreenR.Shared.Extensions;
using ScreenR.Shared.Helpers;
using ScreenR.Shared.Interfaces;
using ScreenR.Shared.Models;
using ScreenR.Shared.Services;
using System.Collections.Concurrent;

namespace ScreenR.Desktop.Control.Services
{
    internal interface IDesktopHubConnection
    {
        Task Connect();
    }

    internal class DesktopHubConnection : IDesktopHubConnection
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IAppState _appState;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly HubConnection _connection;
        private readonly IDeviceCreator _deviceCreator;
        private readonly ILogger<DesktopHubConnection> _logger;
        private readonly ConcurrentDictionary<StreamToken, IViewerSession> _streamingSessions = new();
        private DesktopDevice _deviceInfo;

        public DesktopHubConnection(
            IHostApplicationLifetime appLifetime,
            IAppState appState,
            IServiceScopeFactory scopeFactory,
            IHubConnectionBuilderFactory builderFactory,
            IDeviceCreator deviceCreator,
            ILogger<DesktopHubConnection> logger)
        {
            _appLifetime = appLifetime;
            _appState = appState;
            _scopeFactory = scopeFactory;
            _deviceCreator = deviceCreator;
            _logger = logger;
            _deviceInfo = _deviceCreator.CreateDesktop(appState.DesktopId, true);

            _connection = builderFactory.CreateBuilder()
                .WithUrl($"{appState.ServerUrl.Trim()}/desktop-hub")
                .AddMessagePackProtocol()
                .WithAutomaticReconnect(new RetryPolicy())
                .Build();
        }

        public async Task Connect()
        {
            _connection.Reconnecting += HubConnection_Reconnecting;
            _connection.Reconnected += HubConnection_Reconnected;

            _connection.On<StreamToken, string>(nameof(IDesktopHubClient.StartDesktopStream), StartDesktopStream);
            _connection.On<Guid, string>(nameof(IDesktopHubClient.RequestWindowsSessions), RequestWindowsSessions);
            _connection.On<Guid, string>(nameof(IDesktopHubClient.GetDisplays), GetDisplays);
            _connection.On<StreamToken>(nameof(IDesktopHubClient.FrameReceived), FrameReceived);

            while (!_appLifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Connecting to server.");

                    await _connection.StartAsync(_appLifetime.ApplicationStopping);

                    _logger.LogInformation("Connected to server.");

                    await _connection.SendAsync("SetDeviceInfo", _deviceInfo, cancellationToken: _appLifetime.ApplicationStopping);

                    _logger.LogInformation("Created session with desktop process ID: {desktopId}", _appState.DesktopId);
                    break;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning("Failed to connect to server.  Status Code: {code}", ex.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in desktop hub connection.");
                }
                await Task.Delay(3_000, _appLifetime.ApplicationStopping);
            }
        }

        private void FrameReceived(StreamToken token)
        {
            if (_streamingSessions.TryGetValue(token, out var session))
            {
                session.FrameReceived();
            }
        }

        private async Task HubConnection_Reconnected(string? arg)
        {
            _deviceInfo = _deviceCreator.CreateDesktop(_appState.DesktopId, true);
            await _connection.InvokeAsync("SetDeviceInfo", _deviceInfo);
            _logger.LogInformation("Reconnected to desktop hub.");
        }

        private Task HubConnection_Reconnecting(Exception? arg)
        {
            _logger.LogWarning(arg, "Reconnecting to desktop hub.");
            return Task.CompletedTask;
        }

        private async void GetDisplays(Guid requestId, string requesterConnectionId)
        {
            switch (EnvironmentHelper.Platform)
            {
                case Platform.Windows:
                    {
                        var displays = DisplaysEnumerationHelper
                            .GetDisplays()
                            .Select(x => new DisplayDto()
                            {
                                Top = x.MonitorArea.Top,
                                Right = x.MonitorArea.Right,
                                Bottom = x.MonitorArea.Bottom,
                                Left = x.MonitorArea.Left,
                                DeviceName = x.DeviceName,
                                IsPrimary = x.IsPrimary
                            });
                        await SendDtoToUser(displays, DtoType.DisplayList, requestId, requesterConnectionId);
                        break;
                    }
                case Platform.Linux:
                    break;
                case Platform.Unknown:
                case Platform.MacOS:
                case Platform.MacCatalyst:
                case Platform.Browser:
                default:
                    _logger.LogError("Platform not supported.");
                    await _connection.InvokeAsync("SendToast", "Platform not supported.", MessageLevel.Warning, requesterConnectionId);
                    break;
            }
        }

        private async void RequestWindowsSessions(Guid requestId, string requesterConnectionId)
        {
            if (EnvironmentHelper.Platform != Platform.Windows)
            {
                _logger.LogWarning("Received request for Windows sessions, but platform is {platform}.", EnvironmentHelper.Platform);
                return;
            }

            var sessions = Win32Interop.GetActiveSessions();
            await SendDtoToUser(sessions, DtoType.WindowsSessions, requestId, requesterConnectionId);
        }

        private async Task SendDtoToUser<T>(T dto, DtoType dtoType, Guid requestId, string requesterConnectionId)
        {
            foreach (var wrapper in DtoChunker.ChunkDto(dto, dtoType, requestId))
            {
                await _connection.InvokeAsync("SendDtoToUser", wrapper, requesterConnectionId);
            }
        }

        private async Task StartDesktopStream(StreamToken streamToken, string passphrase)
        {
            if (passphrase != _appState.Passphrase)
            {
                _logger.LogWarning("Invalid passphrase supplied: {passphrase}", passphrase);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var session = scope.ServiceProvider.GetRequiredService<IViewerSession>();
            _streamingSessions.AddOrUpdate(streamToken, session, (k, v) =>
            {
                v.Stop();
                return session;
            });

            _logger.LogInformation("Starting stream for session {sessionId}, request {requestId}.", 
                streamToken.SessionId, 
                streamToken.RequestId);

            await _connection.SendAsync("SendDesktopStream", streamToken, session.GetDesktopStream());
        }
        private class RetryPolicy : IRetryPolicy
        {
            public TimeSpan? NextRetryDelay(RetryContext retryContext)
            {
                return TimeSpan.FromSeconds(3);
            }
        }
    }
}
