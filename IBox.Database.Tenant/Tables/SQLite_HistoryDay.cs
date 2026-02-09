using IBox.Database.Root.Tables;

namespace IBox.Database.Tenant.Tables
{
    public class SQLite_HistoryDay : BaseTable
    {
        public int Hour { get; set; } = 0;
        public int CountSuccess { get; set; } = 0;
        public int CountFail { get; set; } = 0;
        public string? SiteRun { get; set; } = "";
        public TypeHistory? Type { get; set; }
        public string? Date { get; set; } = "";
        public string? Site { get; set; } = "";
    }

    public enum TypeHistory
    {
        WF = 0,
        API = 1,
        SQL = 2,
    }
}