using System;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
    public static class Compositor
    {
        private static Shader _shader = null!;
        private static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            _shader = new Shader("Composite.vert", "Composite.frag");
            _loaded = true;
        }

        public static void DrawToScreen(int fullscreenVao, int bgTexId, int worldTexId, int screenWidth, int screenHeight)
        {
            if (!_loaded) throw new InvalidOperationException("Compositor not loaded. Call Compositor.Load().");

            screenWidth = Math.Max(1, screenWidth);
            screenHeight = Math.Max(1, screenHeight);

            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.Viewport(0, 0, screenWidth, screenHeight);

            GL.Disable(EnableCap.DepthTest);

            _shader.Use();

            // Texture units
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, bgTexId);
            _shader.SetInt("uBg", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, worldTexId);
            _shader.SetInt("uWorld", 1);

            GL.BindVertexArray(fullscreenVao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
        }
    }
}
