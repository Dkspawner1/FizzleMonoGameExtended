using System.Collections.Generic;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Scene;

namespace FizzleMonoGameExtended.Managers;

public class SceneManager(TexturePool texturePool) : DisposableComponent
{
    public readonly Dictionary<string, SceneBase> scenes = [];
    private SceneBase currentScene;

    public void AddScene(string sceneName, SceneBase scene) => scenes[sceneName] = scene;

    public void LoadScene(string sceneName)
    {
        if (!scenes.TryGetValue(sceneName, out SceneBase newScene)) return;
        
        // Properly cleanup current scene
        if (currentScene != null)
        {
            currentScene.ReleaseTextures(texturePool);
            currentScene.Dispose();
        }
        
        currentScene = newScene;
        currentScene.LoadContent(texturePool);
    }

    public void UpdateCurrentScene(GameTime gameTime) => currentScene?.Update(gameTime);
    public void DrawCurrentScene(GameTime gameTime) => currentScene?.Draw(gameTime);

    protected override void DisposeManagedResources()
    {
        if (currentScene != null)
        {
            currentScene.ReleaseTextures(texturePool);
            currentScene.Dispose();
        }

        foreach (var scene in scenes.Values)
        {
            if (scene != currentScene) // Avoid double disposal
            {
                scene.ReleaseTextures(texturePool);
                scene.Dispose();
            }
        }

        scenes.Clear();
    }
}