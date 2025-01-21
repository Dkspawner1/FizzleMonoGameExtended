using System;
using System.Collections.Generic;

namespace FizzleMonoGameExtended.Common;

public class DisposableManager : DisposableComponent
{
    // Pre-allocate capacity
    private readonly List<IDisposable> disposables = new(16);

    public void Add(IDisposable disposable)
    {
        // Early exit for null or disposed state
        if (IsDisposed || disposable == null) return;
        disposables.Add(disposable);
    }

    public void Remove(IDisposable disposable) => disposables.Remove(disposable);

    protected override void DisposeManagedResources()
    {
        // Dispose in reverse order for dependency handling
        for (int i = disposables.Count - 1; i >= 0; i--)
        {
            disposables[i]?.Dispose();
        }
        disposables.Clear();
    }
}
