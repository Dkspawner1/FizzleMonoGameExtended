using System;
using FizzleMonoGameExtended.Managers;

namespace FizzleMonoGameExtended.Common;

public class LoadingScreen : DisposableComponent
{
    private readonly GraphicsDevice graphics;
    private readonly SpriteBatch spriteBatch;
    private readonly ContentManagerAsync content;
    private readonly Texture2D fadeTexture;
    private readonly Texture2D progressTexture;
    private readonly Color progressBarColor = Color.Black;
    private readonly Color progressBarBackgroundColor = Color.DarkGray;

    private LoadingStates currentStates = LoadingStates.Starting;
    private float fadeAlpha = 1f;
    private Rectangle progressBarBounds;

    public bool IsComplete { get; private set; }

    [Flags]
    private enum LoadingStates : byte
    {
        None = 0,
        Starting = 1 << 0,
        Loading = 1 << 1,
        Finished = 1 << 2,
    }

    public LoadingScreen(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManagerAsync contentManager)
    {
        graphics = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        this.spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
        content = contentManager ?? throw new ArgumentNullException(nameof(contentManager));

        // Create solid color textures
        fadeTexture = new Texture2D(graphics, 1, 1);
        fadeTexture.SetData([Color.Black]);

        progressTexture = new Texture2D(graphics, 1, 1);
        progressTexture.SetData([Color.White]);

        CalculateProgressBarDimensions();
    }

    private void CalculateProgressBarDimensions()
    {
        var progressBarWidth = graphics.Viewport.Width * 0.6f;
        const float progressBarHeight = 20f;
        var progressBarPosition = new Vector2(
            (graphics.Viewport.Width - progressBarWidth) / 2,
            (graphics.Viewport.Height - progressBarHeight) / 2
        );

        progressBarBounds = new Rectangle(
            (int)progressBarPosition.X,
            (int)progressBarPosition.Y,
            (int)progressBarWidth,
            (int)progressBarHeight
        );
    }

    public void Update(GameTime gameTime)
    {
        if (IsDisposed) return;

        try
        {
            var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Console.WriteLine($"LoadingScreen Update - Progress: {content.Progress:P0} ({content.Progress})");
            UpdateLoadingState(deltaTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loading screen update error: {ex.Message}");
        }
    }

    private void UpdateLoadingState(float deltaTime)
    {
        const float fadeSpeed = 2f;

        switch (currentStates)
        {
            case LoadingStates.Starting:
                fadeAlpha = Math.Max(0f, fadeAlpha - deltaTime * fadeSpeed);
                if (fadeAlpha <= 0f)
                    currentStates = LoadingStates.Loading;
                break;

            case LoadingStates.Loading:
                if (content.Progress >= 1.0f && !content.HasError)
                    currentStates = LoadingStates.Finished;
                break;

            case LoadingStates.Finished:
                fadeAlpha = Math.Min(1f, fadeAlpha + deltaTime * fadeSpeed);
                if (fadeAlpha >= 1f)
                    IsComplete = true;
                break;
        }
    }

    public void Draw()
    {
        if (IsDisposed) return;

        try
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            if (currentStates is LoadingStates.Loading or LoadingStates.Starting)
                DrawProgressBar();

            spriteBatch.Draw(fadeTexture, graphics.Viewport.Bounds, Color.Black * fadeAlpha);
            spriteBatch.End();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Loading screen draw error: {ex.Message}");
        }
    }

    private void DrawProgressBar()
    {
        var progressWidth = (int)(progressBarBounds.Width * content.Progress);
        Console.WriteLine($"Drawing progress bar - Width: {progressWidth}/{progressBarBounds.Width} (Progress: {content.Progress:P0})");
    
        spriteBatch.Draw(progressTexture, progressBarBounds, progressBarBackgroundColor);

        var progressRect = new Rectangle(
            progressBarBounds.X,
            progressBarBounds.Y,
            progressWidth,
            progressBarBounds.Height
        );
        spriteBatch.Draw(progressTexture, progressRect, progressBarColor);
    }

    public void OnResolutionChanged() => CalculateProgressBarDimensions();

    protected override void DisposeManagedResources()
    {
        fadeTexture?.Dispose();
        progressTexture?.Dispose();
    }
}
