using GraphDataStructure;
using System;
using System.Diagnostics;

namespace SubgraphIsomorphismBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            printBenchmark(1, 0.9);
            printBenchmark(16, 0.9);
            printBenchmark(17, 0.9);
            printBenchmark(18, 0.9);
            printBenchmark(19, 0.9);
        }
        private const int iterations = 1;
        private static void printBenchmark(int n, double density)
        {
            var time = TimeSpan.Zero;
            for (int i = 0; i < iterations; i++)
            {
                time += BenchmarkIsomorphism(n, density, i);
            }
            Console.WriteLine($"{n}: Elapsed: {time.TotalMilliseconds / iterations}ms");
            printBenchmark(n+1, density);
        }

        private static TimeSpan BenchmarkIsomorphism(int n, double density, int seed)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, seed);
            var h = GraphFactory.GeneratePermuted(g, 0);

            // run the algorithm
            var solver = new SubgraphIsomorphismExactAlgorithm.AlphaSubgraphIsomorphismExtractor<int>();
            sw.Start();
            solver.Extract(g, h, (vertices, edges) => vertices + edges, 0, out int score, out var gToH, out var hToG);
            sw.Stop();
            return sw.Elapsed;
        }
    }
}
