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

    public ContentManagerAsync(IServiceProvider serviceProvider) : base(serviceProvider) => RootDirectory = "Content";

    public async Task LoadAssetsAsync<T>(string[] assetNames)
    {
        Console.WriteLine($"Starting to load {assetNames.Length} assets:");
        foreach (var name in assetNames)
        {
            Console.WriteLine($"  - {name}");
        }

        await loadingSemaphore.WaitAsync();
        try
        {
            if (IsLoading)
            {
                Console.WriteLine("Already loading, returning early");
                return;
            }

            IsLoading = true;
            totalAssets = assetNames.Length;
            loadedAssetCount = 0;

            Console.WriteLine($"Beginning load of {totalAssets} assets");

            foreach (var assetName in assetNames)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Loading cancelled");
                    break;
                }

                Console.WriteLine($"Loading asset: {assetName}");
                await LoadAssetAsync<T>(assetName);
                Console.WriteLine(
                    $"Current progress after loading {assetName}: {Progress:P0} ({loadedAssetCount}/{totalAssets})");
                await Task.Delay(50);
            }

            Console.WriteLine($"Asset loading complete. Final count: {loadedAssetCount}/{totalAssets}");
            loadingComplete.TrySetResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LoadAssetsAsync: {ex}");
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
            Console.WriteLine($"Starting LoadAssetAsync for: {assetName}");

            var asset = await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"Task.Run: Loading {assetName}");
                    // Check if already loaded first
                    if (loadedAssets.TryGetValue(assetName, out var cachedAsset))
                    {
                        Console.WriteLine($"Found cached asset: {assetName}");
                        return (T)cachedAsset;
                    }

                    var loadedAsset = base.Load<T>(assetName);
                    Console.WriteLine($"Task.Run: Successfully loaded {assetName}");

                    // Add to cache and increment counter in one atomic operation
                    if (loadedAssets.TryAdd(assetName, loadedAsset))
                    {
                        int newCount = Interlocked.Increment(ref loadedAssetCount);
                        Console.WriteLine($"Added asset and incremented count: {newCount}/3");
                    }

                    return loadedAsset;
                }
                catch (ContentLoadException ex)
                {
                    Console.WriteLine($"Task.Run: Failed to load {assetName}: {ex.Message}");
                    throw;
                }
            }, cts.Token);

            if (asset == null)
            {
                Console.WriteLine($"Loaded asset is null for {assetName}");
                throw new ContentLoadException($"Asset {assetName} loaded as null");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"Exception in LoadAssetAsync for {assetName}: {ex}");
            throw;
        }
    }

    public override T Load<T>(string assetName)
    {
        if (loadedAssets.TryGetValue(assetName, out var asset))
        {
            Console.WriteLine($"Returning cached asset: {assetName}");
            return (T)asset;
        }

        Console.WriteLine($"Asset not in cache, loading: {assetName}");
        return base.Load<T>(assetName);
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