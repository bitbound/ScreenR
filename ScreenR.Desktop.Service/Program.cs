using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScreenR.Desktop.Service.Interfaces;
using ScreenR.Desktop.Service.Services;
using ScreenR.Desktop.Service.Services.StartupActions;
using ScreenR.Desktop.Shared.Services;
using ScreenR.Shared.Services;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

var rootCommand = new RootCommand("Provides remote control by streaming the desktop and input through a ScreenR server.");

var installCommand = new Command("install", "Install the ScreenR service.");

var unInstallCommand = new Command("uninstall", "Uninstall the ScreenR service.");

var runCommand = new Command("run", "Run the ScreenR service.");

var serverOption = new Option<Uri>(
    new[] { "-s", "--server-url" },
    "The server URL to which to connect (e.g. https://myserver.example.com).")
{
    IsRequired = true
};


var deviceOption = new Option<Guid>(
    new[] { "-d", "--device-id" },
    "A unique identifier to use for this device.");
deviceOption.IsRequired = true;

installCommand.AddOption(serverOption);
installCommand.AddOption(deviceOption);
runCommand.AddOption(serverOption);
runCommand.AddOption(deviceOption);
rootCommand.AddCommand(installCommand);
rootCommand.AddCommand(runCommand);
rootCommand.AddCommand(unInstallCommand);

installCommand.SetHandler(async (Uri serverUrl, Guid deviceId) =>
{
    using var host = BuildHost<Install>(new (serverUrl, deviceId));
    await host.RunAsync();
}, serverOption, deviceOption);

runCommand.SetHandler(async (Uri serverUrl, Guid deviceId) =>
{
    using var host = BuildHost<Run>(new(serverUrl, deviceId));
    await host.RunAsync();
}, serverOption, deviceOption);

unInstallCommand.SetHandler(async () =>
{
    using var host = BuildHost<Uninstall>();
    await host.RunAsync();
});

return await rootCommand.InvokeAsync(args);


IHost BuildHost<TStartupAction>(AppState? appState = null)
    where TStartupAction : class, IHostedService
{
    var host = Host.CreateDefaultBuilder(args);

    if (Environment.UserInteractive)
    {
        host.UseConsoleLifetime();
    }
    else if (OperatingSystem.IsWindows())
    {
        host.UseWindowsService();
    }
    else if (OperatingSystem.IsLinux())
    {
        host.UseSystemd();
    }

    return host
        .ConfigureServices(services =>
        {
            services.AddHostedService<TStartupAction>();
            services.AddSingleton<IDeviceCreator, DeviceCreator>();
            services.AddSingleton<IProcessLauncher, ProcessLauncher>();
            services.AddSingleton<IServiceHubConnection, ServiceHubConnection>();
            services.AddSingleton<IHubConnectionBuilderFactory, HubConnectionBuilderFactory>();

            if (appState is not null)
            {
                services.AddSingleton<IAppState>(appState);
            }

            if (OperatingSystem.IsWindows())
            {
                services.AddScoped<IInstallerService, InstallerServiceWindows>();
            }
            else if (OperatingSystem.IsLinux())
            {
                services.AddScoped<IInstallerService, InstallerServiceLinux>();
            }
            else
            {
                throw new PlatformNotSupportedException("Only Windows and Linux are supported.");
            }
        })
        .ConfigureLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider("ScreenR_Service"));
            builder.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        })
        .Build();
}