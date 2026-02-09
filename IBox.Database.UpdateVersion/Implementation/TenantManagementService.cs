using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Common.Objects;
using IBox.Database.UpdateVersion.Factories;

namespace IBox.Database.UpdateVersion.Implementation
{
    public class TenantManagementService : ITenantManagementService
    {
        private readonly IBContext<RootContext> _rootContext;
        private readonly IConfiguration _configuration;
        private readonly IEncryption _encryption;

        public TenantManagementService(
            IBContext<RootContext> rootContext,
            IConfiguration configuration,
            IEncryption encryption)
        {
            _rootContext = rootContext;
            _configuration = configuration;
            _encryption = encryption;
        }

        public void SynchronizeAllTenants()
        {
            // Giả sử RootContext có phương thức lấy danh sách tenant
            var tenants = _rootContext.Context.GetAllTenants(); // Cần implement trong RootContext

            foreach (var tenant in tenants)
            {
                try
                {
                    var tenantContext = new TenantContext(_configuration, _encryption, _rootContext)
                    {
                        TenantInfo = tenant
                    };

                    var synchronizer = new DatabaseSynchronizer<TenantContext>(tenantContext);
                    synchronizer.SynchronizeDatabase();
                    Console.WriteLine($"Successfully synchronized database for tenant: {tenant.TenantCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to synchronize tenant {tenant.TenantCode}: {ex.Message}");
                }
            }
        }
    }
}