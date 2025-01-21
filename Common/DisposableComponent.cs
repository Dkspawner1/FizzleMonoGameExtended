using System;

namespace FizzleMonoGameExtended.Common;

public class DisposableComponent : IDisposable
{
    protected volatile bool IsDisposed;

    protected virtual void Dispose(bool disposing)
    {
        // Thread-safe check and set
        if (!IsDisposed)
        {
            if (disposing)
            {
                DisposeManagedResources();
            }
            DisposeUnmanagedResources();
            IsDisposed = true;
        }
    }

    protected virtual void DisposeManagedResources()
    {
    }

    protected virtual void DisposeUnmanagedResources()
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DisposableComponent()
    {
        Dispose(false);
    }
}