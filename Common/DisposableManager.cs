using System;
using System.Collections.Generic;

namespace FizzleMonoGameExtended.Common;

public class DisposableManager : DisposableComponent
{
    private readonly List<IDisposable> disposables = [];

    public void Add(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        disposables.Add(disposable);
    }

    public void Remove(IDisposable disposable) => disposables.Remove(disposable);

    protected override void DisposeManagedResources()
    {
        for (var i = disposables.Count - 1; i >= 0; i--)
        {
            disposables[i].Dispose();
        }

        disposables.Clear();
    }
}