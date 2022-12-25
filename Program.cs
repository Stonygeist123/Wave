namespace Wave
{
    internal class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("> ");
                string? line = Console.ReadLine();
                if (line is null || line.Trim() == "")
                    continue;

                if (line == "1 + 2 + 3")
                    Console.WriteLine(7);
                else
                    Console.WriteLine("Invalid Expression.");
            }
        }
    }
}