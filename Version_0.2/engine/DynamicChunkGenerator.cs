namespace OpenCraft
{
    class ChunkGenerator()
    {
        const int distance = 8;
        const int MaxChunks = 50000;
        const int CreatePerFrame = 1;

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

        public static void GenerateChunks(Camera camera, World world)
        {
            int cx = (int)Math.Floor(camera.Position.X / (float)Chunk.Size);
            int cy = (int)Math.Floor(camera.Position.Y / (float)Chunk.Size);
            int cz = (int)Math.Floor(camera.Position.Z / (float)Chunk.Size);

            
            int chunksGenerated = 0;
            foreach (var (dx, dy, dz) in defaultOffsets)
            {
                int x = cx + dx;
                int y = cy + dy;
                int z = cz + dz;

                var key = (x, y, z);
                if (!world.Chunks.ContainsKey(key))
                {
                    world.Chunks[key] = new Chunk(x, y, z, world.Atlas, world.Chunks);
                    chunksGenerated++;
                }
                if (chunksGenerated >= CreatePerFrame) break;
            }

            while (world.Chunks.Count > MaxChunks) {RemoveFarthestChunk(world, cx, cy, cz);}
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
            world.Chunks.Remove(farKey);
        }
    }
}