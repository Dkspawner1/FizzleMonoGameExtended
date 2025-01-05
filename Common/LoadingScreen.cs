namespace FizzleMonoGameExtended.Common;

public class LoadingScreen
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly SpriteBatch spriteBatch;
    private readonly ContentManagerAsync contentManager;

    private float fadeAlpha = 1f;
    private bool isFadingIn = true;
    private bool isFadingOut = false;

    private float progress = 0f;
    private Texture2D fadeTexture;
    private Texture2D progressBarTexture;

    public bool IsComplete { get; private set; } = false;

    public LoadingScreen(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, ContentManagerAsync contentManager)
    {
        this.graphicsDevice = graphicsDevice;
        this.spriteBatch = spriteBatch;
        this.contentManager = contentManager;

        fadeTexture = new Texture2D(graphicsDevice, 1, 1);
        fadeTexture.SetData(new[] { Color.Black });
        progressBarTexture = new Texture2D(graphicsDevice, 1, 1);
        progressBarTexture.SetData(new[] { Color.White });
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Handle fade-in
        if (isFadingIn)
        {
            fadeAlpha -= deltaTime; // Reduce alpha
            if (fadeAlpha <= 0f)
            {
                fadeAlpha = 0f;
                isFadingIn = false;
            }
        }

        // Update content loading progress
        if (!isFadingIn && !isFadingOut)
        {
            contentManager.UpdateAsync().Wait(); // Process queued load actions
            progress = contentManager.Progress;

            if (progress >= 1.0f) // All assets loaded
            {
                isFadingOut = true;
            }
        }

        // Handle fade-out
        if (isFadingOut)
        {
            fadeAlpha += deltaTime; // Increase alpha
            if (fadeAlpha >= 1f)
            {
                fadeAlpha = 1f;
                IsComplete = true; // Mark as complete
            }
        }
    }

    public void Draw()
    {
        spriteBatch.Begin();
        if (!isFadingIn && !isFadingOut)
        {
            int barWidth = (int)(graphicsDevice.Viewport.Width * 0.6f);
            int barHeight = 20;
            int barX = (graphicsDevice.Viewport.Width - barWidth) / 2;
            int barY = (graphicsDevice.Viewport.Height - barHeight) / 2;
            spriteBatch.Draw(progressBarTexture, new Rectangle(barX, barY, (int)(barWidth * progress), barHeight),
                Color.Green);
        }

        spriteBatch.Draw(fadeTexture, graphicsDevice.Viewport.Bounds, Color.Black * fadeAlpha);

        spriteBatch.End();
    }
}