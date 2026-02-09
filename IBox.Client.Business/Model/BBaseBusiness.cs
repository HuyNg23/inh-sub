using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Common.TCP;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Workflow.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace IBox.Client.Business.Model
{
    public class BBaseBusiness<T> : ATenantContext<BBaseBusiness<T>> where T : BBaseBusiness<T>
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected IBContext<RootContext> RootContext { get; set; }
        protected IEncryption Encryption { get; set; }
        protected IConfiguration Configuration { get; set; }
        protected IWorkflowControl WfControl { get; set; }
        protected IRestAPI RestAPI { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Constructor for chilrend object
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public T Init(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new IboxLog("ServiceProvider not implementation", "AppLogs");
            }

            var contextX = serviceProvider.GetService<IBContext<RootContext>>();
            if (contextX == null)
            {
                throw new IboxLog(string.Format("Base Business object can not get Instance: {0}", typeof(IBContext<RootContext>)), "AppLogs");
            }

            var tenantContextX = serviceProvider.GetService<IBContext<TenantContext>>();
            if (tenantContextX == null)
            {
                throw new IboxLog(string.Format("Base Business object can not get Instance: {0}", typeof(IBContext<TenantContext>)), "AppLogs");
            }

            var encryptionsX = serviceProvider.GetService<IEncryption>();
            if (encryptionsX == null)
            {
                throw new IboxLog(string.Format("Base Business object can not get Instance: {0}", typeof(IEncryption)), "AppLogs");
            }

            var configurationsX = serviceProvider.GetService<IConfiguration>();
            if (configurationsX == null)
            {
                throw new IboxLog(string.Format("Base Business object can not get Instance: {0}", typeof(IConfiguration)), "AppLogs");
            }

            var wfconfigX = serviceProvider.GetService<IWorkflowControl>();
            if (wfconfigX == null)
            {
                throw new IboxLog(string.Format("Base Business object can not get Instance: {0}", typeof(IWorkflowControl)), "AppLogs");
            }

            var restAPIX = serviceProvider.GetService<IRestAPI>();
            if (restAPIX == null)
            {
                throw new IboxLog(string.Format("Base Business object can not get Instance: {0}", typeof(IRestAPI)), "AppLogs");
            }

            this.RootContext = contextX;

            if (this.tenantContext == null)
            {
                this.tenantContext = tenantContextX;
            }

            this.Encryption = encryptionsX;
            this.Configuration = configurationsX;
            this.WfControl = wfconfigX;
            this.RestAPI = restAPIX;

            return (T)this;
        }
    }
}