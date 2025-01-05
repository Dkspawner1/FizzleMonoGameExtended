using DefaultEcs;
using DefaultEcs.System;
using FizzleMonoGameExtended.Common;
using Microsoft.Xna.Framework.Content;

namespace FizzleMonoGameExtended.Scene;

public abstract class SceneBase(Game1 game) : DisposableComponent
{
    protected World World { get; private set; } = new World();
    protected ISystem<float> UpdateSystem { get; private set; }
    protected ISystem<SpriteBatch> DrawSystem { get; private set; }

    protected Game1 Game { get; } = game;
    protected ContentManager Content { get; } = game.Content;
    protected GraphicsDevice GraphicsDevice { get; } = game.GraphicsDevice;
    protected SpriteBatch SpriteBatch { get; } = new SpriteBatch(game.GraphicsDevice);

    public virtual void LoadContent() => InitializeSystems();

    protected abstract void InitializeSystems();


    public virtual void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateSystem?.Update(deltaTime);
    }

    public virtual void Draw(GameTime gameTime)
    {
        SpriteBatch.Begin();
        DrawSystem?.Update(SpriteBatch);
        SpriteBatch.End();
    }

    protected override void DisposeManagedResources()
    {
        base.DisposeManagedResources();
        SpriteBatch.Dispose();
        World.Dispose();
        UpdateSystem?.Dispose();
        DrawSystem?.Dispose();
    }
}