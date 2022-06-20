using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScreenR.Desktop.Control;
using ScreenR.Desktop.Control.Interfaces;
using ScreenR.Desktop.Control.Services;
using ScreenR.Desktop.Windows.Capture;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

var rootCommand = CommandBuilder.BuildRootCommand(args, services =>
{
    services.AddTransient<IScreenGrabber, ScreenGrabber>();
    services.AddTransient<IDesktopStreamer, DesktopStreamer>();
});

return await rootCommand.InvokeAsync(args);