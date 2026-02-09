using IBox.Database.Root;
using IBox.Database.Tenant;

namespace IBox.Common.Security
{
    public interface ITenantContext<T> : IDisposable where T : class
    {
        T SetTenantContext(string tenantID);

        T SetTenantContext(IBContext<TenantContext> tenantContext);

        T SetTenantContext(IBContext<TenantContext> tenantContext, string tenantID);
    }
}