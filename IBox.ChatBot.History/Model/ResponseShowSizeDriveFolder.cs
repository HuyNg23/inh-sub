namespace IBox.ChatBot.History.Model
{
    public class ResponseShowSizeDriveFolder
    {
        public long CapacityDriver { get; set; } = 0;
        public long CapacityFolder { get; set; } = 0;
    }

    public class ResponseSizeFolder
    {
        public string? ThisSite { get; set; }
        public long TotalSizeDisk { get; set; }
        public List<ResponseSizeFolderTenant>? SizeTenant { get; set; }
    }

    public class ResponseSizeFolderTenant
    {
        public string? TenantId { get; set; }
        public string? TenantName { get; set; }
        public long TotalSizeFolder { get; set; }
    }
}