namespace IBox.Common.Objects
{
    public class LogInfo
    {
        public string? Location { get; set; }
        public string? Name { get; set; }
        public long? Size { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public enum IboxService
    {
        RootServersIBox,
        ServersIBox,
        RestServiceIBox,
        ScheduleServiceIBox,
        LogService,
        ChatBotService,
        StoreService
    }
}