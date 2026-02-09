using IBox.Common.Objects;
using IBox.Database.Tenant.Tables;
using System.Text.Json.Serialization;

namespace IBox.Workflow.Model
{
    public class WFStep : IBoxDisposable
    {
        public string? Id { get; set; }
        public string? Description { get; set; }
        public string? WfId { get; set; }
        public List<WFStep>? ChildSteps { get; set; }
        public WF_Type? Type { get; set; }
        public bool? Debug { get; set; }
        public bool? IsDelete { get; set; }
        public float? PositionX { get; set; }
        public float? PositionY { get; set; }
        public string? Param { get; set; }
        public bool? IsExceptionStep { get; set; } = false;
        public string? Response { get; set; }
        public string? DllID { get; set; }
        public string? FunctionName { get; set; }
        public string? Config { get; set; }
        public bool? SaveResponseToCache { get; set; }
        public DynamicLinkedLibrary? DynamicLinkedLibrary { get; set; }
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && this.ChildSteps != null)
                {
                    this.ChildSteps.ForEach(ptr => ptr.Dispose());
                    this.ChildSteps.Clear();
                }

                // Dispose unmanaged resources (if any)
                // No unmanaged resources in this example, but you can add them here

                disposed = true;
            }

            base.Dispose(disposing);
        }

        ~WFStep()
        {
            Dispose(false);
        }
    }

    public class WFEdge : IBoxDisposable
    {
        public string? Id { get; set; }
        public string? Source { get; set; }
        public string? Target { get; set; }
        public string? SourceHandle { get; set; }
    }
}