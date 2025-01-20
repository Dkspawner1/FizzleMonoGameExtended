using DefaultEcs;
using DefaultEcs.System;
using FizzleMonoGameExtended.ECS.Components;

namespace FizzleMonoGameExtended.ECS.Systems.Render;

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