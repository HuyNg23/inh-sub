using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(Obj_Schema.FieldName), nameof(Obj_Schema.ObjID), IsUnique = true)]
    public class Obj_Schema : BaseTable
#pragma warning restore S101 // Types should be named in PascalCase
    {
        private string? objID;
        private string? fieldName;
        private FieldType? type;
        private string? objectReferent;

        [Required]
        public string? ObjID { get => objID; set => objID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? FieldName { get => fieldName; set => fieldName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public FieldType? Type { get => type; set => type = value; }
        public string? ObjectReferent { get => objectReferent; set => objectReferent = string.IsNullOrEmpty(value) ? "" : value.Trim(); }
    }

    public enum FieldType
    {
        Nvarchar,
        Integer,
        Decimal,
        Datetime,
        Float,
        Object,
        ListObject,
        ListPrimitiveTypesString,
        ListPrimitiveTypesNumber
    }
}