using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

var rootCommand = new RootCommand("Provides remote control by streaming the desktop and input through a ScreenR server.");

var installCommand = new Command("install", "Install the ScreenR service.");

var runCommand = new Command("run", "Run the ScreenR service.");

var serverOption = new Option<Uri>(
    new[] { "-s", "--server-url" },
    "The server URL to which to connect (e.g. https://myserver.example.com).");
serverOption.IsRequired = true;

installCommand.AddOption(serverOption);
rootCommand.AddCommand(installCommand);
runCommand.AddOption(serverOption);
rootCommand.AddCommand(runCommand);

installCommand.SetHandler(async (Uri serverUrl) =>
{
    using var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSystemd()
        .UseConsoleLifetime()
        .ConfigureServices(services =>
        {
            
        })
        .ConfigureLogging(builder =>
        {

        })
        .Build();

    await host.RunAsync();
}, serverOption);

return await rootCommand.InvokeAsync(args);