using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
    public static class Background
    {
        public static int _vao;
        
        private static int _vbo;
        private static int _ebo;

        private static int _fbo;
        private static int _colorTex;

        private static int _rtWidth;
        private static int _rtHeight;

        private static Shader _genShader = null!;
        private static bool _isLoaded;

        public static int TextureId => _colorTex;

        public static void Load(int width, int height)
        {
            if (_isLoaded) return;

            _rtWidth  = Math.Max(1, width);
            _rtHeight = Math.Max(1, height);

            // Full-screen quad: pos.xy + uv.xy
            float[] verts =
            {
                -1f, -1f,  0f, 0f,
                 1f, -1f,  1f, 0f,
                 1f,  1f,  1f, 1f,
                -1f,  1f,  0f, 1f
            };

            uint[] indices =
            {
                0, 1, 2,
                2, 3, 0
            };

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

            int stride = 4 * sizeof(float);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            // Generator shader (рендерить фон у FBO)
            _genShader = new Shader("Background.vert", "Background.frag");

            CreateRenderTarget(_rtWidth, _rtHeight);

            _isLoaded = true;
        }

        public static void Resize(int width, int height)
        {
            if (!_isLoaded) return;

            int w = Math.Max(1, width);
            int h = Math.Max(1, height);
            if (w == _rtWidth && h == _rtHeight) return;

            _rtWidth = w;
            _rtHeight = h;

            CreateRenderTarget(_rtWidth, _rtHeight);
        }

        public static void Draw(float pitch, int screenWidth, int screenHeight)
        {
            if (!_isLoaded)
                throw new InvalidOperationException("Background is not loaded. Call Background.Load(width,height) first.");

            screenWidth  = Math.Max(1, screenWidth);
            screenHeight = Math.Max(1, screenHeight);

            // Save only what is critical for correctness
            GL.GetInteger(GetPName.ReadFramebufferBinding, out int prevReadFbo);
            GL.GetInteger(GetPName.DrawFramebufferBinding, out int prevDrawFbo);

            int[] prevViewport = new int[4];
            GL.GetInteger(GetPName.Viewport, prevViewport);

            bool prevDepth = GL.IsEnabled(EnableCap.DepthTest);

            // -------------------------
            // PASS A: render-to-texture
            // -------------------------
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
            GL.Viewport(0, 0, _rtWidth, _rtHeight);

            // Для фону depth не потрібен
            GL.Disable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            _genShader.Use();
            _genShader.SetFloat("uPitch", pitch / 90f);
            _genShader.SetVector3("uTopColor", new Vector3(0.20f, 0.30f, 0.60f));
            _genShader.SetVector3("uBottomColor", new Vector3(0.90f, 0.80f, 0.70f));

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);

            // -----------------------------------
            // PASS B: blit to default framebuffer
            // -----------------------------------
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.Viewport(0, 0, screenWidth, screenHeight);

            GL.BlitFramebuffer(
                0, 0, _rtWidth, _rtHeight,
                0, 0, screenWidth, screenHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear
            );

            // Restore state
            if (prevDepth) GL.Enable(EnableCap.DepthTest);
            else GL.Disable(EnableCap.DepthTest);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, prevReadFbo);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, prevDrawFbo);
            GL.Viewport(prevViewport[0], prevViewport[1], prevViewport[2], prevViewport[3]);
        }

        public static void Unload()
        {
            if (!_isLoaded) return;

            if (_colorTex != 0) GL.DeleteTexture(_colorTex);
            if (_fbo != 0) GL.DeleteFramebuffer(_fbo);

            if (_ebo != 0) GL.DeleteBuffer(_ebo);
            if (_vbo != 0) GL.DeleteBuffer(_vbo);
            if (_vao != 0) GL.DeleteVertexArray(_vao);

            _colorTex = 0;
            _fbo = 0;
            _ebo = 0;
            _vbo = 0;
            _vao = 0;

            _isLoaded = false;
        }

        private static void CreateRenderTarget(int width, int height)
        {
            // Preserve bindings to avoid “чорні текстури” в інших місцях
            GL.GetInteger(GetPName.TextureBinding2D, out int prevTex);
            GL.GetInteger(GetPName.FramebufferBinding, out int prevFbo);

            if (_colorTex != 0) GL.DeleteTexture(_colorTex);
            if (_fbo != 0) GL.DeleteFramebuffer(_fbo);

            _colorTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _colorTex);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            _fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _colorTex, 0);

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"Background FBO incomplete: {status}");

            // Restore previous bindings
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
            GL.BindTexture(TextureTarget.Texture2D, prevTex);
        }
    }
}
