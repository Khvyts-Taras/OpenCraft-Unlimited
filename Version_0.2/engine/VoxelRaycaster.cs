using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace OpenCraft
{
    public sealed class VoxelRaycaster
    {
        private readonly Dictionary<(int, int, int), Chunk> Chunks;

        public VoxelRaycaster(Dictionary<(int, int, int), Chunk> chunks)
        {
            Chunks = chunks;
        }

        public bool RaycastBlock(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            out Vector3i hitBlock,
            out Vector3i hitNormal)
        {
            hitBlock = default;
            hitNormal = default;

            int x = (int)MathF.Floor(origin.X);
            int y = (int)MathF.Floor(origin.Y);
            int z = (int)MathF.Floor(origin.Z);

            int stepX = direction.X > 0 ? 1 : direction.X < 0 ? -1 : 0;
            int stepY = direction.Y > 0 ? 1 : direction.Y < 0 ? -1 : 0;
            int stepZ = direction.Z > 0 ? 1 : direction.Z < 0 ? -1 : 0;

            float tMaxX = StepTMax(origin.X, direction.X, x, stepX);
            float tMaxY = StepTMax(origin.Y, direction.Y, y, stepY);
            float tMaxZ = StepTMax(origin.Z, direction.Z, z, stepZ);

            float tDeltaX = stepX == 0 ? float.PositiveInfinity : MathF.Abs(1f / direction.X);
            float tDeltaY = stepY == 0 ? float.PositiveInfinity : MathF.Abs(1f / direction.Y);
            float tDeltaZ = stepZ == 0 ? float.PositiveInfinity : MathF.Abs(1f / direction.Z);

            float t = 0f;

            if (IsSolidBlockWorld(x, y, z))
            {
                hitBlock = new Vector3i(x, y, z);
                hitNormal = Vector3i.Zero;
                return true;
            }

            while (t <= maxDistance)
            {
                if (tMaxX < tMaxY && tMaxX < tMaxZ)
                {
                    x += stepX;
                    t = tMaxX;
                    tMaxX += tDeltaX;
                    hitNormal = new Vector3i(-stepX, 0, 0);
                }
                else if (tMaxY < tMaxZ)
                {
                    y += stepY;
                    t = tMaxY;
                    tMaxY += tDeltaY;
                    hitNormal = new Vector3i(0, -stepY, 0);
                }
                else
                {
                    z += stepZ;
                    t = tMaxZ;
                    tMaxZ += tDeltaZ;
                    hitNormal = new Vector3i(0, 0, -stepZ);
                }

                if (t > maxDistance)
                    break;

                if (IsSolidBlockWorld(x, y, z))
                {
                    hitBlock = new Vector3i(x, y, z);
                    return true;
                }
            }

            hitBlock = default;
            hitNormal = default;
            return false;
        }

        public bool IsSolidBlockWorld(int wx, int wy, int wz)
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

            return chunk.Blocks[lx, ly, lz] != 0;
        }

        private static float StepTMax(float originCoord, float dirCoord, int cellCoord, int step)
        {
            if (step == 0) return float.PositiveInfinity;
            float nextBoundary = step > 0 ? (cellCoord + 1) : cellCoord;
            return (nextBoundary - originCoord) / dirCoord;
        }
    }
}
