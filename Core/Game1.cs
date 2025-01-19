using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;
using FizzleMonoGameExtended.Scene;

namespace FizzleMonoGameExtended.Core;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly DisposableManager disposableManager;
    private readonly ContentManagerAsync contentManager;

    private SceneManager sceneManager;
    private LoadingScreen loadingScreen;
    private TexturePool texturePool;

    private GameState currentState = GameState.Loading;
    private bool isExiting;
    private bool isInitialized;
    private bool isTransitioningToMenu;

    private enum GameState
    {
        Loading,
        Running,
        Error
    }

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        disposableManager = new DisposableManager();
        contentManager = new ContentManagerAsync(Services);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = false;
        graphics.SynchronizeWithVerticalRetrace = false;

        disposableManager.Add(contentManager);
        Window.ClientSizeChanged += OnClientSizeChanged;
        Exiting += OnExiting;
    }

    private void OnClientSizeChanged(object sender, EventArgs e)
    {
        if (!isInitialized) return;

        graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        graphics.ApplyChanges();

        sceneManager?.OnResolutionChanged(graphics.PreferredBackBufferWidth,
            graphics.PreferredBackBufferHeight);
    }

    protected override void Initialize()
    {
        try
        {
            ConfigureGraphics();
            InitializeManagers();
            base.Initialize();
            isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization error: {ex.Message}");
            currentState = GameState.Error;
        }
    }

    private void ConfigureGraphics()
    {
        graphics.PreferredBackBufferWidth = 1280;
        graphics.PreferredBackBufferHeight = 720;
        graphics.SynchronizeWithVerticalRetrace = true;
        graphics.PreferMultiSampling = true;
        graphics.ApplyChanges();
    }

    private void InitializeManagers()
    {
        texturePool = new TexturePool(GraphicsDevice, Content);
        disposableManager.Add(texturePool);

        sceneManager = new SceneManager(texturePool);
        disposableManager.Add(sceneManager);

        sceneManager.AddScene("MenuScene", new MenuScene(this));
    }

    protected override void LoadContent()
    {
        try
        {
            var spriteBatch = new SpriteBatch(GraphicsDevice);
            disposableManager.Add(spriteBatch);

            loadingScreen = new LoadingScreen(GraphicsDevice, spriteBatch, contentManager);
            _ = LoadAssetsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Content loading error: {ex.Message}");
            currentState = GameState.Error;
        }
    }

    private async Task LoadAssetsAsync()
    {
        try
        {
            var assetPaths = new[] { "Textures/btn0", "Textures/btn1", "Textures/btn2" };
            await contentManager.LoadAssetsAsync<Texture2D>(assetPaths);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Asset loading error: {ex}");
            currentState = GameState.Error;
        }
    }

    protected override async void Update(GameTime gameTime)
    {
        if (isExiting) return;

        try
        {
            switch (currentState)
            {
                case GameState.Loading:
                    await UpdateLoadingState(gameTime);
                    break;
                case GameState.Running:
                    UpdateRunningState(gameTime);
                    break;
                case GameState.Error:
                    UpdateErrorState(gameTime);
                    break;
            }

            base.Update(gameTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Update error: {ex.Message}");
            currentState = GameState.Error;
        }
    }

    private async Task UpdateLoadingState(GameTime gameTime)
    {
        loadingScreen.Update(gameTime);

        if (!isTransitioningToMenu &&
            !contentManager.IsLoading &&
            contentManager.Progress >= 1.0f &&
            !contentManager.HasError)
        {
            isTransitioningToMenu = true;
            await Task.Delay(500);

            try
            {
                await sceneManager.LoadSceneAsync("MenuScene");
                currentState = GameState.Running;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load menu scene: {ex}");
                currentState = GameState.Error;
            }
        }
        else if (contentManager.HasError)
            currentState = GameState.Error;

        await contentManager.UpdateAsync();
    }

    private void UpdateRunningState(GameTime gameTime) => sceneManager.UpdateCurrentScene(gameTime);

    private void UpdateErrorState(GameTime gameTime)
    {
        if (gameTime.TotalGameTime.TotalSeconds > 5)
            Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        try
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
                case GameState.Error:
                    DrawErrorScreen();
                    break;
            }

            base.Draw(gameTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Draw error: {ex.Message}");
            currentState = GameState.Error;
        }
    }

    private void DrawErrorScreen()
    {
        // Implement error screen drawing
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        if (isExiting) return;
        isExiting = true;

        try
        {
            disposableManager.Dispose();
            texturePool?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exit error: {ex.Message}");
        }
        finally
        {
            base.OnExiting(sender, args);
        }
    }
}