using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Desktop.Windows.Capture;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

var rootCommand = new RootCommand("Provides remote control by streaming the desktop and input through a ScreenR server.");

var startCommand = new Command("start", "Start a new remote control session.");

var sessionOption = new Option<Guid>(
    new[] { "-s", "--session-id" },
    "The session ID to use for the connection.  The viewer will use this ID when connecting.");
sessionOption.IsRequired = true;

var passphraseOption = new Option<string>(
    new[] { "-p", "--passphrase" },
    "The passphrase that the viewer must use when connecting.");
passphraseOption.IsRequired = true;

var timeoutOption = new Option<int>(
    new[] { "-t", "--timeout" },
    () => 30,
    "The amount of seconds to wait for a viewer connection before shutting down.");


startCommand.AddOption(sessionOption);
startCommand.AddOption(passphraseOption);
startCommand.AddOption(timeoutOption);
rootCommand.AddCommand(startCommand);

startCommand.SetHandler(async (Guid sessionId, string passphrase, int timeout) =>
{
    using var host = Host.CreateDefaultBuilder(args)
        .UseConsoleLifetime()
        .ConfigureServices(services =>
        {
            services.AddScoped<IScreenGrabber, ScreenGrabber>();
        })
        .ConfigureLogging(builder =>
        {
            
        })
        .Build();

    await host.RunAsync();
}, sessionOption, passphraseOption, timeoutOption);

return await rootCommand.InvokeAsync(args);