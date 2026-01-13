using System;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
    public class Mesh : IDisposable
    {
        public int VAO { get; private set; }

        private int modelVBO;
        private int textureVBO;
        private int normalVBO;
        private int aoVBO;


        private int EBO;
        private int indexCount;

        public Mesh(float[] vertices, float[] texCoords, float[] ao, uint[] indices)
        {
            indexCount = indices.Length;

            float[] normals = ComputeNormals(vertices, indices);

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);

            // === Positions VBO (location = 0) ===
            modelVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, modelVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(index: 0, size: 3, type: VertexAttribPointerType.Float, normalized: false, stride: 0, offset: 0);
            GL.EnableVertexAttribArray(0);

            // === Normals VBO (location = 1) ===
            normalVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, normals.Length * sizeof(float), normals, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(index: 1, size: 3, type: VertexAttribPointerType.Float, normalized: false, stride: 0, offset: 0);
            GL.EnableVertexAttribArray(1);

            // === TexCoords VBO (location = 2) ===
            textureVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, textureVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, texCoords.Length * sizeof(float), texCoords, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(index: 2, size: 2, type: VertexAttribPointerType.Float, normalized: false, stride: 0, offset: 0);
            GL.EnableVertexAttribArray(2);

            // === AO VBO (location = 3) ===
            aoVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, aoVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, ao.Length * sizeof(float), ao, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(index: 3, size: 1, type: VertexAttribPointerType.Float, normalized: false, stride: 0, offset: 0);
            GL.EnableVertexAttribArray(3);

            // === EBO ===
            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            GL.BindVertexArray(VAO);
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(modelVBO);
            GL.DeleteBuffer(textureVBO);
            GL.DeleteBuffer(normalVBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteVertexArray(VAO);
        }

        public static float[] ComputeNormals(float[] vertices, uint[] indices)
        {
            int vertexCount = vertices.Length / 3;
            var normals = new Vector3[vertexCount];

            for (int i = 0; i < indices.Length; i += 3)
            {
                int i0 = (int)indices[i + 0];
                int i1 = (int)indices[i + 1];
                int i2 = (int)indices[i + 2];

                Vector3 p0 = new Vector3(vertices[i0 * 3 + 0], vertices[i0 * 3 + 1], vertices[i0 * 3 + 2]);
                Vector3 p1 = new Vector3(vertices[i1 * 3 + 0], vertices[i1 * 3 + 1], vertices[i1 * 3 + 2]);
                Vector3 p2 = new Vector3(vertices[i2 * 3 + 0], vertices[i2 * 3 + 1], vertices[i2 * 3 + 2]);

                Vector3 e1 = p1 - p0;
                Vector3 e2 = p2 - p0;

                Vector3 n = Vector3.Cross(e1, e2);

                normals[i0] += n;
                normals[i1] += n;
                normals[i2] += n;
            }

            var outNormals = new float[vertexCount * 3];

            for (int v = 0; v < vertexCount; v++)
            {
                Vector3 n = normals[v];
                if (n.LengthSquared() > 1e-20f) {n = Vector3.Normalize(n);}
                else {n = Vector3.UnitY;}

                outNormals[v * 3 + 0] = n.X;
                outNormals[v * 3 + 1] = n.Y;
                outNormals[v * 3 + 2] = n.Z;
            }
            
            return outNormals;
        }
    }
}
