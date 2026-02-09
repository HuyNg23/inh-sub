using IBox.ChatBot.HandleScheduleSQL;
using IBox.Database.Root;
using IBox.Database.Tenant;
using Quartz;
using Serilog;

namespace IBox.ChatBot.Schedule.DB.Schedule
{
    [DisallowConcurrentExecution]
    public class ExecuteScheduleCreateDB : IJob
    {
        private readonly ICreateDB _scheduleCreateDB;

        public ExecuteScheduleCreateDB(ICreateDB scheduleCreateDB)
        {
            _scheduleCreateDB = scheduleCreateDB;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                foreach (var tenant_id in IBGlobalConfig.Tenants)
                {
                    this._scheduleCreateDB.CreateDBLiteChatBot(tenant_id.Id);
                    this._scheduleCreateDB.CreateDBSQLiteHistory(tenant_id.Id);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"CreateDBLiteChatBot: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}