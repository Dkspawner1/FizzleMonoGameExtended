using System;
using System.Threading.Tasks;
using FizzleMonoGameExtended.Assets;
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
    private TexturePool texturePool;

    private GameState currentState = GameState.Loading;

    private bool isExiting;

    private enum GameState
    {
        Loading,
        Running
    }

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        disposableManager = new DisposableManager();
        contentManager = new ContentManagerAsync(Services);

        disposableManager.Add(contentManager);

        Exiting += OnExiting;
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (isExiting) return;
        isExiting = true;

        try
        {
            // Cleanup resources in reverse order of initialization
            if (sceneManager != null)
            {
                foreach (var scene in sceneManager.scenes.Values)
                {
                    if (scene is ITextureUser textureUser)
                    {
                        textureUser.ReleaseTextures(texturePool);
                    }
                }
                sceneManager.Dispose();
            }

            // Dispose other managed resources
            spriteBatch?.Dispose();
            disposableManager?.Dispose();
            contentManager?.Dispose();
            texturePool?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during cleanup: {ex.Message}");
        }
        finally
        {
            base.OnExiting(sender, args);
        }
    }

    protected override void Initialize()
    {
        texturePool = new TexturePool(GraphicsDevice,Content);
        sceneManager = new SceneManager(texturePool);
        disposableManager.Add(sceneManager);
        
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

            // Queue all assets for loading
            QueueAssetLoading();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading content: {ex.Message}");
        }
    }

    private async void QueueAssetLoading()
    {
        var assetPaths = new[] { "Content/Textures/btn0" };
        foreach (var path in assetPaths)
        {
            var texture = texturePool.Acquire(path);
            await contentManager.LoadAssetsAsync<Texture2D>([path]);
        }
    }

    protected override async void Update(GameTime gameTime)
    {
        switch (currentState)
        {
            case GameState.Loading:
                await UpdateLoadingState(gameTime);
                break;

            case GameState.Running:
                UpdateRunningState(gameTime);
                break;
        }

        base.Update(gameTime);
    }

    private async Task UpdateLoadingState(GameTime gameTime)
    {
        await contentManager.UpdateAsync();
        loadingScreen.Update(gameTime);

        if (contentManager.Progress >= 1.0f)
        {
            await Task.Delay(500); // Smooth transition
            currentState = GameState.Running;
            sceneManager.LoadScene("MenuScene");
        }
    }

    private void UpdateRunningState(GameTime gameTime)
    {
        sceneManager.UpdateCurrentScene(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        switch (currentState)
        {
            case GameState.Loading:
                loadingScreen.Draw();
                break;

            case GameState.Running:
                sceneManager.DrawCurrentScene(gameTime);
                break;
        }

        base.Draw(gameTime);
    }
}