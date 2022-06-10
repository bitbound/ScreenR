using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScreenR.Desktop.Core;
using ScreenR.Desktop.Core.Interfaces;
using ScreenR.Desktop.Core.Services;
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
});

return await rootCommand.InvokeAsync(args);