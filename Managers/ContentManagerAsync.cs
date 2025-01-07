using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;

namespace FizzleMonoGameExtended.Managers;

public class ContentManagerAsync : ContentManager
{
    private readonly ConcurrentDictionary<string, object> loadedAssets = new(32, 4);
    private readonly CancellationTokenSource cts = new();
    private readonly TaskCompletionSource<bool> loadingComplete = new();
    private readonly SemaphoreSlim loadingSemaphore = new(1, 1);

    private int totalAssets;
    private int loadedAssetCount;
    private volatile Exception loadException;

    public float Progress => totalAssets == 0 ? 0 : (float)loadedAssetCount / totalAssets;
    public bool HasError => loadException != null;
    public Exception LoadException => loadException;
    public bool IsLoading { get; private set; }

    public ContentManagerAsync(IServiceProvider serviceProvider) : base(serviceProvider) { }

    public async Task LoadAssetsAsync<T>(string[] assetNames)
    {
        try
        {
            await loadingSemaphore.WaitAsync();
            if (IsLoading) return;
            IsLoading = true;

            Interlocked.Add(ref totalAssets, assetNames.Length);
            var loadTasks = new List<Task>(assetNames.Length);

            foreach (var assetName in assetNames)
            {
                if (cts.Token.IsCancellationRequested) break;
                loadTasks.Add(LoadAssetAsync<T>(assetName));
            }

            await Task.WhenAll(loadTasks);
            loadingComplete.TrySetResult(true);
        }
        finally
        {
            IsLoading = false;
            loadingSemaphore.Release();
        }
    }

    private async Task LoadAssetAsync<T>(string assetName)
    {
        try
        {
            var asset = await Task.Run(() => base.Load<T>(assetName), cts.Token);
            if (loadedAssets.TryAdd(assetName, asset))
            {
                Interlocked.Increment(ref loadedAssetCount);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"Failed to load asset: {assetName}: {ex.Message}");
            loadException = ex;
            throw;
        }
    }

    public async Task UpdateAsync()
    {
        if (cts.Token.IsCancellationRequested) return;

        try
        {
            if (loadedAssetCount == totalAssets || HasError)
            {
                loadingComplete.TrySetResult(true);
            }
        }
        catch (Exception ex)
        {
            loadException = ex;
            loadingComplete.TrySetException(ex);
            throw;
        }
    }

    public new T Load<T>(string assetName)
    {
        if (loadedAssets.TryGetValue(assetName, out var asset))
            return (T)asset;
            
        throw new ContentLoadException(
            $"Asset {assetName} not found or not loaded yet. Current progress: {Progress:P0}");
    }

    public void CancelLoading()
    {
        if (!cts.IsCancellationRequested)
        {
            cts.Cancel();
            loadingComplete.TrySetCanceled();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
                    
                cts.Dispose();
                loadingSemaphore.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }

            foreach (var asset in loadedAssets.Values)
            {
                if (asset is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            loadedAssets.Clear();
        }
        base.Dispose(disposing);
    }
}
