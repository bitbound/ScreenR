namespace ScreenR.Web.Client.Models
{
    public class RemoteSession
    {
        public Guid SessionId { get; init; }
        public string DeviceName { get; init; } = string.Empty;
    }
}
