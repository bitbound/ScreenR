namespace ScreenR.Web.Server.Models
{
    public class StreamingSession
    {
        public StreamingSession(Guid sessionId)
        {
            SessionId = sessionId;
        }

        public SemaphoreSlim ReadySignal { get; } = new(0, 1);
        public Guid SessionId { get; init; }
        public IAsyncEnumerable<byte>? Stream { get; set; }
    }
}
