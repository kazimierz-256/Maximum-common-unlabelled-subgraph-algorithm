using GraphDataStructure;
using MathParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SubgraphIsomorphismBenchmark
{
    class Program
    {
        private const string path = @"benchmark.csv";
        private static Func<int, int, double> criterion;
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter an optimization criterion (please make sure it is non-decreasing in 'vertices' and 'edges')");
            criterion = Parse.ParseInput(Console.ReadLine());

            File.WriteAllText(path, string.Empty);
            printBenchmark(2, 0.7m);
        }
        private const int oddIterations = 0;
        private static void printBenchmark(int n, decimal density)
        {
            var results = new List<TimeSpan>();
            for (int i = 1; i <= oddIterations * 2 + 1; i++)
            {
                results.Add(BenchmarkIsomorphism(n, (double)density, i));
            }
            results.Sort();
            var medianTime = results[results.Count / 2];
            Console.WriteLine($"{n}, {density}: Elapsed: {medianTime.TotalMilliseconds}ms");
            using (var sw = File.AppendText(path))
            {
                sw.WriteLine($"{n},{density},{medianTime.TotalMilliseconds}");
            }
            //if (density < 0.6m)
            //{
            //    printBenchmark(n, density + 0.05m);
            //}
            //else
            //{
            printBenchmark(n + 1, density);
            //}
            //printBenchmark(n, density + 0.01m);
        }

        private static TimeSpan BenchmarkIsomorphism(int n, double density, int seed)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, seed);
#if true
            var h = GraphFactory.GenerateRandom(n, density, -seed - seed * seed);
#else
            var h = GraphFactory.GeneratePermuted(g, 0);
#endif

            // run the algorithm
            var solver = new SubgraphIsomorphismExactAlgorithm.ParallelLeverager<double>();
            sw.Start();
            solver.ExtractOptimalSubgraph(g, h, criterion, 0, out double score, out var gToH, out var hToG);
            sw.Stop();
            return sw.Elapsed;
        }
    }
}
