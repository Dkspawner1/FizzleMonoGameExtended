using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
public class ContentManagerAsync : ContentManager
{
    private readonly ConcurrentQueue<Func<Task>> loadActions = new ConcurrentQueue<Func<Task>>();
    private readonly ConcurrentDictionary<string, object> loadedAssets = new ConcurrentDictionary<string, object>();
    private int totalAssets;
    private int loadedAssetCount;
    private Exception loadException;
    private TaskCompletionSource<bool> loadingComplete = new TaskCompletionSource<bool>();

    public float Progress => totalAssets == 0 ? 0 : (float)loadedAssetCount / totalAssets;
    public bool HasError => loadException != null;
    public Exception LoadException => loadException;

    public ContentManagerAsync(IServiceProvider serviceProvider) : base(serviceProvider) { }

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
        {
            loadingComplete.TrySetResult(true);
        }
    }

    public new T Load<T>(string assetName)
    {
        if (loadedAssets.TryGetValue(assetName, out var asset))
        {
            return (T)asset;
        }
        throw new ContentLoadException($"Asset {assetName} not found or not loaded yet.");
    }
}
