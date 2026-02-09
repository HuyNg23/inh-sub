namespace IBox.Database.SQLiteMaster
{
    public interface IDBChatMasterContext<TContext> : IDisposable
    {
        TContext Context { get; }
    }
}
