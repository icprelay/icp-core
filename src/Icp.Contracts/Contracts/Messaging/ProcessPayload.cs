namespace Icp.Contracts.Contracts.Messaging
{
    public class ProcessPayload
    {
        public Guid instanceId { get; set; }
        public string? kind { get; set; } = string.Empty;
        public string? metaType { get; set; } = string.Empty;
        public string? payloadRef { get; set; } = string.Empty;
    }
}
