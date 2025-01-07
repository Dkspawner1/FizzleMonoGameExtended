using System;
using System.Collections.Generic;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;

public class LoadingScreen : DisposableComponent
{
    private readonly GraphicsDevice graphics;
    private readonly SpriteBatch spriteBatch;
    private readonly ContentManagerAsync content;
    private readonly Dictionary<string, Texture2D> loadingTextures = new(4);

    private LoadingState currentState = LoadingState.Starting;
    private float fadeAlpha = 1f;
    private Vector2 textPosition = Vector2.Zero;

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
        graphics = graphicsDevice;
        this.spriteBatch = spriteBatch;
        content = contentManager;

        InitializeTextures();
        CalculateTextPosition();
    }

    private void InitializeTextures()
    {
        loadingTextures["fade"] = new Texture2D(graphics, 1, 1);
        loadingTextures["fade"].SetData(new[] { Color.Black });
        loadingTextures["progress"] = new Texture2D(graphics, 1, 1);
        loadingTextures["progress"].SetData(new[] { Color.White });
    }

    private void CalculateTextPosition()
    {
        int barWidth = (int)(graphics.Viewport.Width * 0.6f);
        int barHeight = 20;
        int barX = (graphics.Viewport.Width - barWidth) / 2;
        int barY = (graphics.Viewport.Height - barHeight) / 2;
        textPosition = new Vector2(barX, barY - 30);
    }

    public void Update(GameTime gameTime)
    {
        if (IsDisposed) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (currentState)
        {
            case LoadingState.Starting:
                fadeAlpha = Math.Max(0f, fadeAlpha - deltaTime * 2f); // Faster fade
                if (fadeAlpha <= 0f)
                    currentState = LoadingState.Loading;
                break;

            case LoadingState.Loading:
                // Add progress smoothing
                if (content.Progress >= 1.0f)
                {
                    currentState = LoadingState.Finished;
                }
                break;

            case LoadingState.Finished:
                fadeAlpha = Math.Min(1f, fadeAlpha + deltaTime);
                if (fadeAlpha >= 1f)
                    IsComplete = true;
                break;
        }
    }

    public void Draw()
    {
        if (IsDisposed) return;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (currentState is LoadingState.Loading)
        {
            DrawProgressBar();
            DrawLoadingText();
        }

        spriteBatch.Draw(loadingTextures["fade"], graphics.Viewport.Bounds, Color.Black * fadeAlpha);
        spriteBatch.End();
    }

    private void DrawProgressBar()
    {
        int barWidth = (int)(graphics.Viewport.Width * 0.6f);
        int barHeight = 20;
        int barX = (graphics.Viewport.Width - barWidth) / 2;
        int barY = (graphics.Viewport.Height - barHeight) / 2;

        spriteBatch.Draw(loadingTextures["progress"], 
            new Rectangle(barX, barY, barWidth, barHeight), Color.DarkGray);
        spriteBatch.Draw(loadingTextures["progress"],
            new Rectangle(barX, barY, (int)(barWidth * content.Progress), barHeight), Color.Green);
    }

    private void DrawLoadingText()
    {
        var loadingText = $"Loading... {(int)(content.Progress * 100)}%";
        var textSize = Vector2.One * 12;
        textPosition = new Vector2(
            (graphics.Viewport.Width - textSize.X) / 2,
            (graphics.Viewport.Height - textSize.Y) / 2 - 30
        );
    }

    protected override void DisposeManagedResources()
    {
        foreach (var texture in loadingTextures.Values)
        {
            texture?.Dispose();
        }
        loadingTextures.Clear();
    }
}
