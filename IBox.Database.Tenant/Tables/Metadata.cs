using IBox.Database.Root.Tables;

namespace IBox.Database.Tenant.Tables
{
    public class Metadata : BaseTable
    {
        private string? key00;
        private string? value00;

        public string? Key00 { get => key00; set => key00 = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public string? Value00 { get => value00; set => value00 = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}