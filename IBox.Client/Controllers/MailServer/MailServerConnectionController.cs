using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using IBox.Security;
using Microsoft.AspNetCore.Mvc;

namespace IBox.Client.Controllers.MailServer
{
    [Route("api/[controller]")]
    [ApiController]
    [IBoxAuthorization]
    [IBoxPermissions]
    public class MailServerConnectionController : ActionController<MailServerConnection, TenantContext>
    {
        public MailServerConnectionController(IBContext<TenantContext> mainContext, IEncryption encryption) : base(mainContext, encryption)
        {
        }

        [IBoxActionPermission("integration-config-mailServer-manage", "integration-config-mailServer-view")]
        public override ResponseForm<List<dynamic>> GetAllDeleted(RequestForm<dynamic> req, int pageNumber)
        {
            return base.GetAllDeleted(req, pageNumber);
        }

        [IBoxActionPermission("integration-config-mailServer-manage")]
        public override ResponseForm<dynamic> Delete(RequestForm<dynamic> req)
        {
            return base.Delete(req);
        }

        [IBoxActionPermission("integration-config-mailServer-manage")]
        public override ResponseForm<dynamic> Create(RequestForm<dynamic> req)
        {
            try
            {
                var mailModel = CheckValid(req);

                if (string.IsNullOrEmpty(mailModel.Smtp_Host?.Trim()))
                {
                    throw new IboxLog("Host can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(mailModel.Smtp_Port?.Trim()))
                {
                    throw new IboxLog("Port can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.MailServerConnections.FirstOrDefault(ptr => ptr.Smtp_Host == mailModel.Smtp_Host && !ptr.IsDelete);

                if (data != null)
                {
                    throw new IboxLog("Mail connection info has exist.", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Create(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [IBoxActionPermission("integration-config-mailServer-manage")]
        public override ResponseForm<dynamic> Update(RequestForm<dynamic> req)
        {
            try
            {
                var mailModel = CheckValid(req);

                if (string.IsNullOrEmpty(mailModel.Smtp_Host?.Trim()))
                {
                    throw new IboxLog("Host can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                if (string.IsNullOrEmpty(mailModel.Smtp_Port?.Trim()))
                {
                    throw new IboxLog("Port can't null or Empty", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }

                var data = base.IBContext.Context.MailServerConnections.FirstOrDefault(ptr => ptr.Smtp_Host == mailModel.Smtp_Host && ptr.Id != mailModel.Id && !ptr.IsDelete);
                if (data != null)
                {
                    throw new IboxLog("Mail connection info has exist host", base.IBContext.Context.TenantInfo.Id ?? "AppLogs");
                }
                return base.Update(req);
            }
            catch (Exception ex)
            {
                return new ResponseForm<dynamic>(() => throw ex);
            }
        }

        [HttpPost("GetAll/{pageNumber}")]
        [IBoxActionPermission("integration-config-mailServer-manage", "integration-config-mailServer-view")]
        public override ResponseForm<List<dynamic>> GetAll(RequestForm<dynamic> req, int pageNumber)
        {
            try
            {
                return new ResponseForm<List<dynamic>>(() =>
                {
                    if (pageNumber == -1)
                    {
                        var queryResultPage = base.IBContext.Context.MailServerConnections.Where(ptr => !ptr.IsDelete).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();

                        foreach (var item in queryResultPage)
                        {
                            if (item.Smtp_Password != null)
                            {
                                item.Smtp_Password = "*********";
                            }
                        }

                        return queryResultPage;
                    }
                    else
                    {
                        int numberOfObjectsPerPage = 30;
                        var queryResultPage = base.IBContext.Context.Context.MailServerConnections
                            .Where(ptr => !ptr.IsDelete)
                            .Skip(numberOfObjectsPerPage * (pageNumber - 1))
                            .Take(numberOfObjectsPerPage).OrderByDescending(ptr => ptr.CreatedDate).ToList<dynamic>();

                        foreach (var item in queryResultPage)
                        {
                            if (item.Smtp_Password != null)
                            {
                                item.Smtp_Password = "*********";
                            }
                        }

                        return queryResultPage;
                    }
                });
            }
            catch (Exception ex)
            {
                return new ResponseForm<List<dynamic>>(() => throw ex);
            }
        }
    }
}