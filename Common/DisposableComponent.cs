using System;

namespace FizzleMonoGameExtended.Common;

public class DisposableComponent : IDisposable
{
    public bool IsDisposed { get; private set; } = false;

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed) return;
        if (disposing)
            DisposeManagedResources();

        DisposeUnmanagedResources();
        IsDisposed = true;
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