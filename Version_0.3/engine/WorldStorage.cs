using System;
using System.IO;


namespace OpenCraft
    {
    public static class WorldStorage
    {
        public static void SaveSeed(string path, int seed)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, seed.ToString());
        }

        public static int LoadSeed(string path)
        {
            if (!File.Exists(path))
                return -1;

            string text = File.ReadAllText(path);
            return int.Parse(text);
        }

        public static void SaveChanges(string path, Dictionary<(int, int, int), byte[,,]> changes)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var bw = new BinaryWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None));

            bw.Write(changes.Count);

            foreach (var kv in changes)
            {
                var (cx, cy, cz) = kv.Key;
                byte[,,] arr = kv.Value;

                int xLen = arr.GetLength(0);
                int yLen = arr.GetLength(1);
                int zLen = arr.GetLength(2);

                // координати чанка
                bw.Write(cx);
                bw.Write(cy);
                bw.Write(cz);

                // розміри (навіть якщо завжди 32, краще зберегти для надійності/версій)
                bw.Write(xLen);
                bw.Write(yLen);
                bw.Write(zLen);

                // дані
                for (int x = 0; x < xLen; x++)
                    for (int y = 0; y < yLen; y++)
                        for (int z = 0; z < zLen; z++)
                            bw.Write(arr[x, y, z]);
            }
        }

        public static Dictionary<(int, int, int), byte[,,]> LoadChanges(string path)
        {
            var changes = new Dictionary<(int, int, int), byte[,,]>();

            if (!File.Exists(path))
                return changes;

            using var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

            int count = br.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                int cx = br.ReadInt32();
                int cy = br.ReadInt32();
                int cz = br.ReadInt32();

                int xLen = br.ReadInt32();
                int yLen = br.ReadInt32();
                int zLen = br.ReadInt32();

                var arr = new byte[xLen, yLen, zLen];

                for (int x = 0; x < xLen; x++)
                    for (int y = 0; y < yLen; y++)
                        for (int z = 0; z < zLen; z++)
                            arr[x, y, z] = br.ReadByte();

                changes[(cx, cy, cz)] = arr;
            }

            return changes;
        }

        public static void ApplyChangesToWorld(OpenCraft.World world, Dictionary<(int, int, int), byte[,,]> changes)
        {
            foreach (var kv in changes)
            {
                var (cx, cy, cz) = kv.Key;
                byte[,,] src = kv.Value;

                if (!world.Chunks.ContainsKey((cx, cy, cz)))
                {
                    world.Chunks[kv.Key] = new Chunk(cx, cy, cz, world.Atlas);
                    world.Chunks[kv.Key].AttachWorld(world.Chunks);
                }

                world.Chunks[kv.Key].Blocks = src;
                world.Chunks[kv.Key].GenerateMesh();
                world.Chunks[kv.Key].ApplyMesh();
            }
        }
    }
}