using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace OpenCraft
{
    public class Shader : IDisposable
    {
        private readonly int handle;
        private readonly Dictionary<string, int> uniformLocations = new();

        public Shader(string vertexFile, string fragmentFile)
        {
            handle = CreateProgram(vertexFile, fragmentFile);
        }

        public void Use()
        {
            GL.UseProgram(handle);
        }

        public void SetMatrix4(string name, Matrix4 value, bool transpose = false)
        {
            int location = GetUniformLocation(name);
            if (location != -1)
                GL.UniformMatrix4(location, transpose, ref value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            int location = GetUniformLocation(name);
            if (location != -1)
                GL.Uniform3(location, value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            int location = GetUniformLocation(name);
            if (location != -1)
                GL.Uniform4(location, value);
        }

        public void SetInt(string name, int value)
        {
            int location = GetUniformLocation(name);
            if (location != -1)
                GL.Uniform1(location, value);
        }

        public void SetFloat(string name, float value)
        {
            int location = GetUniformLocation(name);
            if (location != -1)
                GL.Uniform1(location, value);
        }

        private int GetUniformLocation(string name)
        {
            if (uniformLocations.TryGetValue(name, out int location))
                return location;

            location = GL.GetUniformLocation(handle, name);
            uniformLocations[name] = location;
            return location;
        }

        private static int CreateProgram(string vertexFile, string fragmentFile)
        {
            int program = GL.CreateProgram();

            int vertexShader = CompileShader(ShaderType.VertexShader, Path.Combine("shaders", vertexFile));
            int fragmentShader = CompileShader(ShaderType.FragmentShader, Path.Combine("shaders", fragmentFile));

            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }

        private static int CompileShader(ShaderType type, string path)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, File.ReadAllText(path));
            GL.CompileShader(shader); 
            return shader;
        }

        public void Dispose()
        {
            GL.DeleteProgram(handle);
        }
    }
}
