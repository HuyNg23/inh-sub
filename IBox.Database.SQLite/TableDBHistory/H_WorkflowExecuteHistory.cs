using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteDBChatDay.TableDBHistory
{
    public class H_WorkflowExecuteHistory : BaseTableSQLite
    {
        private string? wfid;
        private string? status;
        private string? reason;
        private string? keyExecuteRunWorkFlow;
        private string? siteRun;

        [MaxLength(256)]
        public string? WfId { get => wfid; set => wfid = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Status { get => status; set => status = value; }
        public string? Reason { get => reason; set => reason = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? KeyExecuteRunWorkFlow { get => keyExecuteRunWorkFlow; set => keyExecuteRunWorkFlow = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? SiteRun { get => siteRun; set => siteRun = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Header { get; set; }
        public string? RequestWF { get; set; }
        public string? ResultWF { get; set; }
        [MaxLength(256)]
        public string? WfName { get; set; }
        public int Hour { get; set; }
    }

    public enum WorkflowExecuteStatus
    {
        Running,
        Complete,
        Fail,
    }
}