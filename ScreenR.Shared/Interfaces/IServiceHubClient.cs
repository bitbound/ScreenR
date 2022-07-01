using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenR.Shared.Interfaces
{
    public interface IServiceHubClient
    {
        Task RequestDesktopStream(Guid sessionId, string requesterConnectionId);
        Task RequestWindowsSessions(Guid requestId, string requesterConnectionId);
    }

}
