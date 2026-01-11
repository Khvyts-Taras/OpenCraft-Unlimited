using System;
using OpenTK.Graphics.OpenGL4;

namespace OpenCraft
{
    public sealed class WorldRenderTarget : IDisposable
    {
        private int _fbo;
        private int _colorTex;
        private int _depthRbo;

        private int _width;
        private int _height;

        public int TextureId => _colorTex;
        public int Width => _width;
        public int Height => _height;

        public WorldRenderTarget(int width, int height)
        {
            Resize(width, height);
        }

        public void Resize(int width, int height)
        {
            width = Math.Max(1, width);
            height = Math.Max(1, height);

            if (width == _width && height == _height && _fbo != 0)
                return;

            _width = width;
            _height = height;

            // Preserve bindings to avoid breaking other rendering
            GL.GetInteger(GetPName.FramebufferBinding, out int prevFbo);
            GL.GetInteger(GetPName.TextureBinding2D, out int prevTex);
            GL.GetInteger(GetPName.RenderbufferBinding, out int prevRbo);

            // Delete old
            if (_colorTex != 0) GL.DeleteTexture(_colorTex);
            if (_depthRbo != 0) GL.DeleteRenderbuffer(_depthRbo);
            if (_fbo != 0) GL.DeleteFramebuffer(_fbo);

            // Create color texture
            _colorTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _colorTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);


            // Depth renderbuffer
            _depthRbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent24, _width, _height);

            // FBO
            _fbo = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, _colorTex, 0);

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, _depthRbo);

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                throw new Exception($"World FBO incomplete: {status}");

            // Restore bindings
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, prevFbo);
            GL.BindTexture(TextureTarget.Texture2D, prevTex);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, prevRbo);
        }

        public void Begin()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _fbo);
            GL.Viewport(0, 0, _width, _height);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthMask(true);
            
            GL.ClearColor(0f, 0f, 0f, 0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void End()
        {
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        public void BlitToScreen(int screenWidth, int screenHeight)
        {
            screenWidth = Math.Max(1, screenWidth);
            screenHeight = Math.Max(1, screenHeight);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fbo);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            GL.BlitFramebuffer(
                0, 0, _width, _height,
                0, 0, screenWidth, screenHeight,
                ClearBufferMask.ColorBufferBit,
                BlitFramebufferFilter.Linear
            );

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        public void Dispose()
        {
            if (_colorTex != 0) GL.DeleteTexture(_colorTex);
            if (_depthRbo != 0) GL.DeleteRenderbuffer(_depthRbo);
            if (_fbo != 0) GL.DeleteFramebuffer(_fbo);

            _colorTex = 0;
            _depthRbo = 0;
            _fbo = 0;
        }
    }
}
