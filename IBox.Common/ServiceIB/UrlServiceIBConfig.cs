namespace IBox.Common.ServiceIB
{
    public class UrlServiceIBConfig : IUrlServiceIBConfig
    {
        public const string UrlDeployWF = "/api/Workflow/DeployHA";
        public const string UrlRecoveryWF = "/api/Workflow/RecoveryHA";
        public const string UrlGetAllWFDeploy = "/api/Workflow/ViewWorkflowDeployHA";
        public const string UrlWriteLogWF = "/api/LogService/WriteWF";
        public const string UrlWriteLogAPI = "/api/LogService/WriteAPI";
        public const string UrlWriteLogSQL = "/api/LogService/WriteSQL";
        public const string UrlAPISendMailServer = "/api/MailAlert/SendMailAlert";
        public const string UrlDeployPage = "/api/Page/DeployHA";
        public const string UrlRecoveryPage = "/api/Page/RecoveryHA";
        public const string UrlGetAllPageDeploy = "/api/Page/ViewPageDeployHA";
        public const string UrlDeleteJobSchedule = "/api/Job/DeleteJobSchedule";
        public const string UrlDeleteJobScheduleRoot = "/api/Job/DeleteJobScheduleRoot";
        public const string UrlGetAllJob = "/api/Job/GetAllJob";
        public const string UrlGetAllJobRoot = "/api/Job/GetAllJobRoot";

        public const string GetQuerySQLExecuteHistoryDetail = "/api/LogService/GetQuerySQLExecuteHistoryDetail";
        public const string UrlGetWFHistorySQLiteDetails = "/api/LogService/GetWorkflowExecuteHistoryDetail";
        public const string UrlGetAPIHistorySQLiteDetails = "/api/LogService/GetApiThirdPartyExecuteHistoriesDetail";

        public const string UrlGetAPIStatusDaySQLite = "/api/LogService/GetApiThirdPartyExecuteHistoryDaySQLite";
        public const string UrlGetWFStatusSDaySQLite = "/api/LogService/GetWorkflowExecuteHistoryDayDetails";
        public const string UrlGetQuerySQLStatusSDaySQLite = "/api/LogService/GetQuerySQLStatusSDaySQLite";

        public const string UrlGetAPIStatusMonthSQLite = "/api/LogService/GetApiThirdPartyExecuteHistoryMonthSQLiteDetails";
        public const string UrlGetWFStatusMonthSQLite = "/api/LogService/GetWorkflowExecuteHistoryMonthSQLiteDetails";
        public const string UrlGetQuerySQLStatusMonthSQLite = "/api/LogService/GetQuerySQLExecuteHistoryMonthSQLiteDetails";

        public const string UrlGetDataAPIStream = "/api/GetDataLog/GetDataAPIStream";
        public const string UrlGetDataWFStream = "/api/GetDataLog/GetDataWFStream";
        public const string UrlGetDataSQLStream = "/api/GetDataLog/GetDataSQLStream";
        public const string UrlGetDataStream = "/api/GetDataLog/GetDataStream";

        public const string UrlSaveFileHTML = "/api/AssetLibrary/SaveFileHTML";
        public const string UrlGetFileHTML = "/api/AssetLibrary/UrlGetFileHTML";
    }
}