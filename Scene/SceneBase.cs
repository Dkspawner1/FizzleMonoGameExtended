using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.System;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;
using Microsoft.Xna.Framework.Content;

namespace FizzleMonoGameExtended.Scene;

public abstract class SceneBase(Game1 game) : DisposableComponent, ITextureUser
{
    private readonly World world = new();
    protected ISystem<float> UpdateSystem { get; }
    protected ISystem<SpriteBatch> DrawSystem { get; }
    protected readonly Dictionary<string, Texture2D> SceneTextures = new();

    protected SpriteBatch SpriteBatch { get; private set; }
    protected TexturePool TexturePool { get; private set; }

    public virtual void LoadContent(TexturePool pool)
    {
        TexturePool = pool;
        SpriteBatch = new SpriteBatch(game.GraphicsDevice);

        foreach (var textureName in GetRequiredTextures())
        {
            SceneTextures[textureName] = TexturePool.Acquire(textureName);
        }

        InitializeSystems();
    }

    protected abstract string[] GetRequiredTextures();
    protected abstract void InitializeSystems();

    public virtual void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateSystem?.Update(deltaTime);
    }

    public virtual void Draw(GameTime gameTime)
    {
        if (SpriteBatch is null)
            return;

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
        foreach (var (name, texture) in SceneTextures)
        {
            pool.Release(name, texture);
        }
        SceneTextures.Clear();
    }

    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        SpriteBatch?.Dispose();
        world.Dispose();
        UpdateSystem?.Dispose();
        DrawSystem?.Dispose();
        ReleaseTextures(TexturePool);
    }
}