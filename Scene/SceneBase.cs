using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefaultEcs;
using DefaultEcs.System;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;

public abstract class SceneBase : DisposableComponent, ITextureUser
{
    private readonly World world = new();
    private readonly Game1 game;
    protected readonly Dictionary<string, Texture2D> SceneTextures = new(32);
    
    protected ISystem<float> UpdateSystem { get; private set; }
    protected ISystem<SpriteBatch> DrawSystem { get; private set; }
    protected SpriteBatch SpriteBatch { get; private set; }
    protected TexturePool TexturePool { get; private set; }

    protected SceneBase(Game1 game)
    {
        this.game = game;
    }

    public virtual async Task LoadContentAsync(TexturePool pool)
    {
        if (IsDisposed) return;

        TexturePool = pool;
        SpriteBatch = new SpriteBatch(game.GraphicsDevice);

        var textures = GetRequiredTextures();
        foreach (var textureName in textures)
        {
            try
            {
                SceneTextures[textureName] = TexturePool.Acquire(textureName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load texture {textureName}: {ex.Message}");
            }
        }

        await InitializeSystemsAsync();
    }

    protected abstract string[] GetRequiredTextures();
    protected virtual Task InitializeSystemsAsync() => Task.CompletedTask;

    public virtual void Update(GameTime gameTime)
    {
        if (IsDisposed) return;
        
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        try
        {
            UpdateSystem?.Update(deltaTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during update: {ex.Message}");
        }
    }

    public virtual void Draw(GameTime gameTime)
    {
        if (IsDisposed || SpriteBatch is null) return;

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
        if (pool is null) return;

        foreach (var (name, texture) in SceneTextures)
        {
            if (texture != null)
                pool.Release(name, texture);
        }
        SceneTextures.Clear();
    }

    protected override void DisposeManagedResources()
    {
        SpriteBatch?.Dispose();
        world.Dispose();
        UpdateSystem?.Dispose();
        DrawSystem?.Dispose();
        ReleaseTextures(TexturePool);
    }
}
