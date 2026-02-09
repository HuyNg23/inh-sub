using IBox.Database.Root.Tables;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
#pragma warning disable S101 // Types should be named in PascalCase

    public class WF_Step_Edge : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        [Required, MaxLength(64)]
        public string? WFid { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string? Source { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string? Target { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string? SourceHandle { get; set; } = string.Empty;
    }
}