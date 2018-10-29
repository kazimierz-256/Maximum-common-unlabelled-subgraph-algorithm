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
            PrintBenchmark(21);
        }
        private const int iterations = 1;
        private static void PrintBenchmark(int n)
        {
            using (var texWriter = File.AppendText(texPath))
                texWriter.Write($"{n}&{n}");

            //for (double density = 0.05d; density < 1d; density += 0.05d)
            var density = 0.5d;
            {
                var msTime = 0d;
                //var times = new List<double>();
                for (int i = 1; i <= iterations * 2 + 1; i++)
                {

                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write("APPROXIMATE");
                    Console.ResetColor();
                    Console.WriteLine(".");

                    var aMsTime = BenchmarkIsomorphism(false, n, density, i, out var approximateSubgraphVertices, out var approximateSubgraphEdges, out var approximateScore).TotalMilliseconds;
                    Console.Write($"{aMsTime:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");

                    //for (int nl = 0; nl < 1; nl++)
                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("EXACT");
                    Console.ResetColor();
                    Console.WriteLine(".");

                    msTime = BenchmarkIsomorphism(true, n, density, i, out var subgraphVertices, out var subgraphEdges, out var score).TotalMilliseconds;
                    Console.WriteLine($"{msTime:F2}ms,   ".PadLeft(20));
                    //Console.WriteLine($"vertices: {n}, density: { density}");

                    Console.Write($"Quality of approximation: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"{100d * approximateScore / score:F1}%");
                    Console.ResetColor();
                    Console.WriteLine($", approximate {approximateScore}, exact {score}");
                    //for (int nl = 0; nl < 2; nl++)
                    Console.WriteLine();
                }

                //times.Sort();
                //msTime = times[times.Count / 2];

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

        private static TimeSpan BenchmarkIsomorphism(bool exact, int n, double density, int seed, out int subgraphVertices, out int subgraphEdges, out double score, bool printGraphs = false)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, 365325556 + seed - seed * seed);
            var h = GraphFactory.GenerateRandom(n, 1-density, 129369567 - seed - seed * seed);
            var gToH = new Dictionary<int, int>();
            var hToG = new Dictionary<int, int>();
            //var h = GraphFactory.GeneratePermuted(g, 0);

            // run the algorithm
            sw.Start();
            if (exact)
            {
                SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                    g,
                    h,
                    (v, e) => v,
                    out score,
                    out subgraphEdges,
                    out gToH,
                    out hToG,
                    false,
                    false
                    );
                sw.Stop();

                subgraphVertices = gToH.Keys.Count;
            }
            else
            {
                SubgraphIsomorphismExactAlgorithm.SerialSubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                    g,
                    h,
                    (v, e) => v,
                    out score,
                    out subgraphEdges,
                    out gToH,
                    out hToG,
                    false,
                    false
                    );
                sw.Stop();

                subgraphVertices = gToH.Keys.Count;
            }

            if (printGraphs)
            {
                Console.WriteLine("Graph G:");
                g.PrintSubgraph(gToH.Keys.ToArray(), gToH, ConsoleColor.DarkCyan, ConsoleColor.Cyan);
                Console.WriteLine();

                Console.WriteLine("Graph H:");
                h.PrintSubgraph(gToH.Keys.Select(key => gToH[key]).ToArray(), hToG, ConsoleColor.DarkCyan, ConsoleColor.Cyan);
                Console.WriteLine();
            }
            return sw.Elapsed;
        }
    }
}
