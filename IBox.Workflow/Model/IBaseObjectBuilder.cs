using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;

namespace IBox.Workflow.Model
{
    public interface IBaseObjectBuilder : ITenantContext<BaseObjectBuilder>
    {
        Type CreateNewObject(string rootObjectID, string tenantId, IBContext<TenantContext> tenantContext);

        List<KeyValuePair<string, string>> GetObjectzInBD(string rootObjectID);
    }
}