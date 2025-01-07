using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;

namespace FizzleMonoGameExtended.Managers;

public class SceneManager(TexturePool texturePool) : DisposableComponent
{
    private readonly Dictionary<string, SceneBase> scenes = new(8);
    public IReadOnlyDictionary<string, SceneBase> Scenes => scenes;
    private SceneBase currentScene;
    private bool isLoading;

    public void AddScene(string sceneName, SceneBase scene)
    {
        if (IsDisposed) return;
        scenes[sceneName] = scene;
    }

    public async Task LoadSceneAsync(string sceneName)
    {
        if (IsDisposed || isLoading) return;
        
        if (!scenes.TryGetValue(sceneName, out SceneBase newScene))
        {
            Console.WriteLine($"Scene {sceneName} not found");
            return;
        }

        try
        {
            isLoading = true;

            // Cleanup current scene
            if (currentScene != null)
            {
                currentScene.ReleaseTextures(texturePool);
                currentScene.Dispose();
            }

            currentScene = newScene;
            await currentScene.LoadContentAsync(texturePool);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading scene {sceneName}: {ex.Message}");
            currentScene = null;
        }
        finally
        {
            isLoading = false;
        }
    }

    public void UpdateCurrentScene(GameTime gameTime)
    {
        if (IsDisposed || isLoading || currentScene == null) return;
        
        try
        {
            currentScene.Update(gameTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating scene: {ex.Message}");
        }
    }

    public void DrawCurrentScene(GameTime gameTime)
    {
        if (IsDisposed || isLoading || currentScene == null) return;

        try
        {
            currentScene.Draw(gameTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error drawing scene: {ex.Message}");
        }
    }

    protected override void DisposeManagedResources()
    {
        if (currentScene != null)
        {
            currentScene.ReleaseTextures(texturePool);
            currentScene.Dispose();
        }

        foreach (var scene in scenes.Values)
        {
            if (scene != currentScene && scene != null)
            {
                scene.ReleaseTextures(texturePool);
                scene.Dispose();
            }
        }

        scenes.Clear();
    }
}