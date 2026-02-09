using IBox.ChatBot.DB.DBChatDay;
using Quartz;
using Serilog;

namespace IBox.ChatBot.Schedule.DB.Schedule
{
    [DisallowConcurrentExecution]
    public class ExecuteScheduleEndChat : IJob
    {
        public readonly IChangeDBChatDay _changeDBChatDay;

        public ExecuteScheduleEndChat(IChangeDBChatDay changeDBChatDay)
        {
            _changeDBChatDay = changeDBChatDay;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                _changeDBChatDay.EndChatAll();
            }
            catch (Exception ex)
            {
                Log.Error($"EndChatAll: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}