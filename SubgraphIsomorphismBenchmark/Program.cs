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
        private const string csvExactPath = @"benchmark.csv";
        private const string texExactPath = @"benchmark.tex";
        private const string csvApproxPath = @"approximability.csv";
        private const string texApproxPath = @"approximability.tex";
        private static Func<int, int, double> criterion;
        static void Main(string[] args)
        {
            //Console.WriteLine("Please enter an optimization criterion (please make sure it is non-decreasing in 'vertices' and 'edges')");
            //criterion = Parse.ParseInput(Console.ReadLine());

            File.WriteAllText(csvExactPath, string.Empty);
            File.WriteAllText(texExactPath, string.Empty);
            File.WriteAllText(csvApproxPath, string.Empty);
            File.WriteAllText(texApproxPath, string.Empty);

            PrintBenchmark(21);
        }
        private const int iterations = 0;
        private static void PrintBenchmark(int n)
        {
            using (var texWriter = File.AppendText(texExactPath))
                texWriter.Write($"{n}&{n}");
            using (var texWriter = File.AppendText(texApproxPath))
                texWriter.Write($"{n}&{n}");

            for (double density = 0.3d; density <= 0.7d; density += 0.1d)
            //var density = 0.5d;
            {
                var print = true;
                var msTime = 0d;
                var approximationQualityString = string.Empty;
                for (int i = 1; i <= iterations * 2 + 1; i++)
                {

                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write("APPROXIMATE");
                    Console.ResetColor();
                    Console.WriteLine(".");

                    var aMsTime = BenchmarkIsomorphism(false, n, density, i, out var approximateSubgraphVertices, out var approximateSubgraphEdges, out var approximateScore, print).TotalMilliseconds;
                    Console.Write($"{aMsTime:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
                    //Console.WriteLine(approximateScore);

                    Console.WriteLine();

                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("EXACT");
                    Console.ResetColor();
                    Console.WriteLine(".");

                    msTime = BenchmarkIsomorphism(true, n, density, i, out var subgraphVertices, out var subgraphEdges, out var score, print).TotalMilliseconds;
                    Console.WriteLine($"{msTime:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");

                    Console.Write($"Quality of approximation: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    approximationQualityString = string.Format($"{ 100d * approximateScore / score:F1}");
                    Console.Write($"{approximationQualityString}%");
                    Console.ResetColor();
                    Console.WriteLine($", approximate {approximateScore}, exact {score}");

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                }

                using (var csvWriter = File.AppendText(csvExactPath))
                    csvWriter.WriteLine($"{n},{density},{msTime}");

                using (var csvWriter = File.AppendText(csvApproxPath))
                    csvWriter.WriteLine($"{n},{density},{approximationQualityString}");

                using (var texWriter = File.AppendText(texExactPath))
                    texWriter.Write($"&{msTime:F1}ms");

                using (var texWriter = File.AppendText(texApproxPath))
                    texWriter.Write($"&{approximationQualityString}\\% ");
            }
            Console.WriteLine();
            using (var texWriter = File.AppendText(texExactPath))
                texWriter.WriteLine($"\\\\\\hline");
            using (var texWriter = File.AppendText(texApproxPath))
                texWriter.WriteLine($"\\\\\\hline");
            PrintBenchmark(n + 1);
        }

        private static TimeSpan BenchmarkIsomorphism(bool exact, int n, double density, int seed, out int subgraphVertices, out int subgraphEdges, out double score, bool printGraphs = false)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, 365325556 + seed - seed * seed).Permute(seed * (seed * seed - 1));
            var h = GraphFactory.GenerateRandom(n, density, 129369567 - seed - seed * seed).Permute(seed * seed);
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
                var light = exact ? ConsoleColor.Green : ConsoleColor.Cyan;
                var dark = exact ? ConsoleColor.DarkGreen : ConsoleColor.DarkCyan;

                Console.WriteLine("Graph G:");
                g.PrintSubgraph(gToH.Keys.ToArray(), gToH, dark, light);
                Console.WriteLine();

                Console.WriteLine("Graph H:");
                h.PrintSubgraph(gToH.Keys.Select(key => gToH[key]).ToArray(), hToG, dark, light);
                Console.WriteLine();
            }
            return sw.Elapsed;
        }
    }
}
