using IBox.Common.Security;
using IBox.Database.Root;
using IBox.Database.Tenant;
using IBox.DLEx.Implementation;
using IBox.Workflow.Model;
using Microsoft.AspNetCore.Http;

namespace IBox.DLEx.Execution
{
    public interface IExecuteWF : ITenantContext<ExecuteWF>, IDisposable
    {
        HttpResponse HttpResponse { get; set; }

        /// <summary>
        /// Triển khai các luồng nghiệp vụ lên bộ nhớ tạm
        /// </summary>
        /// <param name="wfid"></param>
        void Deploy(string wfid, string tenantId);

        /// <summary>
        /// Thu hồi các luồng nghiệp vụ lên bộ nhớ tạm
        /// </summary>
        /// <param name="wfid"></param>
        void Recovery(string wfid);

        /// <summary>
        /// Thực thi dll và trả về kết quả
        /// </summary>
        /// <param name="wfid">work flow id</param>
        /// <param name="function">function name</param>
        /// <param name="param">Tham số truyền vào</param>
        /// <returns></returns>
        (dynamic, string) Execute(string wfid, dynamic param, string tenantId);

        (dynamic, string) Execute(string wfid, dynamic param, string tenantId, bool isRedeploy);

        IExecuteWF AddHeader(IHeaderDictionary headers);

        List<WFDefine> ViewWFDeploy();

        /// <summary>
        /// Run debug for workflow
        /// </summary>
        /// <param name="wfid"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        ExecuteWF ExecuteDebug(string wfid, dynamic param, string tenantId);

        ExecuteWF CheckDeploy(string wfid, IBContext<TenantContext> tenantContext, string tenantID);
    }
}