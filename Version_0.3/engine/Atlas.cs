using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

namespace OpenCraft;

public sealed class SimpleGridAtlas
{
    public int TilesX { get; }
    public int TilesY { get; }
    public int TileWidth { get; }
    public int TileHeight { get; }
    public int AtlasWidth  => TilesX * TileWidth;
    public int AtlasHeight => TilesY * TileHeight;

    private readonly byte[] _data;
    private readonly Region?[] _regions;

    static SimpleGridAtlas() => StbImage.stbi_set_flip_vertically_on_load(1);

    public SimpleGridAtlas(int tilesX, int tilesY, int tileWidth, int tileHeight)
    {
        TilesX = tilesX;
        TilesY = tilesY;
        TileWidth = tileWidth;
        TileHeight = tileHeight;

        _data = new byte[AtlasWidth * AtlasHeight * 4];
        _regions = new Region?[TilesX * TilesY];
    }

    public readonly record struct Region(float U0, float V0, float U1, float V1);

    private static ImageResult LoadRgba(string path)
    {
        using var s = File.OpenRead(path);
        return ImageResult.FromStream(s, ColorComponents.RedGreenBlueAlpha);
    }

    public void Add(string path, int id)
    {
        int capacity = TilesX * TilesY;
        if ((uint)id >= (uint)capacity)
            throw new ArgumentOutOfRangeException(nameof(id), $"id має бути 0..{capacity - 1}");

        if (_regions[id].HasValue)
            throw new InvalidOperationException($"Region з id={id} вже зайнятий.");

        var img = LoadRgba(path);
        if (img.Width != TileWidth || img.Height != TileHeight)
            throw new InvalidOperationException(
                $"Текстура {path} має бути {TileWidth}x{TileHeight}, а не {img.Width}x{img.Height}.");

        int cellX = id % TilesX;
        int cellY = id / TilesX;

        int x0 = cellX * TileWidth;
        int y0 = cellY * TileHeight;

        int rowBytes = TileWidth * 4;
        for (int y = 0; y < TileHeight; y++)
        {
            int src = y * rowBytes;
            int dst = ((y0 + y) * AtlasWidth + x0) * 4;
            System.Buffer.BlockCopy(img.Data, src, _data, dst, rowBytes);
        }

        float invW = 1f / AtlasWidth;
        float invH = 1f / AtlasHeight;

        _regions[id] = new Region(
            x0 * invW,
            y0 * invH,
            (x0 + TileWidth) * invW,
            (y0 + TileHeight) * invH
        );
    }

    public Region GetRegion(int id)
    {
        int capacity = TilesX * TilesY;
        if ((uint)id >= (uint)capacity)
            throw new ArgumentOutOfRangeException(nameof(id), $"id має бути 0..{capacity - 1}");

        var r = _regions[id];
        if (!r.HasValue)
            throw new InvalidOperationException($"Region з id={id} ще не додано.");

        return r.Value;
    }

    public int UploadToGpu(
        TextureWrapMode wrap = TextureWrapMode.ClampToEdge,
        TextureMinFilter minFilter = TextureMinFilter.Nearest,
        TextureMagFilter magFilter = TextureMagFilter.Nearest)
    {
        int texId = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, texId);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrap);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrap);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
            AtlasWidth, AtlasHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, _data);

        return texId;
    }
}
