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
        private static float _tankX = 450f;
        private static float _tankY = 450f;
        private static int _modelLocation;
        private static bool _moveLeft;
        private static bool _moveRight;
        private static bool _moveUp;
        private static bool _moveDown;
        private static int _objectColorLocation;
	    private static int _trackOffset;

        static void Main(string[] args)
        {
            Console.WriteLine("Launch the game");

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
            {
                input.Keyboards[i].KeyDown += KeyDown;
                input.Keyboards[i].KeyUp += KeyUp;
            }
                
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
                -2f,  2f, 0f, // bottom left
                2f,  2f, 0f, // bottom right
                2f, -2f, 0f, // top right
                -2f, -2f, 0f // top left
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
uniform mat4 model;

void main()
{
    gl_Position =
        projection *
        model *
        vec4(aPosition, 1.0);
}";

        const string fragmentCode = @"
#version 330 core

uniform vec4 objectColor;
out vec4 out_color;

void main()
{   
    out_color = objectColor;
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

            _projectionLocation =
                _gl.GetUniformLocation(
                    _program,
                    "projection");
                    
            _modelLocation =
                _gl.GetUniformLocation(_program, "model");

            _objectColorLocation =
                _gl.GetUniformLocation(
                    _program,
                    "objectColor");

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


        private static void OnUpdate(double deltaTime)
        {
            float speed = 200f;

            if (_moveLeft)
                _tankX -= speed * (float)deltaTime;

            if (_moveRight)
                _tankX += speed * (float)deltaTime;

            if (_moveUp)
                _tankY -= speed * (float)deltaTime;

            if (_moveDown)
                _tankY += speed * (float)deltaTime;

            if (_moveLeft ||
                _moveRight ||
                _moveUp ||
                _moveDown)
            {
                _trackOffset++;
            }
        }

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

            DrawTrack(_tankX, _tankY, _trackOffset);
            DrawTrack(_tankX + 44f, _tankY, _trackOffset);

            var horizontalHullModel =
                Matrix4x4.CreateScale(
                    8f,
                    4f,
                    1f)
                *
                Matrix4x4.CreateTranslation(
                    _tankX + 22f,
                    _tankY + 22f,
                    0f);
            
            _gl.Uniform4(
                _objectColorLocation,
                1f,
                1f,
                1f,
                1f);

            DrawRectangle(horizontalHullModel);

            var verticalHullModel = 
                Matrix4x4.CreateScale(
                    6f,
                    8f,
                    1f)
                *
                Matrix4x4.CreateTranslation(
                    _tankX + 22f,
                    _tankY + 20f,
                    0f);
                    
            DrawRectangle(verticalHullModel);

            var turretModel =
                Matrix4x4.CreateScale(
                    5f,
                    6f,
                    1f)
                *
                Matrix4x4.CreateTranslation(
                    _tankX + 22f,
                    _tankY + 20f,
                    0f);

            _gl.Uniform4(
                _objectColorLocation,
                0.9f,
                0.9f,
                0.9f,
                1f);

            DrawRectangle(turretModel);

            var gunBarrelModel =
                Matrix4x4.CreateScale(
                    1f,
                    6f,
                    1f)
                *
                Matrix4x4.CreateTranslation(
                    _tankX + 22f,
                    _tankY + 0f,
                    0f);

            DrawRectangle(gunBarrelModel);
        }

        private static void DrawTrack(float tankX, float tankY, int trackOffset)
        {
            for (int segment = 0; segment < 10; segment++)
            {
                var segmentModel =
                    Matrix4x4.CreateScale(
                        3f,
                        1f,
                        1f)
                    *
                    Matrix4x4.CreateTranslation(
                        tankX,
                        tankY + segment * 4f,
                        0f);

                bool grey =
                    ((segment + trackOffset) % 2) == 0;

                if (grey)
                {
                    _gl.Uniform4(
                        _objectColorLocation,
                        0.5f,
                        0.5f,
                        0.5f,
                        1f);
                }
                else
                {
                    _gl.Uniform4(
                        _objectColorLocation,
                        1f,
                        1f,
                        1f,
                        1f);
                }

                DrawRectangle(segmentModel);
            }
        }

        private static unsafe void DrawRectangle(in Matrix4x4 model)
        {
            fixed (float* p = &model.M11)
            {
                _gl.UniformMatrix4(
                    _modelLocation,
                    1,
                    false,
                    p);
            }

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

            if (key == Key.W)
                _moveUp = true;

            if (key == Key.S)
                _moveDown = true;

            if (key == Key.A)
                _moveLeft = true;

            if (key == Key.D)
                _moveRight = true;
        }

        private static void KeyUp(IKeyboard keyboard, Key key, int keyCode)
        {    
            if (key == Key.W)
                _moveUp = false;

            if (key == Key.S)
                _moveDown = false;

            if (key == Key.A)
                _moveLeft = false;

            if (key == Key.D)
                _moveRight = false;
        }
    }
}
