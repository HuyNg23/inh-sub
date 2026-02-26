using System.ComponentModel.DataAnnotations;

namespace IBox.Database.SQLiteDBChatDay.TableDBHistory
{
    public class H_ApiThirdPartyExecuteHistory : BaseTableSQLite
    {
        private string? wfid;
        private string? reason;
        private string? keyExecuteRunApiThirdParty;
        private string? status;
        private string? statusCode;
        private string? url;
        private string? headers;
        private string? request;
        private string? response;
        private string? siteRun;
        private string? stepName;
        private string? stepId;

        [MaxLength(256)]
        public string? WfId { get => wfid; set => wfid = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? WfName { get; set; }

        public string? Reason { get => reason; set => reason = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? KeyExecuteRunApiThirdParty { get => keyExecuteRunApiThirdParty; set => keyExecuteRunApiThirdParty = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Status { get => status; set => status = value; }

        [MaxLength(256)]
        public string? StatusCode { get => statusCode; set => statusCode = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(2048)]
        public string? Url { get => url; set => url = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Headers { get => headers; set => headers = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Request { get => request; set => request = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Response { get => response; set => response = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? SiteRun { get => siteRun; set => siteRun = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? StepName { get => stepName; set => stepName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(256)]
        public string? StepId { get => stepId; set => stepId = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
        public int Hour { get; set; }
    }

    public enum ExecuteRunApiThirdPartyStatus
    {
        Running,
        Complete,
        Fail
    }
}