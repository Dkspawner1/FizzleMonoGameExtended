using System;
using System.Collections.Generic;
using FizzleMonoGameExtended.Common;
using FizzleMonoGameExtended.Managers;
using Microsoft.Xna.Framework.Content;

namespace FizzleMonoGameExtended.Assets;

public class TexturePool(GraphicsDevice graphics, ContentManager content) : DisposableComponent
{
    private readonly Dictionary<string, Queue<Texture2D>> pool = [];
    public Texture2D Acquire(string textureName)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(TexturePool));

        if (!pool.ContainsKey(textureName))
            pool[textureName] = new Queue<Texture2D>();

        if (pool[textureName].Count > 0)
            return pool[textureName].Dequeue();

        try
        {
            // Check if asset is already loaded in ContentManagerAsync
            var texture = content.Load<Texture2D>(textureName);
            pool[textureName].Enqueue(texture);
            return texture;
        }
        catch (ContentLoadException ex)
        {
            Console.WriteLine($"Error loading texture '{textureName}': {ex.Message}");
            var fallback = CreateFallbackTexture(textureName);
            pool[textureName].Enqueue(fallback);
            return fallback;
        }
    }
    private Texture2D CreateFallbackTexture(string textureName)
    {
        // Create a distinctive fallback texture (checkerboard pattern)
        var texture = new Texture2D(graphics, 64, 64);
        var colors = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                colors[x + y * 64] = ((x + y) % 2 == 0) ? Color.Magenta : Color.Black;
            }
        }

        texture.SetData(colors);
        return texture;
    }

    public void Release(string textureName, Texture2D texture)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(TexturePool));

        if (!pool.ContainsKey(textureName))
            pool[textureName] = new Queue<Texture2D>();

        pool[textureName].Enqueue(texture);
    }

    private Texture2D CreateTexture(string textureName)
    {
        var texture = new Texture2D(graphics, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }

    protected override void DisposeManagedResources()
    {
        foreach (var queue in pool.Values)
        {
            while (queue.Count > 0)
            {
                var texture = queue.Dequeue();
                texture.Dispose();
            }
        }

        pool.Clear();
    }
}