using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class S_Service : BaseTable
    {
        private string? name;
        private string? site;
        private string? port;
        private string? subDomain;
        private TypeProtocol typeProtocol;
        private TypeService typeService;
        private string? level;
        private bool isOnline;

        [Description("Tên service")]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("IP server triển khai service")]
        public string? Site { get => site; set => site = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Cổng triển khai service")]
        public string? Port { get => port; set => port = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("SubDomain cho service, chỉ sử dụng khi nhiều service triển khai chung một cổng")]
        public string? SubDomain { get => subDomain; set => subDomain = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Kiểu phương thức, http hoặc https")]
        [DefaultValue(0)]
        public TypeProtocol TypeProtocol { get => typeProtocol; set => typeProtocol = value; }

        [Description("Kiểu service, mapping với các dịch vụ đang có của IBox")]
        [DefaultValue(0)]
        public TypeService TypeService { get => typeService; set => typeService = value; }

        [Description("Cấp độ ưu tiên của service")]
        [MaxLength(50)]
        [DefaultValue("0")]
        public string? Level { get => level; set => level = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Service đang trực tuyến hoặc không, mặc định khi khởi tạo là trực tuyến")]
        [DefaultValue(true)]
        public bool IsOnline { get => isOnline; set => isOnline = value; }
    }

    public enum TypeProtocol
    {
        Http,
        Https
    }

    public enum TypeService
    {
        RootBE,
        ClientBE,
        RestService,
        LogService,
        ScheduleService,
        StoreService,
        ChatBot
    }
}