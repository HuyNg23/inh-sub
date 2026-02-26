using IBox.Workflow.Model;

namespace IBox.History.DB.WorkflowAndApiThirdPartyHistory
{
    public interface IHandelWFAndAPI
    {
        public ResponseGetAllExecuteQuerySQLHistory GetAllQuerySQLExecuteHistories(RequestGetAllExecuteQuerySQLHistory requestGetAllExecuteHistory);

        public ResponseAllApiThirdPartyExecuteHistory GetAllApiThirdPartyExecuteHistories(RequestGetAllExecuteThirdPartyHistory requestGetAllExecuteThirdPartyHistory);

        public ResponseAllWorkflowExecuteHistory GetAllWorkflowExecuteHistories(RequestGetAllExecuteHistory requestGetAllExecuteHistory);

        public ResponseExecuteHistoryMonth GetApiThirdPartyExecuteHistoryMonthToDaySQLite(RequestExecuteHistoryDayOrMonth request);

        public ResponseExecuteHistoryMonth GetWorkflowExecuteHistoryMonthToDaySQLite(RequestExecuteHistoryDayOrMonth request);

        public ResponseExecuteHistoryMonth GetQuerySQLExecuteHistoryMonthToDaySQLite(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryDay> GetApiThirdPartyExecuteHistoryDayToDaySQLite(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryDay> GetWorkflowExecuteHistoryDayToDaySQLite(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryDay> GetQuerySQLExecuteHistoryDayToDaySQLite(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryDay> GetApiThirdPartyExecuteHistoryDaySQLServer(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryDay> GetWorkflowExecuteHistoryDaySQLServer(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryDay> GetQuerySQLExecuteHistoryDaySQLServer(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryMonth> GetApiThirdPartyExecuteHistoryMonthSQLServer(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryMonth> GetWorkflowExecuteHistoryMonthSQLServer(RequestExecuteHistoryDayOrMonth request);

        public List<ResponseExecuteHistoryMonth> GetQuerySQLExecuteHistoryMonthSQLServer(RequestExecuteHistoryDayOrMonth request);

        public T? CallGetDataHistory<T>(object body, string url, string author, string tenantId);
    }
}