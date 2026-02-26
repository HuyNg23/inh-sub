namespace IBox.Database.ChatBot
{
    public interface IDBChatDayContext<TContext> : IDisposable
    {
        TContext Context { get; }
    }
}