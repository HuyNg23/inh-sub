using IBox.Database.Root.Tables;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IBox.Database.Tenant.Tables
{
    [Index(nameof(WF_Step.Name), IsUnique = true)]
    public class WF_Step : BaseTable
    {
        private string? name;
        private string? description;
        private string? wFid;
        private WF_Type? type;
        private bool? debug;
        private string? parentStep;
        private bool? isExceptionStep;
        private string? param;
        private string? response;
        private float? positionX;
        private float? positionY;
        private string? dllID;
        private string? functionName;
        private string? config;
        private bool? saveResponseToCache;

        [Required]
        public string? Name { get => name; set => name = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Description { get => description; set => description = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(64), Required]
        public string? WFid { get => wFid; set => wFid = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public WF_Type? Type { get => type; set => type = value; }

        [Required]
        public bool? Debug { get => debug; set => debug = value; }

        [MaxLength(64), Required]
        public string? ParentStep { get => parentStep; set => parentStep = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public bool? IsExceptionStep { get => isExceptionStep; set => isExceptionStep = value; }

        [MaxLength(64), Required]
        public string? Param { get => param; set => param = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [MaxLength(64), Required]
        public string? Response { get => response; set => response = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public float? PositionX { get => positionX; set => positionX = value; }

        [Required]
        public float? PositionY { get => positionY; set => positionY = value; }

        [MaxLength(64), Required]
        public string? DllID { get => dllID; set => dllID = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        [Required]
        public string? FunctionName { get => functionName; set => functionName = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public string? Config { get => config; set => config = string.IsNullOrEmpty(value) ? "" : value.Trim(); }

        public bool? SaveResponseToCache { get => saveResponseToCache; set => saveResponseToCache = value; }
    }

    public enum WF_Type
    {
        Start,
        CallDLLxJsonOnly,
        CallAPIxJsonOnly,
        DumpResponse,
        Condition,
        SQLExecute,
        Delay,
        RecallWF,
        ReadArray,
        SendMail,
        BuildListObject,
        Formatting,        
        FindInList,
        InformixExecute,
        HttpStatusResponse,
        KafkaPush,
        CallXAPI,
        RunScript,
        ForwardFile
    }
}