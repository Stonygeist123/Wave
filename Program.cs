using Wave.Repl;

namespace Wave
{
    internal static class Program
    {
        static void Main()
        {
            WaveRepl repl = new();
            repl.Run();
        }
    }
}