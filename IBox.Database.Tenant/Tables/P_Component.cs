using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class P_Component: BaseTable
    {
        private string? config;

        [Required, MaxLength(450)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1024)]
        public string PathJs {  get; set; } = string.Empty;

        [MaxLength(1024)]
        public string PathCss {  get; set; } = string.Empty;

        [MaxLength(1024)]
        public string PathLink {  get; set; } = string.Empty;

        [MaxLength(1024)]
        public string PathHtml {  get; set; } = string.Empty;
    }
}
