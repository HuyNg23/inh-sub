using System.ComponentModel;

namespace IBox.Database.Root.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class S_ServerScheduleShrinkLogState : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        [Description("Tên server")]
        public string? Name { get; set; }

        [Description("Địa chỉ IP của server")]
        public string? Site { get; set; }

        [Description("Status")]
        public bool Status { get; set; }
    }
}