using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Desktop.Core.Services;
using ScreenR.Shared.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Core
{
    public static class CommandBuilder
    {
        public static RootCommand BuildRootCommand(string[] args, Action<IServiceCollection>? configureServices)
        {

            var rootCommand = new RootCommand("Provides remote control by streaming the desktop and input through a ScreenR server.");

            var startCommand = new Command("start", "Start a new remote control session.");

            var serverOption = new Option<Uri>(
                new[] { "-s", "--server-url" },
                "The server URL to which to connect (e.g. https://myserver.example.com).")
            {
                IsRequired = true
            };

            var sessionOption = new Option<Guid>(
                new[] { "-i", "--id" },
                () => Guid.NewGuid(),
                "The session ID to use for the connection.  The viewer will use this ID when connecting.");

            var passphraseOption = new Option<string>(
                new[] { "-p", "--passphrase" },
                () => string.Empty,
                "An optional passphrase that the viewer must use when connecting.");

            var timeoutOption = new Option<int>(
                new[] { "-t", "--timeout" },
                () => -1,
                "The amount of seconds to wait for a viewer connection before shutting down.  " +
                "If unspecified, it will wait until manually closed.");

            startCommand.AddOption(sessionOption);
            startCommand.AddOption(passphraseOption);
            startCommand.AddOption(timeoutOption);
            startCommand.AddOption(serverOption);
            rootCommand.AddCommand(startCommand);

            startCommand.SetHandler(async (Uri serverUrl, Guid sessionId, string passphrase, int timeout) =>
            {
                using var host = Host.CreateDefaultBuilder(args)
                    .UseConsoleLifetime()
                    .ConfigureServices(services =>
                    {
                        var appState = new AppState(serverUrl, sessionId, passphrase, timeout);
                        services.AddHostedService<DesktopHubConnection>();
                        services.AddSingleton<IAppState>(appState);
                        services.AddSingleton<IHubConnectionBuilder, HubConnectionBuilder>();
                        configureServices?.Invoke(services);
                    })
                    .ConfigureLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.AddDebug();
                        builder.AddProvider(new FileLoggerProvider("ScreenR_Desktop"));
                        builder.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
                    })
                    .Build();

                await host.RunAsync();
            }, serverOption, sessionOption, passphraseOption, timeoutOption);

            return rootCommand;
        }
    }
}
