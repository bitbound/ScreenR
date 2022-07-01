using ScreenR.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Interfaces
{
    public interface IDesktopHubClient
    {
        Task FrameReceived(StreamToken streamToken);

        Task GetDisplays(Guid requestId, string requesterConnectionId);

        Task RequestWindowsSessions(Guid requestId, string requesterConnectionId);

        Task StartDesktopStream(StreamToken streamToken, string passphrase);
    }

}
