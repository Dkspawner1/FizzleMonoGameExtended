using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using DefaultEcs;
using DefaultEcs.System;

namespace FizzleMonoGameExtended.Scene;

public class MenuScene(Game1 game) : SceneBase(game), IResolutionDependent
{
    private readonly List<Entity> buttonEntities = new();
    private MouseState previousMouseState;
    private const float BUTTON_SPACING = 125f;
    private const int BUTTON_COUNT = 3;

    public void OnResolutionChanged(int width, int height)
    {
        RecalculateButtonPositions();
    }

    private void RecalculateButtonPositions()
    {
        if (!SceneTextures.TryGetValue("Textures/btn0", out var buttonTexture))
            return;

        for (var i = 0; i < buttonEntities.Count; i++)
        {
            var entity = buttonEntities[i];
            if (!entity.IsAlive) continue;

            var buttonPosition = new Vector2(
                200,
                300 + (i * BUTTON_SPACING)
            );

            var buttonWidth = buttonTexture.Width / 4;
            var buttonHeight = buttonTexture.Height / 4;
            var origin = new Vector2(buttonTexture.Width / 2f, buttonTexture.Height / 2f);
            var buttonBounds = new Rectangle(
                (int)(buttonPosition.X - (buttonWidth / 2f)),
                (int)(buttonPosition.Y - (buttonHeight / 2f)),
                buttonWidth,
                buttonHeight
            );

            ref var transform = ref entity.Get<TransformComponent>();
            ref var button = ref entity.Get<ButtonComponent>();

            transform.Position = buttonPosition;
            button.Bounds = buttonBounds;
        }
    }

    protected override string[] GetRequiredTextures() =>
    [
        "Textures/btn0",
        "Textures/btn1",
        "Textures/btn2"
    ];

    protected override async Task InitializeSystemsAsync()
    {
        await base.InitializeSystemsAsync();

        try
        {
            CreateButtons();
            InitializeSystems();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing menu scene: {ex.Message}");
        }
    }

    private void CreateButtons()
    {
        if (!SceneTextures.TryGetValue("Textures/btn0", out var buttonTexture))
            throw new InvalidOperationException("Button texture not loaded");

        string[] buttonIds = ["Play", "Settings", "Exit"];

        for (var i = 0; i < BUTTON_COUNT; i++)        {
            var buttonPosition = new Vector2(
                400,
                300 + (i * BUTTON_SPACING)
            );

            var buttonEntity = world.CreateEntity();
            buttonEntities.Add(buttonEntity);

            var buttonWidth = buttonTexture.Width / 4;
            var buttonHeight = buttonTexture.Height / 4;
            var scale = new Vector2(
                buttonWidth / (float)buttonTexture.Width,
                buttonHeight / (float)buttonTexture.Height
            );

            // Calculate the actual scaled dimensions
            var scaledWidth = (int)(buttonTexture.Width * scale.X);
            var scaledHeight = (int)(buttonTexture.Height * scale.Y);

            // Calculate bounds based on the scaled dimensions
            var buttonBounds = new Rectangle(
                (int)buttonPosition.X, // Left edge at position X
                (int)buttonPosition.Y - scaledHeight / 2, // Center vertically
                scaledWidth,
                scaledHeight
            );

            buttonEntity.Set(new TransformComponent
            {
                Position = buttonPosition,
                Scale = scale,
                Rotation = 0f
            });

            buttonEntity.Set(new ButtonComponent
            {
                Texture = SceneTextures[$"Textures/btn{i}"],
                Bounds = buttonBounds,
                ButtonId = buttonIds[i] // Set the button ID
            });

            buttonEntity.Set(new SpriteComponent
            {
                Texture = SceneTextures[$"Textures/btn{i}"],
                Origin = Vector2.Zero, // Use zero origin since we're calculating bounds directly
                Color = Color.White,
                LayerDepth = 0f
            });
        }
    }

    private void InitializeSystems()
    {
        UpdateSystem = new SequentialSystem<float>(
            new ButtonUpdateSystem(world),
            new TransformUpdateSystem(world)
        );

        DrawSystem = new SequentialSystem<SpriteBatch>(
            new SpriteRenderSystem(world)
        );
    }

    public override void Update(GameTime gameTime)
    {
        var currentMouseState = Mouse.GetState();

        foreach (var entity in buttonEntities)
        {
            if (entity.IsAlive)
            {
                ref var button = ref entity.Get<ButtonComponent>();
                button.PreviousMouseState = previousMouseState;
                button.CurrentMouseState = currentMouseState;
            }
        }

        previousMouseState = currentMouseState;
        base.Update(gameTime);
    }

    protected override void DisposeManagedResources()
    {
        foreach (var entity in buttonEntities)
        {
            if (entity.IsAlive)
            {
                entity.Dispose();
            }
        }

        buttonEntities.Clear();
        base.DisposeManagedResources();
    }
}

// Components
public struct TransformComponent
{
    public Vector2 Position;
    public float Rotation;
    public Vector2 Scale;
}

public struct SpriteComponent
{
    public Texture2D Texture;
    public Vector2 Origin;
    public Color Color;
    public float LayerDepth;
}

public struct ButtonComponent
{
    public Texture2D Texture;
    public Rectangle Bounds;
    public MouseState CurrentMouseState;
    public MouseState PreviousMouseState;
    public bool IsHovered;
    public bool IsPressed;
    public string ButtonId;
    
}

// Systems
public class ButtonUpdateSystem(World world) : AEntitySetSystem<float>(world)
{
    protected override void Update(float deltaTime, in Entity entity)
    {
        ref var button = ref entity.Get<ButtonComponent>();
        ref var sprite = ref entity.Get<SpriteComponent>();

        var mousePosition = new Point(button.CurrentMouseState.X, button.CurrentMouseState.Y);
        button.IsHovered = button.Bounds.Contains(mousePosition);

        sprite.Color = button.IsHovered ? Color.Gray : Color.White;

        if (button.IsHovered)
        {
            if (button.CurrentMouseState.LeftButton == ButtonState.Pressed)
            {
                button.IsPressed = true;
                sprite.Color = Color.DarkGray;
            }
            else
            {
                button.IsPressed = false;
                if (button.PreviousMouseState.LeftButton == ButtonState.Pressed &&
                    button.CurrentMouseState.LeftButton == ButtonState.Released)
                    OnButtonClick(entity, button.ButtonId);
            }
        }
        else
            button.IsPressed = false;
    }

    private void OnButtonClick(in Entity entity, string buttonId)
    {
        Console.WriteLine($"Button '{buttonId}' clicked!");
        
        switch (buttonId)
        {
            case "Play":
                HandlePlayClick();
                break;
            case "Settings":
                HandleSettingsClick();
                break;
            case "Exit":
                HandleExitClick();
                break;
        }
    }
    private void HandlePlayClick()
    {
        Console.WriteLine("Starting game...");
        // Add play logic
    }
    private void HandleSettingsClick()
    {
        Console.WriteLine("Opening settings menu...");
        // Add settings logic
    }
    private void HandleExitClick()
    {
        Console.WriteLine("Exiting game...");
        // Add exit logic
    }
}

public class TransformUpdateSystem(World world) : AEntitySetSystem<float>(world)
{
    protected override void Update(float deltaTime, in Entity entity)
    {
        ref var transform = ref entity.Get<TransformComponent>();
    }
}

public class SpriteRenderSystem(World world) : AEntitySetSystem<SpriteBatch>(world)
{
    protected override void Update(SpriteBatch spriteBatch, in Entity entity)
    {
        ref var transform = ref entity.Get<TransformComponent>();
        ref var sprite = ref entity.Get<SpriteComponent>();
        ref var button = ref entity.Get<ButtonComponent>();

        spriteBatch.Draw(
            sprite.Texture,
            button.Bounds,
            null,
            sprite.Color,
            transform.Rotation,
            sprite.Origin,
            SpriteEffects.None,
            sprite.LayerDepth
        );
    }
}