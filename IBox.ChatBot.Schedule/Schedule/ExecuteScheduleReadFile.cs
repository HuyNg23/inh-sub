using IBox.ChatBot.DB.DBChatDay;
using IBox.Database.Tenant;
using Quartz;
using Serilog;

namespace IBox.ChatBot.Schedule.DB.Schedule
{
    public class ExecuteScheduleReadFile : IJob
    {
        private readonly IChangeDBChatDay _changeDBChatDay;
        private static readonly SemaphoreSlim semaphoreReadFile = new SemaphoreSlim(5);

        public ExecuteScheduleReadFile(IChangeDBChatDay changeDBChatDay)
        {
            _changeDBChatDay = changeDBChatDay;
        }

        public Task Execute(IJobExecutionContext context)
        {
            bool acquired = semaphoreReadFile.Wait(0);

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
                        this._changeDBChatDay.SaveDataChat(configChat);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"CreateDBLiteChatBot: {ex.Message}");
                }
            }
            finally
            {
                semaphoreReadFile.Release();
            }

            return Task.CompletedTask;
        }
    }
}