using System;
using System.Threading.Tasks;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;

namespace FizzleMonoGameExtended.Core;

public class Game1 : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;

    private DisposableManager disposableManager;
    private SceneManager sceneManager;
    private ContentManagerAsync contentManager;

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
        base.Initialize();
    }

    protected override async void LoadContent()
    {
        try
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            disposableManager.Add(spriteBatch);
            loadingTask = contentManager.LoadAssetsAsync<Texture2D>(["s", "a"]);
            await loadingTask;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading content: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    protected override async void Update(GameTime gameTime)
    {
        if (isLoading)
        {
            await contentManager.UpdateAsync();
            
            if (contentManager.HasError)
            {
                Console.WriteLine($"Error loading content: {contentManager.LoadException.Message}");
                isLoading = false;
            }
            else if (contentManager.Progress >= 1.0f)
            {
                isLoading = false;
            }
        }
        else
        {
            sceneManager.UpdateCurrentScene(gameTime);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if (isLoading)
        {
            DrawLoadingScreen();
        }
        else
        {
            sceneManager.DrawCurrentScene(gameTime);
        }

        base.Draw(gameTime);
    }

    private void DrawLoadingScreen()
    {
        spriteBatch.Begin();
        // Draw loading screen elements here
        if (contentManager.HasError)
        {
            // Draw error message
        }
        else
        {
            // Draw progress bar using contentManager.Progress
        }

        spriteBatch.End();
    }
}