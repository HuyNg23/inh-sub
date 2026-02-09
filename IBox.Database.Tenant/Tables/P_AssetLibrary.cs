using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class P_AssetLibrary: BaseTable
    {
        [Required, MaxLength(450)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(450)]
        public string Url { get; set; } = string.Empty;

        [Required, MaxLength(450)]
        public string Version {  get; set; } = string.Empty;

        [Required, MaxLength(450)]
        public string Tags { get; set; } = string.Empty;
    }
}
