using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class W_EventSocket : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        private string eventName = string.Empty;
        private string workflowId = string.Empty;
        private string addressSocket = string.Empty;
        private string webSocketId = string.Empty;

        [Required()]
        public string EventName { get => eventName; set => eventName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }



        [Required()]
        public string WorkflowId { get => workflowId; set => workflowId = string.IsNullOrEmpty(value) ? "" : value.Trim(); }



        public string AddressSocket { get => addressSocket; set => addressSocket = string.IsNullOrEmpty(value) ? "" : value.Trim(); }


        [Required()]
        public string WebSocketId { get => webSocketId; set => webSocketId = string.IsNullOrEmpty(value) ? "" : value.Trim(); }


    }
}