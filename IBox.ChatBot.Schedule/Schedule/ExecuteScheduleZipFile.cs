using IBox.ChatBot.DB.DBChatDay;
using Quartz;
using Serilog;

namespace IBox.ChatBot.Schedule.DB.Schedule
{
    public class ExecuteScheduleZipFile : IJob
    {
        public readonly IChangeDBChatDay _changeDBChatDay;

        public ExecuteScheduleZipFile(IChangeDBChatDay changeDBChatDay)
        {
            _changeDBChatDay = changeDBChatDay;
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                _changeDBChatDay.ZipFileChatBot();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Log.Error($"EndChatAll: {ex.Message}");
                throw;
            }
        }
    }
}