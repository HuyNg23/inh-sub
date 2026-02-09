using IBox.ChatBot.DB.DBChatDay;
using Quartz;
using Serilog;

namespace IBox.ChatBot.Schedule.DB.Schedule
{
    public class ExecuteScheduleRemoveZip : IJob
    {
        public readonly IChangeDBChatDay _changeDBChatDay;

        public ExecuteScheduleRemoveZip(IChangeDBChatDay changeDBChatDay)
        {
            _changeDBChatDay = changeDBChatDay;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                _changeDBChatDay.RemoveFileZip();
            }
            catch (Exception ex)
            {
                Log.Error($"EndChatAll: {ex.Message}");
            }

            return Task.CompletedTask;
        }
    }
}