using IBox.Database.Root.Tables;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class DynamicLinkedLibrary : BaseTable
    {
        private string? name;
        private string? link;
        private string? config;
        private string? functionList;
        private string? groupId;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? Link { get => link; set => link = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? Config { get => config; set => config = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? FunctionList { get => functionList; set => functionList = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [DefaultValue("")]
        [MaxLength(60)]
        public string? GroupId { get => groupId; set => groupId = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}