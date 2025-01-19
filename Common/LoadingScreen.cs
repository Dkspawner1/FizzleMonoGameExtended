using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;

namespace FizzleMonoGameExtended.Core;

public class LoadingScreen : DisposableComponent
{
    private readonly GraphicsDevice graphics;
    private readonly SpriteBatch spriteBatch;
    private readonly ContentManagerAsync content;
    private readonly Texture2D fadeTexture;
    private readonly Texture2D progressTexture;
    private readonly Vector2 textPosition;

    private LoadingState currentState = LoadingState.Starting;
    private float fadeAlpha = 1f;
    private float progressBarWidth;
    private float progressBarHeight;
    private Vector2 progressBarPosition;
    private Rectangle progressBarBounds;
    private readonly Color progressBarColor = new(0, 200, 0); // Green
    private readonly Color progressBarBackgroundColor = new(50, 50, 50); // Dark Gray

    public bool IsComplete { get; private set; }

    [Flags]
    private enum LoadingState : byte
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
        fadeTexture.SetData(new[] { Color.Black });

        progressTexture = new Texture2D(graphics, 1, 1);
        progressTexture.SetData(new[] { Color.White });

        CalculateProgressBarDimensions();
        textPosition = CalculateTextPosition();
    }

    private void CalculateProgressBarDimensions()
    {
        progressBarWidth = graphics.Viewport.Width * 0.6f;
        progressBarHeight = 20;
        progressBarPosition = new Vector2(
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

    private Vector2 CalculateTextPosition()
    {
        return new Vector2(
            progressBarPosition.X,
            progressBarPosition.Y - 30
        );
    }

    public void Update(GameTime gameTime)
    {
        if (IsDisposed) return;

        try
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
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

        switch (currentState)
        {
            case LoadingState.Starting:
                fadeAlpha = Math.Max(0f, fadeAlpha - deltaTime * fadeSpeed);
                if (fadeAlpha <= 0f)
                    currentState = LoadingState.Loading;
                break;

            case LoadingState.Loading:
                if (content.Progress >= 1.0f && !content.HasError)
                    currentState = LoadingState.Finished;
                break;

            case LoadingState.Finished:
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

            if (currentState == LoadingState.Loading)
            {
                DrawProgressBar();
                DrawLoadingText();
            }

            // Draw fade overlay
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
        // Draw background
        spriteBatch.Draw(progressTexture, progressBarBounds, progressBarBackgroundColor);

        // Draw progress
        var progressWidth = (int)(progressBarWidth * content.Progress);
        var progressRect = new Rectangle(
            progressBarBounds.X,
            progressBarBounds.Y,
            progressWidth,
            progressBarBounds.Height
        );
        spriteBatch.Draw(progressTexture, progressRect, progressBarColor);
    }

    private void DrawLoadingText()
    {
        if (content.HasError)
        {
            // Implement error message display or remove this block
        }
        else
        {
            var loadingText = $"Loading... {(int)(content.Progress * 100)}%";
            // Implement text drawing
        }
    }

    public void OnResolutionChanged()
    {
        CalculateProgressBarDimensions();
    }

    protected override void DisposeManagedResources()
    {
        fadeTexture?.Dispose();
        progressTexture?.Dispose();
    }
}
