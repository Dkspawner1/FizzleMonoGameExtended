using System;
using DefaultEcs;
using DefaultEcs.System;
using FizzleMonoGameExtended.ECS.Components;
using Microsoft.Xna.Framework.Input;

namespace FizzleMonoGameExtended.ECS.Systems.Update;

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