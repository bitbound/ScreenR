using ScreenR.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Helpers
{
    public static class EnvironmentHelper
    {
        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public static Platform Platform
        {
            get
            {
                if (OperatingSystem.IsWindows())
                {
                    return Platform.Windows;
                }
                else if (OperatingSystem.IsLinux())
                {
                    return Platform.Linux;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    return Platform.MacOS;
                }
                else if (OperatingSystem.IsMacCatalyst())
                {
                    return Platform.MacOS;
                }
                else
                {
                    return Platform.Unknown;
                }
            }
        }
    }
}
