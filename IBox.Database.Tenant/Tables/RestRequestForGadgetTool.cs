using IBox.Database.Root.Tables;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class RestRequestForGadgetTool : BaseTable
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;

        [Description("Id người tạo bản ghi")]
        [MaxLength(60)]
        public string CreatedById { get; set; } = string.Empty;
    }
}