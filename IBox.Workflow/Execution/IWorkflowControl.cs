using IBox.Common.Security;
using IBox.Workflow.Model;

namespace IBox.Workflow.Execution
{
    public interface IWorkflowControl : ITenantContext<WorkflowControl>, IDisposable
    {
        /// <summary>
        /// Deploy trên môi trường của máy
        /// </summary>
        /// <param name="wfid"></param>
        void Deploy(string wfid, string ternantId);

        /// <summary>
        /// Thu hồi wf đã triển khai trên môi trường HA
        /// </summary>
        /// <param name="wfid"></param>
        void Recovery(string wfid);

        /// <summary>
        /// Danh sách wf đã được deploy trên local
        /// </summary>
        /// <returns></returns>
        public List<WFDefine> AllWFDeploy();

        /// <summary>
        /// get workflow trực tiếp từ Database
        /// </summary>
        /// <param name="wfid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public WFDefine GetByIDInDB(string wfid);

        /// <summary>
        /// Lấy workflow bằng id đã deploy lên local
        /// </summary>
        /// <param name="wfid"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public WFDefine GetWFDeployByID(string wfid, string tenantId);

        /// <summary>
        /// Kiểm tra workflow đã tồn tại trong danh sách ở local hay chưa, nếu chưa có thì sẽ đẩy thêm vào danh sách ở local
        /// </summary>
        /// <param name="wfid"></param>
        /// <returns></returns>
        bool IsExist(string wfid, string tenantId);

        /// <summary>
        /// Kiểm tra workflow, nếu không tồn tại thì thực hiện triển khai.
        /// </summary>
        /// <param name="wfid"></param>
        public void DeployIfNotExist(string wfid, string tenantId);
    }
}