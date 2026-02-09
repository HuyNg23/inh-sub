using IBox.Database.Root;

namespace IBox.Workflow.Execution
{
    public class StatusActionWF
    {
        private readonly IBContext<RootContext> _rootContext;

        public StatusActionWF(IBContext<RootContext> rootContext)
        {
            _rootContext = rootContext;
        }

        private static bool isWriteLogWfHistory = true;
        private static bool isWriteLogApiThirdpartyHistory = true;

        public static bool IsWriteLogWfHistory
        {
            get { return isWriteLogWfHistory; }
            set { isWriteLogWfHistory = value; }
        }

        public static bool IsWriteLogApiThirdpartyHistory
        {
            get { return isWriteLogApiThirdpartyHistory; }
            set { isWriteLogApiThirdpartyHistory = value; }
        }

        public static Dictionary<string, bool> ChangeStatusWriteLogHistory(bool status)
        {
            isWriteLogWfHistory = status;

            return GetAllStatus();
        }

        public static Dictionary<string, bool> ChangeStatusWriteLogApiThirdPartyHistory(bool status)
        {
            isWriteLogApiThirdpartyHistory = status;

            return GetAllStatus();
        }

        public static Dictionary<string, bool> GetAllStatus()
        {
            Dictionary<string, bool> allStatus = new Dictionary<string, bool>();
            allStatus.Add("IsWriteLogWfHistory", isWriteLogWfHistory);
            allStatus.Add("IsWriteLogApiThirdpartyHistory", isWriteLogApiThirdpartyHistory);

            return allStatus;
        }

        public List<string> GetAllTenantId()
        {
            var listTenantId = _rootContext.Context.T_Tenants.Where(ptr => !ptr.IsDelete).Select(x => x.Id).ToList();
            return listTenantId;
        }
    }
}