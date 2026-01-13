using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
	public class Crosshair
	{
        static int crossVao;
        static int crossVbo;

        static Shader crosshairShader = null!;

		public static void LoadCrosshair()
		{
			crosshairShader = new Shader("Crosshair.vert", "Crosshair.frag");

            crossVao = GL.GenVertexArray();
            crossVbo = GL.GenBuffer();

            float size = 0.025f;

            float[] crossVertices =
            {
                -size,  0f,   size,  0f,
                 0f,  -size,  0f,  size
            };

            GL.BindVertexArray(crossVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, crossVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, crossVertices.Length * sizeof(float), crossVertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
		}

		public static void DrawCrosshair(float width, float height)
		{
            GL.Disable(EnableCap.DepthTest);

            crosshairShader.Use();
            float aspect = width / height;
            crosshairShader.SetFloat("uAspect", aspect);
            GL.BindVertexArray(crossVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, 4);

            GL.Enable(EnableCap.DepthTest);
		}
	}
}