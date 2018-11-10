﻿#define approx1_
#define approx2
using GraphDataStructure;
using MathParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SubgraphIsomorphismBenchmark
{
    // this class is used to perform experiments and benchmarks so clean code is not a priority in this module

    class Program
    {
        private const string csvExactPath = @"benchmark.csv";
        private const string texExactPath = @"benchmark.tex";
        private const string csvApprox1Path = @"approximability1.csv";
        private const string texApprox1Path = @"approximability1.tex";
        private const string csvApprox2Path = @"approximability2.csv";
        private const string texApprox2Path = @"approximability2.tex";
        private static Func<int, int, double> criterion;

        static void Main(string[] args)
        {
            //Console.WriteLine("Please enter an optimization criterion (please make sure it is non-decreasing in 'vertices' and 'edges')");
            //criterion = Parse.ParseInput(Console.ReadLine());

            File.WriteAllText(csvExactPath, string.Empty);
            File.WriteAllText(texExactPath, string.Empty);
            File.WriteAllText(csvApprox1Path, string.Empty);
            File.WriteAllText(texApprox1Path, string.Empty);
            File.WriteAllText(csvApprox2Path, string.Empty);
            File.WriteAllText(texApprox2Path, string.Empty);

            PrintBenchmark(18);
        }
        private const int iterations = 0;
        private static void PrintBenchmark(int n)
        {
            using (var texWriter = File.AppendText(texExactPath))
                texWriter.Write($"{n}&{n}");
            using (var texWriter = File.AppendText(texApprox1Path))
                texWriter.Write($"{n}&{n}");
            using (var texWriter = File.AppendText(texApprox2Path))
                texWriter.Write($"{n}&{n}");

            for (double density = 0.01d; density <= 1d; density += 0.02d)
            //var density = 0.5d;
            {
                var print = false;
                var msTime = 0d;
                var approximation1QualityString = string.Empty;
                var approximation2QualityString = string.Empty;
                for (int i = 1; i <= iterations * 2 + 1; i++)
                {
#if (approx1 || approx2)

                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write("APPROXIMATE");
                    Console.ResetColor();
                    Console.WriteLine(".");
#endif
#if (approx1)
                    var aMsTime1 = BenchmarkIsomorphism(1, n, density, i, out var approximate1SubgraphVertices, out var approximate1SubgraphEdges, out var approximate1Score, print).TotalMilliseconds;
                    Console.Write($"{aMsTime1:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
                    Console.WriteLine($"score: {approximate1Score}");
                    Console.WriteLine();
#endif
#if (approx2)
                    var aMsTime2 = BenchmarkIsomorphism(2, n, density, i, out var approximate2SubgraphVertices, out var approximate2SubgraphEdges, out var approximate2Score, print).TotalMilliseconds;
                    Console.Write($"{aMsTime2:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
                    Console.WriteLine($"score: {approximate2Score}");
                    Console.WriteLine();
#endif
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("EXACT");
                    Console.ResetColor();
                    Console.WriteLine(".");

                    msTime = BenchmarkIsomorphism(0, n, density, i, out var subgraphVertices, out var subgraphEdges, out var score, print).TotalMilliseconds;
                    Console.WriteLine($"{msTime:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
#if (approx1)

                    Console.Write($"Quality of approximation 1: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    approximation1QualityString = string.Format($"{ 100d * approximate1Score / score:F1}");
                    Console.Write($"{approximation1QualityString}%");
                    Console.ResetColor();
                    Console.WriteLine($", approximate {approximate1Score}, exact {score}");
#endif
#if (approx2)

                    Console.Write($"Quality of approximation 2: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    approximation2QualityString = string.Format($"{ 100d * approximate2Score / score:F1}");
                    Console.Write($"{approximation2QualityString}%");
                    Console.ResetColor();
                    Console.WriteLine($", approximate {approximate2Score}, exact {score}");
#endif

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                }

                using (var csvWriter = File.AppendText(csvExactPath))
                    csvWriter.WriteLine($"{n},{density},{msTime}");
                using (var texWriter = File.AppendText(texExactPath))
                    texWriter.Write($"&{msTime:F1}ms");

                using (var csvWriter = File.AppendText(csvApprox1Path))
                    csvWriter.WriteLine($"{n},{density},{approximation1QualityString}");
                using (var texWriter = File.AppendText(texApprox1Path))
                    texWriter.Write($"&{approximation1QualityString}\\% ");

                using (var csvWriter = File.AppendText(csvApprox2Path))
                    csvWriter.WriteLine($"{n},{density},{approximation2QualityString}");
                using (var texWriter = File.AppendText(texApprox2Path))
                    texWriter.Write($"&{approximation2QualityString}\\% ");
            }

            Console.WriteLine();

            using (var texWriter = File.AppendText(texExactPath))
                texWriter.WriteLine($"\\\\\\hline");
            using (var texWriter = File.AppendText(texApprox1Path))
                texWriter.WriteLine($"\\\\\\hline");
            using (var texWriter = File.AppendText(texApprox2Path))
                texWriter.WriteLine($"\\\\\\hline");

            PrintBenchmark(n + 1);
        }

        private static TimeSpan BenchmarkIsomorphism(int algorithm, int n, double density, int seed, out int subgraphVertices, out int subgraphEdges, out double score, bool printGraphs = false)
        {
            var sw = new Stopwatch();
            var g = GraphFactory.GenerateRandom(n, density, (int)(123456789 * density) + seed * n - seed * seed).Permute(seed * (seed * seed - 1));
            var h = GraphFactory.GenerateRandom(n, density, (int)(987654321 * density) - seed * n - seed * seed).Permute(seed * seed);
            var gToH = new Dictionary<int, int>();
            var hToG = new Dictionary<int, int>();

            Func<int, int, double> valuation = (v, e) => v + e;
            var disconnected = false;

            // run the algorithm
            if (algorithm == 0)
            {
                sw.Start();
                SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                    g,
                    h,
                    valuation,
                    out score,
                    out subgraphEdges,
                    out gToH,
                    out hToG,
                    disconnected,
                    false
                    );
                sw.Stop();

                subgraphVertices = gToH.Keys.Count;
            }
            else if (algorithm == 1)
            {
                sw.Start();
                SubgraphIsomorphismExactAlgorithm.SerialSubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                    g,
                    h,
                    valuation,
                    out score,
                    out subgraphEdges,
                    out gToH,
                    out hToG,
                    disconnected,
                    false
                    );
                sw.Stop();

                subgraphVertices = gToH.Keys.Count;
            }
            else if (algorithm == 2)
            {
                sw.Start();
                SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                    g,
                    h,
                    valuation,
                    out score,
                    out subgraphEdges,
                    out gToH,
                    out hToG,
                    disconnected,
                    false,
                    (Math.Min(g.EdgeCount, h.EdgeCount) + Math.Min(g.Vertices.Count, h.Vertices.Count)) * 30,
                    0
                    );
                sw.Stop();

                subgraphVertices = gToH.Keys.Count;
            }
            else
            {
                throw new Exception("Wrong algorithm");
            }

            if (printGraphs)
            {
                var light = algorithm == 0 ? ConsoleColor.Green : ConsoleColor.Cyan;
                var dark = algorithm == 0 ? ConsoleColor.DarkGreen : ConsoleColor.DarkCyan;

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
