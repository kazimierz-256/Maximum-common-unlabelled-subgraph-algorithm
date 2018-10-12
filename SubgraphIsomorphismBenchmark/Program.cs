using System;

namespace SubgraphIsomorphismBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            for (long i = 0; i < long.MaxValue; i += 1)
            {
                i *= 2;
                i += 2;
                i /= 2;
                i -= 1;
                if (i % int.MaxValue == 0)
                {
                    Console.WriteLine(i);
                }
            }
            Console.WriteLine("Hello World!");
        }
    }
}
