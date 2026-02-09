using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(Obj.Name), nameof(Obj.DatabaseID), IsUnique = true)]
    public class Obj : BaseTable
    {
        private string? name;
        private string? databaseID;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? DatabaseID { get => databaseID; set => databaseID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }
}