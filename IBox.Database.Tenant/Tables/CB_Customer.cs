using IBox.Database.Root.Tables;

namespace IBox.Database.Tenant.Tables
{
    public class CB_Customer : BaseTable
    {
        public string SenderId { get; set; } = "";
        public string? CustomerInfo { get; set; } = "";
        public string? cif_list_data { get; set; } = "";
        public string? Phone { get; set; } = "";
        public string? ContactId { get; set; } = "";
    }
}