using System;
using System.IO;
using System.Text.Json;
using System.Numerics;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

using System.Runtime.InteropServices;


namespace OpenCraft
{
    public class World : IDisposable
    {
        Shader WorldShader = null!;
        static Random random = new Random();
        public Dictionary<(int, int, int), Chunk> Chunks = new();
        public Dictionary<(int, int, int), byte[,,]> Changes = new();

        int atlasTexture;
        public SimpleGridAtlas Atlas = null!;

        public World(Shader worldShader, int seed=-1)
        {  
            WorldShader = worldShader;

            string path = "config/worldgen.json";

            float frequency;
            int heightScale;
            int border;
            float mPower;
            float vPower;
            float vScale;
            (int blockId, int min, int max)[] layers;

            int randSeed = random.Next(0, 65536);
            if (seed == -1)
                seed = randSeed;

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                JsonElement root = JsonDocument.Parse(json).RootElement;

                frequency   = root.GetProperty("frequency").GetSingle();
                heightScale = root.GetProperty("heightScale").GetInt32();
                border      = root.GetProperty("border").GetInt32();
                mPower      = root.GetProperty("mPower").GetSingle();
                vPower      = root.GetProperty("vPower").GetSingle();
                vScale      = root.GetProperty("vScale").GetSingle();

                var layersJson = root.GetProperty("layers");
                layers = new (int blockId, int min, int max)[layersJson.GetArrayLength()];

                int i = 0;
                foreach (JsonElement el in layersJson.EnumerateArray())
                {
                    int blockId = el.GetProperty("blockId").GetInt32();
                    int min     = el.GetProperty("min").GetInt32();
                    int max     = el.GetProperty("max").GetInt32();

                    layers[i++] = (blockId, min, max);
                }
            }
            else
            {
                frequency   = 0.0003f;
                heightScale = 512;
                border      = 8;
                mPower      = 3f;
                vPower      = 3f;
                vScale      = 0.25f;

                layers = new (int blockId, int min, int max)[]
                {
                    (4, -9999, -20),
                    (3, -21,   50),
                    (2,  51,   120),
                    (5,  121,  9999)
                };
            }

            WorldGenerator.Init(seed, frequency, heightScale, border, mPower, vPower, vScale, layers);
            Console.WriteLine($"Seed: {seed}");

            Atlas = new SimpleGridAtlas(8, 8, 32, 32);

            Atlas.Add("textures/red_wool.png",    id: 1);
            Atlas.Add("textures/dirt.png",        id: 2);
            Atlas.Add("textures/moss_block.png",  id: 3);
            Atlas.Add("textures/sand.png",        id: 4);
            Atlas.Add("textures/stone.png",       id: 5);
            Atlas.Add("textures/cobblestone.png", id: 6);
            Atlas.Add("textures/bricks.png",      id: 7);
            Atlas.Add("textures/oak_planks.png",  id: 8);

            atlasTexture = Atlas.UploadToGpu();
        }

        public void Dispose()
        {
            WorldShader.Dispose();
            foreach (Chunk chunk in Chunks.Values) {chunk.mesh.Dispose();}
        }

        public void Draw(Camera camera)
        {
            WorldShader.Use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, atlasTexture);

            Matrix4 view = camera.GetViewMatrix();
            Matrix4 projection = camera.GetProjectionMatrix();

            WorldShader.SetMatrix4("view", view);
            WorldShader.SetMatrix4("projection", projection);

            foreach (Chunk chunk in Chunks.Values)
            {
                //behind camera
                if (!chunk.noGeometry)
                {
                    Matrix4 model = Matrix4.CreateTranslation(chunk.X * Chunk.Size, chunk.Y * Chunk.Size, chunk.Z * Chunk.Size);
                    WorldShader.SetMatrix4("model", model);
                    chunk.mesh.Draw();
                }
            }
        }


        public bool SetBlockWorld(Vector3i worldBlock, byte id) => SetBlockWorld(worldBlock.X, worldBlock.Y, worldBlock.Z, id);

private void TryRemeshNeighbor(int cx, int cy, int cz, int dx, int dy, int dz)
{
    if (dx == 0 && dy == 0 && dz == 0) return;

    if (Chunks.TryGetValue((cx + dx, cy + dy, cz + dz), out Chunk? n))
        n.GenerateMesh();
}

public bool SetBlockWorld(int wx, int wy, int wz, byte id)
{
    int S = Chunk.Size;

    int cx = (int)MathF.Floor(wx / (float)S);
    int cy = (int)MathF.Floor(wy / (float)S);
    int cz = (int)MathF.Floor(wz / (float)S);

    int lx = wx - cx * S;
    int ly = wy - cy * S;
    int lz = wz - cz * S;

    if (!Chunks.TryGetValue((cx, cy, cz), out Chunk? chunk))
        return false;

    byte old = chunk.GetBlock(lx, ly, lz);
    if (id == old)
        return true;

    chunk.SetBlock(lx, ly, lz, id);
    Changes[(cx, cy, cz)] = chunk.Blocks;

    // Визначаємо, чи блок на межі чанка по кожній осі:
    int ox = (lx == 0) ? -1 : (lx == S - 1) ? +1 : 0;
    int oy = (ly == 0) ? -1 : (ly == S - 1) ? +1 : 0;
    int oz = (lz == 0) ? -1 : (lz == S - 1) ? +1 : 0;

    // Поточний чанк (якщо у тебе це не робиться десь іще)
    chunk.GenerateMesh();

    // ГРАНІ (6 можливих, але фактично до 3 тут)
    if (ox != 0) TryRemeshNeighbor(cx, cy, cz, ox, 0, 0);
    if (oy != 0) TryRemeshNeighbor(cx, cy, cz, 0, oy, 0);
    if (oz != 0) TryRemeshNeighbor(cx, cy, cz, 0, 0, oz);

    // РЕБРА (діагоналі по двох осях)
    if (ox != 0 && oy != 0) TryRemeshNeighbor(cx, cy, cz, ox, oy, 0);
    if (ox != 0 && oz != 0) TryRemeshNeighbor(cx, cy, cz, ox, 0, oz);
    if (oy != 0 && oz != 0) TryRemeshNeighbor(cx, cy, cz, 0, oy, oz);

    // КУТИ (діагональ по трьох осях)
    if (ox != 0 && oy != 0 && oz != 0) TryRemeshNeighbor(cx, cy, cz, ox, oy, oz);

    return true;
}


    }
}