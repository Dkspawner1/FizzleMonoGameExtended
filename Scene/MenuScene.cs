namespace FizzleMonoGameExtended.Scene;

public class MenuScene(Game1 game) : SceneBase(game)
{
    protected override string[] GetRequiredTextures()
    {
        return
        [
            "Content/Textures/btn0"
            // Add other required textures here
        ];
    }

    protected override void InitializeSystems()
    {
        // Initialize your systems here
    }
    
}