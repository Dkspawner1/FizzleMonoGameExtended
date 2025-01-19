using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DefaultEcs;
using DefaultEcs.System;

namespace FizzleMonoGameExtended.Scene;

public class MenuScene : SceneBase, IResolutionDependent
{
    private Entity buttonEntity;
    private Vector2 buttonPosition;
    private MouseState previousMouseState;
    private Rectangle buttonBounds;

    public MenuScene(Game1 game) : base(game)
    {
        CalculateButtonPosition();
    }
    public void OnResolutionChanged(int width, int height)
    {
        CalculateButtonPosition();
        if (buttonEntity.IsAlive && SceneTextures.TryGetValue("Textures/btn0", out var buttonTexture))
        {
            var origin = new Vector2(buttonTexture.Width / 2f, buttonTexture.Height / 2f);
            buttonBounds = new Rectangle(
                (int)(buttonPosition.X - origin.X),
                (int)(buttonPosition.Y - origin.Y),
                buttonTexture.Width,
                buttonTexture.Height
            );
            
            ref var transform = ref buttonEntity.Get<TransformComponent>();
            transform.Position = buttonPosition;
        }
    }
    private void CalculateButtonPosition()
    {
        buttonPosition = new Vector2(
            game.GraphicsDevice.Viewport.Width / 2f,
            game.GraphicsDevice.Viewport.Height / 2f
        );
    }

    protected override string[] GetRequiredTextures()
    {
        return new[]
        {
            "Textures/btn0",
            "Textures/btn1",
            "Textures/btn2"
        };
    }

    protected override async Task InitializeSystemsAsync()
    {
        await base.InitializeSystemsAsync();

        try
        {
            CreateButton();
            InitializeSystems();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing menu scene: {ex.Message}");
        }
    }

    private void CreateButton()
    {
        if (!SceneTextures.TryGetValue("Textures/btn0", out var buttonTexture))
            throw new InvalidOperationException("Button texture not loaded");

        buttonEntity = world.CreateEntity();

        var origin = new Vector2(buttonTexture.Width / 2f, buttonTexture.Height / 2f);
        buttonBounds = new Rectangle(
            (int)(buttonPosition.X - origin.X),
            (int)(buttonPosition.Y - origin.Y),
            buttonTexture.Width,
            buttonTexture.Height
        );

        buttonEntity.Set(new TransformComponent
        {
            Position = buttonPosition,
            Scale = Vector2.One,
            Rotation = 0f
        });

        buttonEntity.Set(new ButtonComponent
        {
            NormalTexture = SceneTextures["Textures/btn0"],
            HoverTexture = SceneTextures["Textures/btn1"],
            PressedTexture = SceneTextures["Textures/btn2"],
            Bounds = buttonBounds
        });

        buttonEntity.Set(new SpriteComponent
        {
            Texture = buttonTexture,
            Origin = origin,
            Color = Color.White,
            LayerDepth = 0f
        });
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

        if (buttonEntity.IsAlive)
        {
            ref var button = ref buttonEntity.Get<ButtonComponent>();
            button.PreviousMouseState = previousMouseState;
            button.CurrentMouseState = currentMouseState;
        }

        previousMouseState = currentMouseState;
        base.Update(gameTime);
    }

    protected override void DisposeManagedResources()
    {
        buttonEntity.Dispose();
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
    public Texture2D NormalTexture;
    public Texture2D HoverTexture;
    public Texture2D PressedTexture;
    public Rectangle Bounds;
    public MouseState CurrentMouseState;
    public MouseState PreviousMouseState;
    public bool IsHovered;
    public bool IsPressed;
}

// Systems
public class ButtonUpdateSystem : AEntitySetSystem<float>
{
    private readonly World world;

    public ButtonUpdateSystem(World world) : base(world)
    {
        this.world = world;
    }

    protected override void Update(float deltaTime, in Entity entity)
    {
        ref var button = ref entity.Get<ButtonComponent>();
        ref var sprite = ref entity.Get<SpriteComponent>();

        var mousePosition = new Point(button.CurrentMouseState.X, button.CurrentMouseState.Y);
        button.IsHovered = button.Bounds.Contains(mousePosition);

        if (button.IsHovered)
        {
            if (button.CurrentMouseState.LeftButton == ButtonState.Pressed)
            {
                button.IsPressed = true;
                sprite.Texture = button.PressedTexture;
            }
            else
            {
                button.IsPressed = false;
                sprite.Texture = button.HoverTexture;

                if (button.PreviousMouseState.LeftButton == ButtonState.Pressed &&
                    button.CurrentMouseState.LeftButton == ButtonState.Released)
                {
                    OnButtonClick(entity);
                }
            }
        }
        else
        {
            button.IsPressed = false;
            sprite.Texture = button.NormalTexture;
        }
    }

    private void OnButtonClick(in Entity entity)
    {
        // Handle button click event
        Console.WriteLine("Button clicked!");
    }
}

public class TransformUpdateSystem : AEntitySetSystem<float>
{
    public TransformUpdateSystem(World world) : base(world)
    {
    }

    protected override void Update(float deltaTime, in Entity entity)
    {
        ref var transform = ref entity.Get<TransformComponent>();
        // Add any transform animations or updates here
    }
}

public class SpriteRenderSystem : AEntitySetSystem<SpriteBatch>
{
    public SpriteRenderSystem(World world) : base(world)
    {
    }

    protected override void Update(SpriteBatch spriteBatch, in Entity entity)
    {
        ref var transform = ref entity.Get<TransformComponent>();
        ref var sprite = ref entity.Get<SpriteComponent>();

        spriteBatch.Draw(
            sprite.Texture,
            transform.Position,
            null,
            sprite.Color,
            transform.Rotation,
            sprite.Origin,
            transform.Scale,
            SpriteEffects.None,
            sprite.LayerDepth
        );
    }
}