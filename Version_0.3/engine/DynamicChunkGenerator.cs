using OpenTK.Mathematics;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCraft
{
    class ChunkGenerator()
    {
        const int distance = 16;
        const int MaxChunks = 50000;
        const int CreatePerFrame = 4;


        static List<(int dx, int dy, int dz)> defaultOffsets = GenerateOffsetsByDistance(distance);

        static List<(int dx, int dy, int dz)> GenerateOffsetsByDistance(int distance)
        {
            int maxD2 = 3 * distance * distance;
            var buckets = new List<(int dx, int dy, int dz)>[maxD2 + 1];

            for (int dx = -distance; dx <= distance; dx++)
            for (int dy = -distance; dy <= distance; dy++)
            for (int dz = -distance; dz <= distance; dz++)
            {
                int d2 = dx*dx + dy*dy + dz*dz;
                (buckets[d2] ??= new List<(int, int, int)>()).Add((dx, dy, dz));
            }

            var result = new List<(int dx, int dy, int dz)>((2*distance + 1) * (2*distance + 1) * (2*distance + 1));
            for (int d2 = 0; d2 <= maxD2; d2++)
                if (buckets[d2] != null)
                    result.AddRange(buckets[d2]);

            return result;
        }


        public static void ApplyMeshInMainTHread(Chunk[] chunks, World world)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var key = (chunk.X, chunk.Y, chunk.Z);

                if (!world.Chunks.ContainsKey(key))
                {
                    if (world.Chunks.TryAdd(key, chunk))
                    {
                        chunk.ApplyMesh();
                        chunk.AttachWorld(world.Chunks);
                    }
                }
            }
        }


        public static Task<Chunk[]> GenerateChunksAsync(Camera camera, World world)
        {
            return Task.Run(() => GenerateChunks(camera, world));
        }

        public static Chunk[] GenerateChunks(Camera camera, World world)
        {
            int cx = (int)MathF.Floor(camera.Position.X / Chunk.Size);
            int cy = (int)MathF.Floor(camera.Position.Y / Chunk.Size);
            int cz = (int)MathF.Floor(camera.Position.Z / Chunk.Size);

            var toCreate = new List<(int x, int y, int z)>(CreatePerFrame);

            foreach (var (dx, dy, dz) in defaultOffsets)
            {
                var key = (cx + dx, cy + dy, cz + dz);
                //if (!world.Chunks.ContainsKey(key) && Frustrum.ChunkInFrustum(camera, new Vector3(dx, dy, dz)))
                if (!world.Chunks.ContainsKey(key))
                {
                    toCreate.Add(key);
                    if (toCreate.Count >= CreatePerFrame) break;
                }
            }

            var prepared = new Chunk[toCreate.Count];

            Parallel.For(0, toCreate.Count, i =>
            {
                var key = toCreate[i];
                var chunk = new Chunk(key.x, key.y, key.z, world.Atlas);
                chunk.GenerateMesh();
                prepared[i] = chunk;
            });

            return prepared;
        }


        private static void RemoveFarthestChunk(World world, int cx, int cy, int cz)
        {
            if (world.Chunks.Count == 0) return;

            (int, int, int) farKey = default;
            long bestDist2 = long.MinValue;
            bool hasKey = false;

            foreach (var k in world.Chunks.Keys)
            {
                long dx = (long)k.Item1 - cx;
                long dy = (long)k.Item2 - cy;
                long dz = (long)k.Item3 - cz;

                long dist2 = dx * dx + dy * dy + dz * dz;

                if (!hasKey || dist2 > bestDist2)
                {
                    bestDist2 = dist2;
                    farKey = k;
                    hasKey = true;
                }
            }

            if (!hasKey) return;

            world.Chunks[farKey].mesh.Dispose();
            world.Chunks.TryRemove(farKey, out _);
        }
    }
}