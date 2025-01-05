using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;

namespace FizzleMonoGameExtended.Managers;

public class ContentManagerAsync(IServiceProvider serviceProvider) : ContentManager(serviceProvider)
{
    private readonly ConcurrentQueue<Func<Task>> loadActions = new();
    private readonly ConcurrentDictionary<string, object> loadedAssets = new();
    private int totalAssets;
    private int loadedAssetCount;
    private Exception loadException;
    private readonly TaskCompletionSource<bool> loadingComplete = new();

    public float Progress => totalAssets == 0 ? 0 : (float)loadedAssetCount / totalAssets;
    private bool HasError => loadException != null;
    public Exception LoadException => loadException;

    public async Task LoadAssetsAsync<T>(string[] assetNames)
    {
        totalAssets += assetNames.Length;

        foreach (var assetName in assetNames)
        {
            loadActions.Enqueue(async () =>
            {
                try
                {
                    var asset = await Task.Run(() => base.Load<T>(assetName));
                    loadedAssets[assetName] = asset;
                    loadedAssetCount++;
                }
                catch (Exception ex)
                {
                    loadException = ex;
                    loadingComplete.TrySetException(ex);
                }
            });
        }

        await loadingComplete.Task;
    }

    public async Task UpdateAsync()
    {
        while (loadActions.TryDequeue(out var action))
        {
            await action();
            if (HasError) break;
        }

        if (loadedAssetCount == totalAssets || HasError)
            loadingComplete.TrySetResult(true);
    }

    public new T Load<T>(string assetName) =>
        loadedAssets.TryGetValue(assetName, out var asset)
            ? (T)asset
            : throw new ContentLoadException($"Asset {assetName} not found or not loaded yet.");
}