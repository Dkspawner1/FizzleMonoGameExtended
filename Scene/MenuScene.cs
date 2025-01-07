using System.Threading.Tasks;

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

    protected override Task InitializeSystemsAsync()
    {
        return base.InitializeSystemsAsync();
    }
    
}