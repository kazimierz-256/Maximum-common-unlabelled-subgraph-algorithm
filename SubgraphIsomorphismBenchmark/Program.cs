using GraphDataStructure;
using MathParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SubgraphIsomorphismBenchmark
{
    class Program
    {
        private const string csvPath = @"benchmark.csv";
        private const string texPath = @"benchmark.tex";
        private static Func<int, int, double> criterion;
        static void Main(string[] args)
        {
            //Console.WriteLine("Please enter an optimization criterion (please make sure it is non-decreasing in 'vertices' and 'edges')");
            //criterion = Parse.ParseInput(Console.ReadLine());

            File.WriteAllText(csvPath, string.Empty);
            File.WriteAllText(texPath, string.Empty);
            PrintBenchmark(12);
        }
        private const int iterations = 0;
        private static void PrintBenchmark(int n)
        {
            using (var texWriter = File.AppendText(texPath))
                texWriter.Write($"{n}&{n}");

            //for (double density = 0.05d; density < 1d; density += 0.05d)
            var density = 0.6d;
            {
                var msTime = 0d;
                var times = new List<double>();
                for (int i = 1; i <= iterations * 2 + 1; i++)
                {
                    times.Add(BenchmarkIsomorphism(n, density, i, out var subgraphVertices, out var subgraphEdges).TotalMilliseconds);
                }

                times.Sort();
                msTime = times[times.Count / 2];

                Console.Write($"{msTime:F2}ms,   ".PadLeft(20));
                Console.WriteLine($"vertices: {n}, density: { density}");

                using (var csvWriter = File.AppendText(csvPath))
                    csvWriter.WriteLine($"{n},{density},{msTime}");

                using (var texWriter = File.AppendText(texPath))
                    texWriter.Write($"&{msTime:F1}ms");
            }
            Console.WriteLine();
            using (var texWriter = File.AppendText(texPath))
                texWriter.WriteLine($"\\\\\\hline");
            PrintBenchmark(n + 1);
        }

        private static TimeSpan BenchmarkIsomorphism(int n, double density, int seed, out int subgraphVertices, out int subgraphEdges)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, 36532556 + seed - seed * seed);
            var h = GraphFactory.GenerateRandom(n, 1 - density, 123698567 - seed - seed * seed);
            //var h = GraphFactory.GeneratePermuted(g, 0);

            // run the algorithm
            sw.Start();
            SubgraphIsomorphismExactAlgorithm.SerialSubgraphIsomorphismApproximator.ApproximateOptimalSubgraph(
                5,
                g,
                h,
                (v, e) => v,
                0,
                out var score,
                out var edges,
                out var gToH,
                out var hToG,
                false,
                false
                );
            sw.Stop();

            Console.WriteLine("Graf G:");
            g.PrintSubgraph(gToH.Keys.ToArray(), gToH);

            Console.WriteLine();
            Console.WriteLine("Graf H:");
            h.PrintSubgraph(gToH.Keys.Select(key => gToH[key]).ToArray(), hToG);

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            subgraphVertices = gToH.Keys.Count;
            subgraphEdges = edges;
            return sw.Elapsed;
        }
    }
}
