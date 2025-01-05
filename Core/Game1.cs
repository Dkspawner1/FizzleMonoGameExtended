using System;
using System.Threading.Tasks;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;
using FizzleMonoGameExtended.Scene;

namespace FizzleMonoGameExtended.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    private DisposableManager disposableManager;
    private SceneManager sceneManager;
    private ContentManagerAsync contentManager;
    private LoadingScreen loadingScreen;
    private Task loadingTask;

    private bool isLoading = true;
    private bool isExiting = false;

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        disposableManager = new DisposableManager();
        sceneManager = new SceneManager();
        contentManager = new ContentManagerAsync(Services);

        disposableManager.Add(sceneManager);
        disposableManager.Add(contentManager);

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
            contentManager?.Dispose();
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
        var menuScene = new MenuScene(this);
        sceneManager.AddScene("MenuScene", menuScene);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        try
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            disposableManager.Add(spriteBatch);

            loadingScreen = new LoadingScreen(GraphicsDevice, spriteBatch, contentManager);

            loadingTask = contentManager.LoadAssetsAsync<Texture2D>(["Content/Textures/btn0"]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading content: {ex.Message}");
        }
        // Remove the finally block that sets isLoading to false
    }

    protected override async void Update(GameTime gameTime)
    {
        if (!loadingScreen.IsComplete)
        {
            await contentManager.UpdateAsync();
            loadingScreen.Update(gameTime);
            
            if (contentManager.Progress >= 1.0f)
            {
                loadingScreen.IsComplete = true;
                isLoading = false;
            }
        }
        else
        {
            if (isLoading)
            {
                sceneManager.LoadScene("MenuScene");
                isLoading = false;
            }
            sceneManager.UpdateCurrentScene(gameTime);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if (!loadingScreen.IsComplete)
        {
            loadingScreen.Draw();
        }
        else
        {
            sceneManager.DrawCurrentScene(gameTime);
        }

        base.Draw(gameTime);
    }
}
