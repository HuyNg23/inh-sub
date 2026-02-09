using IBox.ChatBot.DB.DBChatDay;
using IBox.Database.Tenant;
using Quartz;
using Serilog;

namespace IBox.ChatBot.Schedule.DB.Schedule
{
    public class ExecuteScheduleReadFileCustomer : IJob
    {
        private readonly IChangeDBChatDay _changeDBChatDay;
        private static readonly SemaphoreSlim semaphoreCustomer = new SemaphoreSlim(5);

        public ExecuteScheduleReadFileCustomer(IChangeDBChatDay changeDBChatDay)
        {
            _changeDBChatDay = changeDBChatDay;
        }

        public Task Execute(IJobExecutionContext context)
        {
            bool acquired = semaphoreCustomer.Wait(0);

            if (!acquired)
            {
                return Task.CompletedTask;
            }

            try
            {
                try
                {
                    foreach (var configChat in IBGlobalTenantConfig.ConfigChatBots)
                    {
                        this._changeDBChatDay.IdentificationCustomer(configChat);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"CreateDBLiteChatBot: {ex.Message}");
                }
            }
            finally
            {
                semaphoreCustomer.Release();
            }

            return Task.CompletedTask;
        }
    }
}