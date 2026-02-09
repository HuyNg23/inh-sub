using IBox.Common.Objects;
using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Root.Tables;
using IBox.Database.Tenant;
using IBox.Database.Tenant.Tables;
using Newtonsoft.Json;

namespace IBox.MailService
{
    public class ServiceMailAlert : IServiceMailAlert
    {
        public T_Tenant Body(MailAlertRootConfig mailConfigInfo, IConfiguration _configuration, IEncryption _encryption)
        {
            T_Tenant t_Tenant;
            var rootContext = new RootContext(_configuration, _encryption);
            WF_Define wF_Defines = new WF_Define();


            if (mailConfigInfo.Id.Contains("MailAlertROOT"))
            {
                var configRoot = IBGlobalConfig.ConfigRoot.FirstOrDefault(ptr => ptr.TypeConfig == ((int)TypeConfigRoot.MailAlertConfig).ToString() && !ptr.IsDelete);
                if (configRoot == null)
                {
                    throw new IboxLog("MailConfigInfo is null", "AppLogs");
                }



                t_Tenant = JsonConvert.DeserializeObject<T_Tenant>(configRoot.ValueConfig);



                if (t_Tenant == null)
                {
                    throw new IboxLog("Mail Root config is null", "AppLogs");
                }

                if (t_Tenant.AutoSendMail != true)
                {
                    throw new IboxLog("Mail Root config not run", "AppLogs");
                }

                t_Tenant.MailBody = mailConfigInfo.MailBody;
            }
            else
            {
                var context = new TenantContext(_configuration, _encryption, new RootContext(_configuration, _encryption));
                var tenantContext = context.GetTenantContext(mailConfigInfo.Id).Context;

                t_Tenant = IBGlobalConfig.Tenants.FirstOrDefault(ptr => ptr.Id == mailConfigInfo.Id && ptr.AutoSendMail == true && !ptr.IsDelete);

                if (t_Tenant == null)
                {
                    throw new IboxLog("Tenants is null or off AutoSendMail", "AppLogs");
                }

                if (!string.IsNullOrEmpty(mailConfigInfo.WFID))
                {

                    wF_Defines = tenantContext.Context.WF_Defines.FirstOrDefault(ptr => ptr.Id == mailConfigInfo.WFID);


                    if (wF_Defines == null)
                    {
                        throw new IboxLog("Workflow is null", t_Tenant.Id);
                    }
                }

                TypeWarning typeWarning;
                bool flag = Enum.TryParse(mailConfigInfo.typeWarning.ToString(), out typeWarning);
                if (!flag)
                {
                    if (mailConfigInfo.typeWarning.ToString().Length == 1)
                        typeWarning = (TypeWarning)mailConfigInfo.typeWarning.ToString().ToCharArray().First();
                }

                switch (typeWarning)
                {
                    case TypeWarning.APIUnlock:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} đã mở khóa API với Workflow \"{wF_Defines.Name}\"";
                        break;

                    case TypeWarning.WorkflowError:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Hiện tại Workflow {wF_Defines.Name} thuộc Tenant {t_Tenant.TenantName} lỗi khi chạy<BR>{mailConfigInfo.MailBody}";
                        break;

                    case TypeWarning.WorkflowWHError:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Hiện tại WorkflowWH {wF_Defines.Name} thuộc Tenant {t_Tenant.TenantName} lỗi khi chạy";
                        break;

                    case TypeWarning.ChangeConfig:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} đã thay đổi cấu hình Service {mailConfigInfo.MailBody}";
                        break;

                    case TypeWarning.LoginTenant:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} đang login";
                        break;

                    case TypeWarning.TenantDelete:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} đã bị xóa";
                        break;

                    case TypeWarning.APINotFound:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>API expose với Workflow {wF_Defines.Name} không tìm thấy thuộc Tenant {t_Tenant.TenantName}";
                        break;

                    case TypeWarning.ErrorRunSQL:

                        if (string.IsNullOrEmpty(mailConfigInfo.StepID))
                        {
                            throw new IboxLog("Step ID is null", t_Tenant.Id);
                        }

                        var wF_Steps = tenantContext.Context.WF_Steps.FirstOrDefault(ptr => ptr.Id == mailConfigInfo.StepID);

                        if (wF_Steps == null)
                        {
                            throw new IboxLog("Step is null", t_Tenant.Id);
                        }

                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>API Workflow {wF_Defines.Name} thuộc Tenant {t_Tenant.TenantName} xảy ra lỗi tại step {wF_Steps.Description} với step id {mailConfigInfo.StepID}<BR>{mailConfigInfo.MailBody}";
                        break;

                    case TypeWarning.CallAPIError:

                        if (string.IsNullOrEmpty(mailConfigInfo.StepID))
                        {
                            throw new IboxLog("Step ID is null", t_Tenant.Id);
                        }

                        var wF_StepsAPI = tenantContext.Context.WF_Steps.FirstOrDefault(ptr => ptr.Id == mailConfigInfo.StepID);

                        if (wF_StepsAPI == null)
                        {
                            throw new IboxLog("Step is null", t_Tenant.Id);
                        }

                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>API Workflow {wF_Defines.Name} thuộc Tenant {t_Tenant.TenantName} xảy ra lỗi tại step {wF_StepsAPI.Description} với step id {mailConfigInfo.StepID}<BR>{mailConfigInfo.MailBody}";
                        break;

                    case TypeWarning.WorkflowDeployment:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} vừa triển khai Workflow {wF_Defines.Name}";
                        break;

                    case TypeWarning.APIRecall:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} vừa thu hồi Workflow {wF_Defines.Name}";
                        break;

                    case TypeWarning.CreateStepWF:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} vừa thêm mới Step {mailConfigInfo.MailBody} tại Workflow {wF_Defines.Name}";
                        break;

                    case TypeWarning.UpdateStepWF:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} vừa sửa Step {mailConfigInfo.MailBody} tại Workflow {wF_Defines.Name}";
                        break;

                    case TypeWarning.DeleteStepWF:
                        t_Tenant.MailBody = $"Dear Team, <BR> <BR>Tenant {t_Tenant.TenantName} vừa xóa Step {mailConfigInfo.MailBody} tại Workflow {wF_Defines.Name}";
                        break;

                    default:
                        t_Tenant.MailBody = mailConfigInfo.MailBody;
                        break;
                }
                tenantContext.Context.Dispose();
            }

            rootContext.Context.Dispose();
            t_Tenant.MailBody = $"{t_Tenant.MailBody}<BR>Thời gian {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}<BR>Nếu bạn là người thực hiện hành động hãy bỏ qua cảnh báo này.<BR><BR><b> Lưu ý:<BR><BR> Nếu bạn không phải là người thực hiện hành động này hãy liên hệ với team Tích Hợp để kiểm tra kịp thời.</b>";
            return t_Tenant;
        }
    }
}