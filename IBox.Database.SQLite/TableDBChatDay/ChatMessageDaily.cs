using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteDBChatDay
{
    public class ChatMessageDaily
    {
        [Key]
        [MaxLength(60)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? SessionId { get; set; } = "";
        public string MessageContent { get; set; } = "";
        public bool? IsInputIC { get; set; } = false;
        public bool? IsInputCDP { get; set; } = false;
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
    }
}