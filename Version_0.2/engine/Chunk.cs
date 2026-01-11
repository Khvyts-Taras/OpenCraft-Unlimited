using System;
using System.Collections.Generic;

namespace OpenCraft
{
    public class Chunk
    {
        public Mesh mesh = null!;

        public bool noGeometry = true;
        public bool original = true;

        public int X, Y, Z;
        public const int Size = 32;
        public byte[,,] Blocks;
        private readonly Dictionary<(int,int,int), Chunk> Chunks;
        
        SimpleGridAtlas Atlas;
        Random random = new Random();

        public Chunk(int x, int y, int z, SimpleGridAtlas atlas, Dictionary<(int,int,int), Chunk> chunks)
        {
            Atlas = atlas;
            
            X = x;
            Y = y;
            Z = z;

            Chunks = chunks;
            Blocks = new byte[Size, Size, Size];

            Generate();
        }

        private void Generate()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        int blockX = X*Chunk.Size+x;
                        int blockY = Y*Chunk.Size+y;
                        int blockZ = Z*Chunk.Size+z;


                        byte blockType = WorldGenerator.GenerateBlock(blockX, blockY, blockZ);
                        Blocks[x, y, z] = blockType;

                        if (blockType != 0) noGeometry = false;
                    }
                }
            }

            GenerateMesh();
        }

        public (float[] vertices, float[] uvs, float[] ao, uint[] indices) CalcMesh()
        {
            List<float> vertices = new();
            List<float> uvs = new();
            List<float> ao = new();
            List<uint> indices = new();

            uint indexOffset = 0;

            for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
            for (int z = 0; z < Size; z++)
            {
                byte b = GetBlock(x, y, z);
                if (b == 0) continue;

                var region = Atlas.GetRegion(b);

                if (!IsSolid(GetBlock(x + 1, y, z))) AddFace(FaceDir.PosX, PosX, x, y, z, region, ref vertices, ref uvs, ref ao, ref indices, ref indexOffset);
                if (!IsSolid(GetBlock(x - 1, y, z))) AddFace(FaceDir.NegX, NegX, x, y, z, region, ref vertices, ref uvs, ref ao, ref indices, ref indexOffset);
                if (!IsSolid(GetBlock(x, y + 1, z))) AddFace(FaceDir.PosY, PosY, x, y, z, region, ref vertices, ref uvs, ref ao, ref indices, ref indexOffset);
                if (!IsSolid(GetBlock(x, y - 1, z))) AddFace(FaceDir.NegY, NegY, x, y, z, region, ref vertices, ref uvs, ref ao, ref indices, ref indexOffset);
                if (!IsSolid(GetBlock(x, y, z + 1))) AddFace(FaceDir.PosZ, PosZ, x, y, z, region, ref vertices, ref uvs, ref ao, ref indices, ref indexOffset);
                if (!IsSolid(GetBlock(x, y, z - 1))) AddFace(FaceDir.NegZ, NegZ, x, y, z, region, ref vertices, ref uvs, ref ao, ref indices, ref indexOffset);
            }

            return (vertices.ToArray(), uvs.ToArray(), ao.ToArray(), indices.ToArray());
        }

        static readonly int[][] PosX = {new[]{1,0,0}, new[]{1,1,0}, new[]{1,1,1}, new[]{1,0,1}};
        static readonly int[][] NegX = {new[]{0,0,1}, new[]{0,1,1}, new[]{0,1,0}, new[]{0,0,0}};
        static readonly int[][] PosY = {new[]{0,1,1}, new[]{1,1,1}, new[]{1,1,0}, new[]{0,1,0}};
        static readonly int[][] NegY = {new[]{0,0,0}, new[]{1,0,0}, new[]{1,0,1}, new[]{0,0,1}};
        static readonly int[][] PosZ = {new[]{1,0,1}, new[]{1,1,1}, new[]{0,1,1}, new[]{0,0,1}};
        static readonly int[][] NegZ = {new[]{0,0,0}, new[]{0,1,0}, new[]{1,1,0}, new[]{1,0,0}};

        private enum FaceDir { PosX, NegX, PosY, NegY, PosZ, NegZ }
        static bool IsSolid(byte b) => b != 0;

        private void AddFace(
                FaceDir dir,
                int[][] face, int x, int y, int z,
                SimpleGridAtlas.Region r,
                ref List<float> v, ref List<float> uv, ref List<float> ao, ref List<uint> ind, ref uint indexOffset)
            {
                // Вершини
                for (int i = 0; i < 4; i++)
                {
                    v.Add(x + face[i][0]);
                    v.Add(y + face[i][1]);
                    v.Add(z + face[i][2]);
                }

                // UV (як у тебе)
                uv.Add(r.U0); uv.Add(r.V0);
                uv.Add(r.U0); uv.Add(r.V1);
                uv.Add(r.U1); uv.Add(r.V1);
                uv.Add(r.U1); uv.Add(r.V0);

                // AO: рахуємо 4 occlusion рівні (0..3) і мапимо в brightness float
                int o0 = CalcVertexOcclusionLevel(dir, x, y, z, face[0][0], face[0][1], face[0][2]);
                int o1 = CalcVertexOcclusionLevel(dir, x, y, z, face[1][0], face[1][1], face[1][2]);
                int o2 = CalcVertexOcclusionLevel(dir, x, y, z, face[2][0], face[2][1], face[2][2]);
                int o3 = CalcVertexOcclusionLevel(dir, x, y, z, face[3][0], face[3][1], face[3][2]);

                ao.Add(OcclusionToBrightness(o0));
                ao.Add(OcclusionToBrightness(o1));
                ao.Add(OcclusionToBrightness(o2));
                ao.Add(OcclusionToBrightness(o3));

                // Flip diagonal (прибирає шахматні артефакти на AO)
                bool flip = (o0 + o2) > (o1 + o3);

                if (!flip)
                {
                    ind.Add(indexOffset);
                    ind.Add(indexOffset + 1);
                    ind.Add(indexOffset + 2);

                    ind.Add(indexOffset);
                    ind.Add(indexOffset + 2);
                    ind.Add(indexOffset + 3);
                }
                else
                {
                    ind.Add(indexOffset + 1);
                    ind.Add(indexOffset + 2);
                    ind.Add(indexOffset + 3);

                    ind.Add(indexOffset + 1);
                    ind.Add(indexOffset + 3);
                    ind.Add(indexOffset);
                }

                indexOffset += 4;
            }

        private static float OcclusionToBrightness(int occ)
        {
            // 0 = світло, 3 = темно (підбирай під стиль)
            return occ switch
            {
                0 => 1.00f,
                1 => 0.80f,
                2 => 0.60f,
                _ => 0.40f,
            };
        }

        private int CalcVertexOcclusionLevel(FaceDir dir, int x, int y, int z, int vx, int vy, int vz)
        {
            // base = клітинка прямо "перед" гранню
            int bx = x, by = y, bz = z;

            // u/v — осі в площині грані
            int uDx = 0, uDy = 0, uDz = 0;
            int vDx = 0, vDy = 0, vDz = 0;

            // uStep/vStep мають бути -1 або +1 (не 0/1!)
            int uStep = 0, vStep = 0;

            switch (dir)
            {
                case FaceDir.PosX:
                    bx = x + 1; by = y; bz = z;
                    uDx = 0; uDy = 1; uDz = 0;   // u = Y
                    vDx = 0; vDy = 0; vDz = 1;   // v = Z
                    uStep = vy * 2 - 1;          // vy: 0->-1, 1->+1
                    vStep = vz * 2 - 1;          // vz: 0->-1, 1->+1
                    break;

                case FaceDir.NegX:
                    bx = x - 1; by = y; bz = z;
                    uDx = 0; uDy = 1; uDz = 0;   // u = Y
                    vDx = 0; vDy = 0; vDz = 1;   // v = Z
                    uStep = vy * 2 - 1;
                    vStep = vz * 2 - 1;
                    break;

                case FaceDir.PosY:
                    bx = x; by = y + 1; bz = z;
                    uDx = 1; uDy = 0; uDz = 0;   // u = X
                    vDx = 0; vDy = 0; vDz = 1;   // v = Z
                    uStep = vx * 2 - 1;
                    vStep = vz * 2 - 1;
                    break;

                case FaceDir.NegY:
                    bx = x; by = y - 1; bz = z;
                    uDx = 1; uDy = 0; uDz = 0;   // u = X
                    vDx = 0; vDy = 0; vDz = 1;   // v = Z
                    uStep = vx * 2 - 1;
                    vStep = vz * 2 - 1;
                    break;

                case FaceDir.PosZ:
                    bx = x; by = y; bz = z + 1;
                    uDx = 1; uDy = 0; uDz = 0;   // u = X
                    vDx = 0; vDy = 1; vDz = 0;   // v = Y
                    uStep = vx * 2 - 1;
                    vStep = vy * 2 - 1;
                    break;

                case FaceDir.NegZ:
                    bx = x; by = y; bz = z - 1;
                    uDx = 1; uDy = 0; uDz = 0;   // u = X
                    vDx = 0; vDy = 1; vDz = 0;   // v = Y
                    uStep = vx * 2 - 1;
                    vStep = vy * 2 - 1;
                    break;
            }

            // side1/side2/corner відносно base
            bool side1 = IsSolid(GetBlock(bx + uDx * uStep, by + uDy * uStep, bz + uDz * uStep));
            bool side2 = IsSolid(GetBlock(bx + vDx * vStep, by + vDy * vStep, bz + vDz * vStep));
            bool corner = IsSolid(GetBlock(
                bx + uDx * uStep + vDx * vStep,
                by + uDy * uStep + vDy * vStep,
                bz + uDz * uStep + vDz * vStep));

            // “закритий кут”
            if (side1 && side2) return 3;

            int occ = 0;
            if (side1) occ++;
            if (side2) occ++;
            if (corner) occ++;
            return occ; // 0..3
        }

    

        public void GenerateMesh()
        {
            var (v, uv, ao, ind) = CalcMesh();
            
            if (ind.Length == 0){noGeometry = true;}
            else {noGeometry = false;}

            mesh = new Mesh(v, uv, ao, ind);
        }

        public byte GetBlock(int x, int y, int z)
        {
            if ((uint)x < Size && (uint)y < Size && (uint)z < Size)
                return Blocks[x, y, z];

            int ncx = X;
            int ncy = Y;
            int ncz = Z;

            if (x < 0) { ncx--; x += Size; }
            else if (x >= Size) { ncx++; x -= Size; }

            if (y < 0) { ncy--; y += Size; }
            else if (y >= Size) { ncy++; y -= Size; }

            if (z < 0) { ncz--; z += Size; }
            else if (z >= Size) { ncz++; z -= Size; }

            if (Chunks.TryGetValue((ncx, ncy, ncz), out Chunk? neighbor) && neighbor != null)
                return neighbor.GetBlock(x, y, z);

            int worldX = ncx * Size + x;
            int worldY = ncy * Size + y;
            int worldZ = ncz * Size + z;

            return WorldGenerator.GenerateBlock(worldX, worldY, worldZ);
        }

        public void SetBlock(int x, int y, int z, byte value)
        {
            Blocks[x, y, z] = value;
            GenerateMesh();
        }
    }
}
