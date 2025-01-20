using Microsoft.Xna.Framework.Input;

namespace FizzleMonoGameExtended.ECS.Components;

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