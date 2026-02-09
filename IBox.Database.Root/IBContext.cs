namespace IBox.Database.Root
{
    public interface IBContext<TContext> : IDisposable
    {
        TContext Context { get; }

        IBContext<TContext> GetTenantContext(string? tenantID);
    }
}