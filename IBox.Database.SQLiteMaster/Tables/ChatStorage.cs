using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteMaster.Tables
{
    public class ChatStorage
    {
        [Key]
        [MaxLength(256)]
        public string? Id { get; set; } = Guid.NewGuid().ToString();
        public string? SenderId { get; set; } = "";
        public string? StoragePath { get; set; } = "";
        public StateChat State { get; set; } = StateChat.Active;
        public bool? IsSupport { get; set; } = false;
        public string? ParentChatStorage { get; set; } = "";
    }
    public enum StateChat
    {
        Active,
        Close
    }
}
