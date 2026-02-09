using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;

namespace IBox.Database.Tenant
{
    public abstract class ATenantContext<T> : IBoxDisposable, ITenantContext<T> where T : ATenantContext<T>
    {
        protected IBContext<TenantContext>? tenantContext { get; set; }

        protected virtual void ImplementationDIInLocalObject()
        {
            throw new NotImplementedException();
        }

        public virtual T SetTenantContext(string tenantID)
        {
            if (tenantContext == null)
            {
                throw new IboxLog("need to call init function in BBaseBusiness model firt", tenantID);
            }
            this.tenantContext = tenantContext.GetTenantContext(tenantID);
            return (T)this;
        }

        public virtual T SetTenantContext(IBContext<TenantContext> tenantContext)
        {
            this.tenantContext = tenantContext;
            return (T)this;
        }

        public virtual T SetTenantContext(IBContext<TenantContext> tenantContext, string tenantID)
        {
            this.tenantContext = tenantContext.GetTenantContext(tenantID);
            return (T)this;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && this.tenantContext != null)
                {
                    if (this.tenantContext.Context != null)
                    {
                        this.tenantContext.Context.Dispose();
                    }
                    this.tenantContext = null;
                }

                //if (disposing && this.tenantContext != null && this.tenantContext.Context != null)
                //{
                //    //this.tenantContext.Context.Database.CloseConnection();
                //}

                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}