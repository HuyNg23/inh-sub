using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteDBChatDay
{
    public class ChatSessionDaily
    {
        [Key]
        [MaxLength(60)]
        public string SessionId { get; set; } = Guid.NewGuid().ToString();

        public string SenderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public bool IsClose { get; set; } = false;
        public string? InteractionCRM { get; set; } = "";
        public string? ContactId { get; set; } = "";
        public string? Channel { get; set; } = "";
        public string? SubChannel { get; set; } = "";
        public bool IsSupport { get; set; } = false;
        public string? Tag { get; set; } = "";
        public string? CustomerId { get; set; } = "";
        public string? IdIc { get; set; } = "";
        public string? IsSiteClose { get; set; } = "";
        public DateTime? DateMessageLast { get; set; } = DateTime.Now;
    }
}