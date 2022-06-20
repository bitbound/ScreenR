using ScreenR.Desktop.Shared.Dtos;
using ScreenR.Desktop.Shared.Models;

namespace ScreenR.Web.Server.Models
{
    internal class StreamingSession
    {
        public StreamingSession(StreamToken streamToken)
        {
            StreamToken = streamToken;
        }

        public SemaphoreSlim ReadySignal { get; } = new(0, 1);
        public SemaphoreSlim EndSignal { get; } = new(0, 1);
        public StreamToken StreamToken { get; init; }
        public IAsyncEnumerable<DesktopFrameChunk>? Stream { get; set; }
    }
}
