using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Services
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _appName;

        public FileLoggerProvider(string appName)
        {
            _appName = appName;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(_appName, categoryName);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
