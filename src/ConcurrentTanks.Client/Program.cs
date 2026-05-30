using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System.Drawing;
using System.Numerics;

namespace ConcurrentTanks.Client
{
    public class Program
    {
        private static IWindow _window;
        private static GL _gl;
        private static uint _vao;
        private static uint _vbo;
        private static uint _ebo;
        private static uint _program;
        private static Matrix4x4 _projection;
        private static int _projectionLocation;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World 2!");

            WindowOptions options = WindowOptions.Default with
            {
                Size = new Vector2D<int>(900, 900),
                Title = "Concurrent Tanks",
                WindowBorder = WindowBorder.Fixed
            };

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.Run();
        }

        private static unsafe void OnLoad()
        {
            IInputContext input = _window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            input.Keyboards[i].KeyDown += KeyDown;
            _gl = _window.CreateOpenGL();

            _projection =
                Matrix4x4.CreateOrthographicOffCenter(
                    0f,     // left
                    900f,   // right
                    900f,   // bottom
                    0f,     // top
                    -1f,
                    1f);

            _gl.ClearColor(Color.CornflowerBlue);

            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            float[] vertices =
            {
                400f, 435f, 0f, // top left
                465f, 435f, 0f, // top right
                465f, 495f, 0f, // bottom right
                400f, 495f, 0f  // bottom left
            };
            
            _vbo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            fixed (float* buf = vertices)
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint) (vertices.Length * sizeof(float)),
                    buf, BufferUsageARB.StaticDraw);

            uint[] indices =
            {
                0, 1, 3,
                1, 2, 3
            };

            _ebo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            
            fixed (uint* buf = indices)
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint) (indices.Length * sizeof(uint)), 
                    buf, BufferUsageARB.StaticDraw);

            const string vertexCode = @"
#version 330 core

layout (location = 0) in vec3 aPosition;

uniform mat4 projection;

void main()
{
    gl_Position =
        projection *
        vec4(aPosition, 1.0);
}";

        const string fragmentCode = @"
#version 330 core

out vec4 out_color;

void main()
{
    out_color = vec4(1.0, 1.0, 1.0, 1.0);
}";

            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexCode);

            _gl.CompileShader(vertexShader);

            _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int) GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + _gl.GetShaderInfoLog(vertexShader));

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentCode);

            _gl.CompileShader(fragmentShader);

            _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int) GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + _gl.GetShaderInfoLog(fragmentShader));

            _program = _gl.CreateProgram();

            _gl.AttachShader(_program, vertexShader);
            _gl.AttachShader(_program, fragmentShader);

            _gl.LinkProgram(_program);

            int projectionLocation =
                _gl.GetUniformLocation(
                    _program,
                    "projection");
                    
            _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int lStatus);
            if (lStatus != (int) GLEnum.True)
                throw new Exception("Program failed to link: " + _gl.GetProgramInfoLog(_program));

            _gl.DetachShader(_program, vertexShader);
            _gl.DetachShader(_program, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            const uint positionLoc = 0;
            _gl.EnableVertexAttribArray(positionLoc);
            _gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*) 0);

            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);

            _gl.Viewport(_window.FramebufferSize);
        }

        private static void OnUpdate(double deltaTime) { }

        private static unsafe void OnRender(double deltaTime)
        {
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            _gl.UseProgram(_program);

            fixed (float* p = &_projection.M11)
            {
                _gl.UniformMatrix4(
                    _projectionLocation,
                    1,
                    false,
                    p);
            }

            _gl.BindVertexArray(_vao);

            _gl.DrawElements(
                PrimitiveType.Triangles,
                6,
                DrawElementsType.UnsignedInt,
                (void*)0);
        }

        private static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
        {
            if (key == Key.Escape)
                _window.Close();
        }
    }
}
