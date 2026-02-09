using IBox.Database.Root.Tables;

namespace IBox.Database.Tenant.Tables
{
    public class SQLite_HistoryMonth : BaseTable
    {
        public string? Date { get; set; }
        public string? DateMonth { get; set; }
        public int CountSuccess { get; set; } = 0;
        public int CountFail { get; set; } = 0;
        public string? SiteRun { get; set; } = "";
        public string? Site { get; set; } = "";
        public TypeHistory? Type { get; set; }
        public DateTime? DateReal { get; set; } = DateTime.Now.AddDays(-1);
    }
}