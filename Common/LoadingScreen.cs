using System;
using FizzleMonoGameExtended.Managers;

namespace FizzleMonoGameExtended.Common;

public class LoadingScreen
{
    private readonly GraphicsDevice graphics;
    private readonly SpriteBatch spriteBatch;
    private readonly ContentManagerAsync content;
    private SpriteFont loadingFont;
    private const string LoadingText = "Loading...";
    private Vector2 textPosition = Vector2.Zero;


    private LoadingState currentState = LoadingState.Starting;

    private Texture2D fadeTexture;
    private Texture2D progressBarTexture;

    [Flags]
    private enum LoadingState : byte
    {
        None = 0,
        Starting = 1 << 0,
        Loading = 1 << 1,
        Finished = 1 << 2,
    }


    // Transition:
    public bool IsComplete { get; private set; }
    private float fadeAlpha = 1f;

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
        fadeTexture = new Texture2D(graphics, 1, 1);
        fadeTexture.SetData(new[] { Color.Black });
        progressBarTexture = new Texture2D(graphics, 1, 1);
        progressBarTexture.SetData(new[] { Color.White });
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
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (currentState)
        {
            case LoadingState.Starting:
                fadeAlpha -= deltaTime;
                if (fadeAlpha <= 0f)
                {
                    fadeAlpha = 0f;
                    currentState = LoadingState.Loading;
                }

                break;

            case LoadingState.Loading:
                if (content.Progress >= 1.0f)
                {
                    currentState = LoadingState.Finished;
                }

                break;

            case LoadingState.Finished:
                fadeAlpha += deltaTime;
                if (fadeAlpha >= 1f)
                {
                    fadeAlpha = 1f;
                    IsComplete = true;
                }

                break;
        }
    }

    public void Draw()
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (currentState is LoadingState.Loading)
        {
            DrawProgressBar();
            
            // Remove the loadingFont check since we'll use basic text for now
            var loadingText = $"{LoadingText} {(int)(content.Progress * 100)}%";
            var textSize = Vector2.One * 12; // Basic size estimation
            textPosition = new Vector2(
                (graphics.Viewport.Width - textSize.X) / 2,
                (graphics.Viewport.Height - textSize.Y) / 2 - 30
            );
        }

        // Draw fade overlay
        spriteBatch.Draw(fadeTexture, graphics.Viewport.Bounds, Color.Black * fadeAlpha);

        spriteBatch.End();
    }

    private void DrawProgressBar()
    {
        int barWidth = (int)(graphics.Viewport.Width * 0.6f);
        int barHeight = 20;
        int barX = (graphics.Viewport.Width - barWidth) / 2;
        int barY = (graphics.Viewport.Height - barHeight) / 2;


        // Draw background
        spriteBatch.Draw(progressBarTexture, new Rectangle(barX, barY, barWidth, barHeight),
            Color.DarkGray);
        // Draw progress
        spriteBatch.Draw(progressBarTexture,
            new Rectangle(barX, barY, (int)(barWidth * content.Progress), barHeight),
            Color.Green);
    }
}