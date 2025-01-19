using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using FizzleMonoGameExtended.Common;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace FizzleMonoGameExtended.Assets;

public class TexturePool : DisposableComponent
{
    private readonly GraphicsDevice graphics;
    private readonly ContentManager content;
    private readonly ConcurrentDictionary<string, Queue<Texture2D>> pool = new();
    private readonly object lockObject = new();

    public TexturePool(GraphicsDevice graphicsDevice, ContentManager contentManager)
    {
        graphics = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        content = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
    }

    public Texture2D Acquire(string textureName)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(TexturePool));

        var queue = pool.GetOrAdd(textureName, _ => new Queue<Texture2D>());

        lock (lockObject)
        {
            if (queue.Count > 0)
                return queue.Dequeue();
        }

        return LoadTexture(textureName);
    }

    private Texture2D LoadTexture(string textureName)
    {
        try
        {
            return content.Load<Texture2D>(textureName);
        }
        catch (ContentLoadException ex)
        {
            Console.WriteLine($"Error loading texture '{textureName}': {ex.Message}");
            return CreateFallbackTexture();
        }
    }

    private Texture2D CreateFallbackTexture()
    {
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
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        var queue = pool.GetOrAdd(textureName, _ => new Queue<Texture2D>());
        lock (lockObject)
        {
            queue.Enqueue(texture);
        }
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
