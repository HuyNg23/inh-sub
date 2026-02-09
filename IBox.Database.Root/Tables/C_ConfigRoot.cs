using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Root.Tables
{
    public class C_ConfigRoot : BaseTable
    {
        private string? nameConfig;
        private string? valueConfig;
        private string? typeConfig;

        [Description("Tên config")]
        public string? NameConfig { get => nameConfig; set => nameConfig = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Description("Value config dạng json")]
        public string? ValueConfig { get => valueConfig; set => valueConfig = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        [Description("Type config xác định thuộc loại cấu hình nào (0 mail alert root, 1 DFS)")]
        public string? TypeConfig { get => typeConfig; set => typeConfig = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}