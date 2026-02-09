namespace IBox.CiscoAdapter.Models
{
    public class WebexEventModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string OrgId { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public string ActorId { get; set; } = string.Empty;
        public Dictionary<string, object>? Data { get; set; }
    }

    public class WebexCallEventData
    {
        public string CallId { get; set; } = string.Empty;
        public string CallerId { get; set; } = string.Empty;
        public string CalleeId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}
