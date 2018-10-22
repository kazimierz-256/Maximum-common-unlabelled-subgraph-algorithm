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
            //Console.WriteLine("Please enter an optimization criterion (please make sure it is non-decreasing in 'vertices' and 'edges')");
            //criterion = Parse.ParseInput(Console.ReadLine());

            File.WriteAllText(path, string.Empty);
            PrintBenchmark(2);
        }
        private const int oddIterations = 0;
        private static void PrintBenchmark(int n)
        {
            //var results = new List<TimeSpan>();
            for (int i = 1; i <= oddIterations * 2 + 1; i++)
            {
                for (double density = 0.1d; density < 1d; density += 0.1d)
                {
                    //results.Add(
                    var time = BenchmarkIsomorphism(n, density, i);
                    //);
                    Console.Write($"{time.TotalMilliseconds:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
                    using (var sw = File.AppendText(path))
                    {
                        sw.WriteLine($"{n},{density},{time.TotalMilliseconds}");
                    }
                }
                Console.WriteLine();
            }
            //results.Sort();
            //var medianTime = results[results.Count / 2];
            //Console.WriteLine($"{n}, {density}: Elapsed: {medianTime.TotalMilliseconds}ms");
            //if (density < 0.6m)
            //{
            //    printBenchmark(n, density + 0.05m);
            //}
            //else
            //{
            PrintBenchmark(n + 1);
            //}
            //printBenchmark(n, density + 0.01m);
        }

        private static TimeSpan BenchmarkIsomorphism(int n, double density, int seed)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, 363256 + seed - seed * seed);
            var h = GraphFactory.GenerateRandom(n, density, 123998567 - seed - seed * seed);
            //var h = GraphFactory.GeneratePermuted(g, 0);

            // run the algorithm
            sw.Start();
            SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor<double>.ExtractOptimalSubgraph(
                g,
                h,
                (v, e) => v,
                0,
                out double score,
                out var gToH,
                out var hToG,
                false,
                false
                );
            sw.Stop();
            return sw.Elapsed;
        }
    }
}
