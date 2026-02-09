using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    public class P_ComponentInLayout: BaseTable
    {
        [Required, MaxLength(60)]
        public string ComponentId { get; set; } = string.Empty;
        [Required, MaxLength(60)]
        public string LayOutId { get; set; } = string.Empty;
        [Required]
        public float PositionX { get; set; } = 0;
        [Required]
        public float PositionY { get; set; } = 0;
        [Required]
        public int MinWidth { get; set; } = 0;
        [Required]
        public int MinHeight { get; set; } = 0;
    }
}
