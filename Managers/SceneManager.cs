using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzleMonoGameExtended.Assets;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Scene;

namespace FizzleMonoGameExtended.Managers;

public class SceneManager(TexturePool texturePool) : DisposableComponent
{
    private readonly Dictionary<string, SceneBase> scenes = new(8);
    public IReadOnlyDictionary<string, SceneBase> Scenes => scenes;
    private SceneBase currentScene;
    private bool isLoading;
    private string currentSceneName;  
    
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
        if (IsDisposed || isLoading) return;
        if (sceneName == currentSceneName && currentScene != null) return; // Don't reload same scene
    
        Console.WriteLine($"Attempting to load scene: {sceneName}");
    
        if (!scenes.TryGetValue(sceneName, out SceneBase newScene))
        {
            Console.WriteLine($"Scene {sceneName} not found");
            return;
        }

        try
        {
            isLoading = true;
            Console.WriteLine($"Starting scene transition from {currentSceneName} to {sceneName}");

            // Cleanup current scene
            if (currentScene != null)
            {
                Console.WriteLine($"Cleaning up current scene: {currentSceneName}");
                currentScene.ReleaseTextures(texturePool);
                currentScene.Dispose();
                currentScene = null;
                currentSceneName = null;
            }

            // Initialize new scene
            Console.WriteLine($"Initializing new scene: {sceneName}");
            await newScene.LoadContentAsync(texturePool);
            
            // Only set current scene after successful initialization
            currentScene = newScene;
            currentSceneName = sceneName;
            Console.WriteLine($"Scene transition complete: {sceneName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading scene {sceneName}: {ex}");
            currentScene = null;
            currentSceneName = null;
            throw; // Rethrow to handle in Game1
        }
        finally
        {
            isLoading = false;
        }
    }


    public void UpdateCurrentScene(GameTime gameTime)
    {
        if (IsDisposed || isLoading) return;
    
        try
        {
            if (currentScene != null)
            {
                currentScene.Update(gameTime);
            }
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