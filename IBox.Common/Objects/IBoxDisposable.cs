namespace IBox.Common.Objects
{
    public abstract class IBoxDisposable : IDisposable
    {
        protected bool disposed = false;

        // Implement IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                }

                // Dispose unmanaged resources (if any)
                // No unmanaged resources in this example, but you can add them here

                disposed = true;
            }
        }

        // Finalizer
        ~IBoxDisposable()
        {
            Dispose(false);
        }
    }
}