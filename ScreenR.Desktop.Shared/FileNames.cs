using ScreenR.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared
{
    public static class FileNames
    {
        public static string RemoteControl
        {
            get
            {
                return EnvironmentHelper.Platform switch
                {
                    ScreenR.Shared.Enums.Platform.Windows => "ScreenR.exe",
                    ScreenR.Shared.Enums.Platform.Linux => "screenr",
                    ScreenR.Shared.Enums.Platform.MacOS => throw new PlatformNotSupportedException(),
                    ScreenR.Shared.Enums.Platform.MacCatalyst => throw new PlatformNotSupportedException(),
                    _ => throw new PlatformNotSupportedException(),
                };
            }
        }

        public static string Service
        {
            get
            {
                return EnvironmentHelper.Platform switch
                {
                    ScreenR.Shared.Enums.Platform.Windows => "ScreenR_Service.exe",
                    ScreenR.Shared.Enums.Platform.Linux => "screenr-service",
                    ScreenR.Shared.Enums.Platform.MacOS => throw new PlatformNotSupportedException(),
                    ScreenR.Shared.Enums.Platform.MacCatalyst => throw new PlatformNotSupportedException(),
                    _ => throw new PlatformNotSupportedException(),
                };
            }
        }
    }
}
