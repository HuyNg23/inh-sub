using System.ComponentModel;

namespace IBox.Database.Root.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class S_ServerScheduleStatus : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        private string? name;
        private string? site;
        private bool? status;

        [Description("Tên server")]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Địa chỉ IP của server")]
        public string? Site { get => site; set => site = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Status")]
        public bool? Status { get => status; set => status = value; }
    }
}