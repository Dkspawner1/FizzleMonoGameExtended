using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Scene;

namespace FizzleMonoGameExtended.Managers;

public class SceneManager(TexturePool texturePool) : DisposableComponent
{
    private readonly TexturePool texturePool = texturePool ?? throw new ArgumentNullException(nameof(texturePool));
    private readonly Dictionary<string, SceneBase> scenes = new(8);
    private SceneBase currentScene;
    private string currentSceneName;
    private bool isLoading;

    public IReadOnlyDictionary<string, SceneBase> Scenes => scenes;

    public void AddScene(string sceneName, SceneBase scene)
    {
        if (IsDisposed) return;
        scenes[sceneName] = scene;
    }

    public void OnResolutionChanged(int width, int height)
    {
        if (currentScene is IResolutionDependent resolutionDependent)
        {
            resolutionDependent.OnResolutionChanged(width, height);
        }
    }

    public async Task LoadSceneAsync(string sceneName)
    {
        if (IsDisposed || isLoading || (sceneName == currentSceneName && currentScene != null)) 
            return;

        if (!scenes.TryGetValue(sceneName, out SceneBase newScene))
        {
            Console.WriteLine($"Scene {sceneName} not found");
            return;
        }

        try
        {
            isLoading = true;
            await TransitionToNewScene(sceneName, newScene);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading scene {sceneName}: {ex}");
            currentScene = null;
            currentSceneName = null;
            throw;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task TransitionToNewScene(string newSceneName, SceneBase newScene)
    {
        if (currentScene != null)
        {
            currentScene.ReleaseTextures(texturePool);
            currentScene.Dispose();
            currentScene = null;
            currentSceneName = null;
        }

        await newScene.LoadContentAsync(texturePool);
        currentScene = newScene;
        currentSceneName = newSceneName;
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
            Console.WriteLine($"Error updating scene: {ex}");
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
            Console.WriteLine($"Error drawing scene: {ex}");
        }
    }

    protected override void DisposeManagedResources()
    {
        currentScene?.ReleaseTextures(texturePool);
        currentScene?.Dispose();

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
