namespace ScreenR.Web.Client.Models
{
    public class RemoteSession
    {
        public RemoteSession(Guid sessionId, string deviceName, Guid requestId)
        {
            SessionId = sessionId;
            DeviceName = deviceName;
            RequestId = requestId;
        }

        public Guid SessionId { get; init; }
        public string DeviceName { get; init; } = string.Empty;
        public Guid RequestId { get; init; }
    }
}
