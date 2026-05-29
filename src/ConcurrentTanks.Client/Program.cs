using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace ConcurrentTanks.Client
{
    public class Program
    {
        private static IWindow _window;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World 2!");

            WindowOptions options = WindowOptions.Default with
            {
                Size = new Vector2D<int>(800, 600),
                Title = "My first Silk.NET application!"
            };

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.Run();
        }

        private static void OnLoad() { }

        private static void OnUpdate(double deltaTime) { }

        private static void OnRender(double deltaTime) { }
    }
}
