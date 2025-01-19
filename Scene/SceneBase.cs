using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DefaultEcs;
using DefaultEcs.System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;

namespace FizzleMonoGameExtended.Scene;

public abstract class SceneBase : DisposableComponent, ITextureUser
{
    protected readonly World world;
    protected readonly Game1 game;
    protected readonly ConcurrentDictionary<string, Texture2D> SceneTextures;
    private readonly object initLock = new();
    
    protected ISystem<float> UpdateSystem { get; set; }
    protected ISystem<SpriteBatch> DrawSystem { get; set; }
    protected SpriteBatch SpriteBatch { get; private set; }
    protected TexturePool TexturePool { get; private set; }

    private bool isInitialized;

    protected SceneBase(Game1 game)
    {
        this.game = game ?? throw new ArgumentNullException(nameof(game));
        world = new World();
        SceneTextures = new ConcurrentDictionary<string, Texture2D>();
    }

    public virtual async Task LoadContentAsync(TexturePool pool)
    {
        if (IsDisposed) return;

        lock (initLock)
        {
            if (isInitialized) return;
            TexturePool = pool ?? throw new ArgumentNullException(nameof(pool));
            SpriteBatch = new SpriteBatch(game.GraphicsDevice);
            isInitialized = true;
        }

        await LoadTexturesAsync();
        await InitializeSystemsAsync();
    }

    private async Task LoadTexturesAsync()
    {
        var textures = GetRequiredTextures();
        var loadTasks = new Task[textures.Length];

        for (int i = 0; i < textures.Length; i++)
        {
            var textureName = textures[i];
            loadTasks[i] = Task.Run(() =>
            {
                try
                {
                    var texture = TexturePool.Acquire(textureName);
                    SceneTextures.TryAdd(textureName, texture);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load texture {textureName}: {ex.Message}");
                }
            });
        }

        await Task.WhenAll(loadTasks);
    }

    protected abstract string[] GetRequiredTextures();

    protected virtual Task InitializeSystemsAsync()
    {
        UpdateSystem ??= CreateUpdateSystem();
        DrawSystem ??= CreateDrawSystem();
        return Task.CompletedTask;
    }

    protected virtual ISystem<float> CreateUpdateSystem() => new EmptySystem<float>();
    protected virtual ISystem<SpriteBatch> CreateDrawSystem() => new EmptySystem<SpriteBatch>();

    public virtual void Update(GameTime gameTime)
    {
        if (IsDisposed || !isInitialized) return;

        try
        {
            UpdateSystem?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during update: {ex.Message}");
        }
    }

    public virtual void Draw(GameTime gameTime)
    {
        if (IsDisposed || !isInitialized || SpriteBatch == null) return;

        try
        {
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            DrawSystem?.Update(SpriteBatch);
            SpriteBatch.End();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during draw: {ex.Message}");
        }
    }

    public virtual void ReleaseTextures(TexturePool pool)
    {
        if (pool == null) return;

        foreach (var (key, texture) in SceneTextures)
        {
            try
            {
                pool.Release(key, texture);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error releasing texture {key}: {ex.Message}");
            }
        }
        SceneTextures.Clear();
    }

    protected override void DisposeManagedResources()
    {
        try
        {
            SpriteBatch?.Dispose();
            world?.Dispose();
            UpdateSystem?.Dispose();
            DrawSystem?.Dispose();
            ReleaseTextures(TexturePool);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during disposal: {ex.Message}");
        }
    }

    private sealed class EmptySystem<T> : ISystem<T>
    {
        public bool IsEnabled { get; set; } = true;
        public void Update(T state) { }
        public void Dispose() { }
    }
}
