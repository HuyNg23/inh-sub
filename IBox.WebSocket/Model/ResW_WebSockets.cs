using IBox.Database.Tenant.Tables;

namespace IBox.WebSocket.Model
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class ResW_WebSockets
#pragma warning restore S101 // Types should be named in PascalCase
    {
        public string? Domain { get; set; }
        public string? Site { get; set; }
        public string? Name { get; set; }
        public int Port { get; set; }
        public string? Id { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsDelete { get; set; }
        public DateTime? ModificationDate { get; set; }
        public List<W_EventSocket>? ListEventSocket { get; set; }
    }
}