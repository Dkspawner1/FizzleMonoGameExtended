using System;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;

namespace FizzleMonoGameExtended.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    private DisposableManager disposableManager;
    private SceneManager sceneManager;
    private bool isExiting = false;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        disposableManager = new DisposableManager();
        sceneManager = new SceneManager();
        disposableManager.Add(sceneManager);

        Exiting += OnExiting;
    }


    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (isExiting) return;
        isExiting = true;

        try
        {
            sceneManager?.Dispose();
            disposableManager?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during exit: {ex.Message}");
        }
        finally
        {
            base.OnExiting(sender, args);
        }
    }


    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        disposableManager.Add(_spriteBatch);
    }

    protected override void Update(GameTime gameTime)
    {
        sceneManager.UpdateCurrentScene(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        sceneManager.DrawCurrentScene(gameTime);


        base.Draw(gameTime);
    }
}