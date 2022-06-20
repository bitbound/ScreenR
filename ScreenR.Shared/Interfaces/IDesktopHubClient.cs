using ScreenR.Desktop.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Desktop.Shared.Interfaces
{
    public interface IDesktopHubClient
    {
        Task StartDesktopStream(StreamToken streamToken, string passphrase);
    }

}
