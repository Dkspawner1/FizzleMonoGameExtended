using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FizzleMonoGameExtended.Managers;

public class ContentManagerAsync : ContentManager
{
    private readonly ConcurrentDictionary<string, object> loadedAssets = new();
    private readonly CancellationTokenSource cts = new();
    private readonly SemaphoreSlim loadingSemaphore = new(1, 1);
    private readonly TaskCompletionSource<bool> loadingComplete = new();

    private int totalAssets;
    private int loadedAssetCount;
    private volatile Exception loadException;

    public float Progress => totalAssets == 0 ? 0 : (float)loadedAssetCount / totalAssets;
    public bool HasError => loadException != null;
    public Exception LoadException => loadException;
    public bool IsLoading { get; private set; }

    public ContentManagerAsync(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        RootDirectory = "Content";
    }

    public async Task LoadAssetsAsync<T>(string[] assetNames)
    {
        if (assetNames == null || assetNames.Length == 0)
            return;

        await loadingSemaphore.WaitAsync();
        try
        {
            if (IsLoading) return;
            IsLoading = true;

            totalAssets = assetNames.Length;
            loadedAssetCount = 0;

            var loadTasks = new List<Task>(assetNames.Length);
            foreach (var assetName in assetNames)
            {
                if (cts.Token.IsCancellationRequested) break;
                loadTasks.Add(LoadAssetAsync<T>(assetName));
                await Task.Delay(10); // Small delay to prevent overwhelming the content pipeline
            }

            await Task.WhenAll(loadTasks);
            loadingComplete.TrySetResult(true);
        }
        catch (Exception ex)
        {
            loadException = ex;
            loadingComplete.TrySetException(ex);
            throw;
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
            var asset = await Task.Run(() =>
            {
                try
                {
                    return Load<T>(assetName);
                }
                catch (ContentLoadException ex)
                {
                    Console.WriteLine($"Failed to load {assetName}: {ex.Message}");
                    throw;
                }
            }, cts.Token);

            if (loadedAssets.TryAdd(assetName, asset))
            {
                Interlocked.Increment(ref loadedAssetCount);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"Failed to load asset: {assetName}: {ex.Message}");
            throw;
        }
    }

    public override T Load<T>(string assetName)
    {
        if (loadedAssets.TryGetValue(assetName, out var asset))
            return (T)asset;

        var loadedAsset = base.Load<T>(assetName);
        loadedAssets.TryAdd(assetName, loadedAsset);
        return loadedAsset;
    }

    public async Task UpdateAsync()
    {
        if (cts.Token.IsCancellationRequested) return;

        try
        {
            if (loadedAssetCount == totalAssets || HasError)
            {
                await Task.Yield(); // Add this to make the method truly async
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
                loadingSemaphore.Dispose();

                foreach (var asset in loadedAssets.Values)
                {
                    (asset as IDisposable)?.Dispose();
                }
                loadedAssets.Clear();
            }
            catch (ObjectDisposedException)
            {
                // Ignore if already disposed
            }
        }
        base.Dispose(disposing);
    }
}
