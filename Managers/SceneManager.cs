using System.Collections.Generic;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Scene;

namespace FizzleMonoGameExtended.Managers;

public class SceneManager : DisposableComponent
{
    private readonly Dictionary<string, SceneBase> scenes = [];
    private SceneBase currentScene;

    public void AddScene(string sceneName, SceneBase scene)
    {
        scenes[sceneName] = scene;
    }

    public void LoadScene(string sceneName)
    {
        if (scenes.TryGetValue(sceneName, out SceneBase newScene))
        {
            currentScene?.Dispose();
            currentScene = newScene;
            currentScene.LoadContent();
        }
    }

    public void UpdateCurrentScene(GameTime gameTime) => currentScene?.Update(gameTime);
    public void DrawCurrentScene(GameTime gameTime) => currentScene?.Draw(gameTime);

    protected override void DisposeManagedResources()
    {
        foreach (var scene in scenes.Values)
        {
            scene?.Dispose();
        }

        scenes.Clear();
    }

    public void SafeDispose()
    {
        if (currentScene is not null)
        {
            currentScene.Dispose();
            currentScene = null;
        }

        if (scenes is null) return;
        foreach (var scene in scenes.Values)
        {
            scene?.Dispose();
        }

        scenes.Clear();
    }
}