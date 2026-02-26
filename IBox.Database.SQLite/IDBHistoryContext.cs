namespace IBox.Database.ChatBot
{
    public interface IDBHistoryContext<TContext> : IDisposable
    {
        TContext Context { get; }
    }
}