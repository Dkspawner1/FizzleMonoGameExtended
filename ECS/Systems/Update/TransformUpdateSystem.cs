using DefaultEcs;
using DefaultEcs.System;
using FizzleMonoGameExtended.ECS.Components;

namespace FizzleMonoGameExtended.ECS.Systems.Update;

public class TransformUpdateSystem(World world) : AEntitySetSystem<float>(world)
{
    protected override void Update(float deltaTime, in Entity entity)
    {
        ref var transform = ref entity.Get<TransformComponent>();
    }
}