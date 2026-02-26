namespace IBox.Database.SQLiteDBChatDay.TableDBChatDay
{
    public class ViewChatMessageDaily
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? SessionId { get; set; } = "";
        public string? MessageContent { get; set; } = "";
        public bool? IsInputIC { get; set; } = false;
    }
}