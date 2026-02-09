using IBox.Common.Objects;
using IBox.Common.TCP;

namespace IBox.Workflow.Model
{
    public class WFDefine : IBoxDisposable
    {
        private string tenantId = string.Empty;
        private string id = string.Empty;
        private string name = string.Empty;
        private string description = string.Empty;
        private List<WFStep> wFs = new List<WFStep>();
        private List<WFEdge> edges = new List<WFEdge>();
        private DateTime createdDate = DateTime.Now;
        private DateTime modificationDate = DateTime.Now;
        private AuthorType authenType = 0;
        private string userName = string.Empty;
        private string password = string.Empty;
        private DateTime deployDate = DateTime.Now;
        private string source = string.Empty;
        private bool isLock = false;
        private DateTime dateToUpdate = DateTime.Now;

        public string Id { get => id; set => id = value; }
        public string Name { get => name; set => name = value; }
        public string Description { get => description; set => description = value; }
        public List<WFStep> WFstep { get => wFs; set => wFs = value; }
        public List<WFEdge> Edges { get => edges; set => edges = value; }
        public DateTime CreatedDate { get => createdDate; set => createdDate = value; }
        public DateTime ModificationDate { get => modificationDate; set => modificationDate = value; }
        public AuthorType AuthenType { get => authenType; set => authenType = value; }
        public string UserName { get => userName; set => userName = value; }
        public string Password { get => password; set => password = value; }
        public DateTime DeployDate { get => deployDate; set => deployDate = value; }
        public string Source { get => source; set => source = value; }
        public bool IsLock { get => isLock; set => isLock = value; }
        public DateTime DateToUpdate { get => dateToUpdate; set => dateToUpdate = value; }
        public string TenantId { get => tenantId; set => tenantId = value; }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    this.WFstep.ForEach(ptr => ptr.Dispose());
                    this.WFstep.Clear();
                }

                // Dispose unmanaged resources (if any)
                // No unmanaged resources in this example, but you can add them here

                disposed = true;
            }

            base.Dispose(disposing);
        }

        // Finalizer
        ~WFDefine()
        {
            Dispose(false);
        }
    }
}