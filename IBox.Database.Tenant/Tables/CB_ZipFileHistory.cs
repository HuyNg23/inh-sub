using IBox.Database.Root.Tables;

namespace IBox.Database.Tenant.Tables
{
    public class CB_ZipFileHistory : BaseTable
    {
        public string? NameFile { get; set; }
        public string? PathFile { get; set; }
        public string? ListFileZip { get; set; }
        public string? ThisSite { get; set; }
    }
}