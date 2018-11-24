#define approx1
#define approx2
#define exact
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

            PrintBenchmark(20);
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

            foreach (var density in new double[] { 0.05, 0.1, 0.2, 0.35, 0.5, 0.65, 0.8, 0.9, 0.95 })
            {
                var print = false;
                var msTime = 0d;
                var approximation1QualityString = string.Empty;
                var approximation2QualityString = string.Empty;
                for (int i = 1; i <= iterations * 2 + 1; i += 1)
                {
#if (approx1 || approx2)

                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkCyan;
                    Console.Write("APPROXIMATE");
                    Console.ResetColor();
                    Console.WriteLine(".");
#endif
                    var aMsTime2 = 500d;
#if (approx2)
                    Console.WriteLine("Limited recursion algorithm");
                    aMsTime2 = BenchmarkIsomorphism(2, n, density, i, out var approximate2SubgraphVertices, out var approximate2SubgraphEdges, out var approximate2Score, print).TotalMilliseconds;
                    Console.Write($"{aMsTime2:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
                    Console.WriteLine($"score: {approximate2Score}");
                    Console.WriteLine();
#endif
#if (approx1)
                    Console.WriteLine("Randomized approximation algorithm");
                    var aMsTime1 = BenchmarkIsomorphism(1, n, density, i, out var approximate1SubgraphVertices, out var approximate1SubgraphEdges, out var approximate1Score, print, timeout: aMsTime2).TotalMilliseconds;
                    Console.Write($"{aMsTime1:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
                    Console.WriteLine($"score: {approximate1Score}");
                    Console.WriteLine();
#endif
#if (exact)
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.Write("EXACT");
                    Console.ResetColor();
                    Console.WriteLine(".");

                    msTime = BenchmarkIsomorphism(0, n, density, i, out var subgraphVertices, out var subgraphEdges, out var score, print).TotalMilliseconds;
                    Console.WriteLine($"{msTime:F2}ms,   ".PadLeft(20));
                    Console.WriteLine($"vertices: {n}, density: { density}");
#endif
#if (approx2 && exact)

                    Console.Write($"Quality of Limited recursion algorithm: ");
                    Console.ForegroundColor = approximate2Score == score ? ConsoleColor.DarkGreen : ConsoleColor.DarkYellow;
                    approximation2QualityString = string.Format($"{ 100d * approximate2Score / score:F1}");
                    Console.Write($"{approximation2QualityString}%");
                    Console.ResetColor();
                    Console.WriteLine($", approximate {approximate2Score}, exact {score}");
#endif
#if (approx1 && exact)

                    Console.Write($"Quality of Randomized approximation algorithm: ");
                    Console.ForegroundColor = approximate1Score == score ? ConsoleColor.DarkGreen : ConsoleColor.DarkYellow;
                    approximation1QualityString = string.Format($"{ 100d * approximate1Score / score:F1}");
                    Console.Write($"{approximation1QualityString}%");
                    Console.ResetColor();
                    Console.WriteLine($", approximate {approximate1Score}, exact {score}");
#endif
#if (approx1 && approx2)
                    switch (Math.Sign(approximate1Score - approximate2Score))
                    {
                        case -1:
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.Write($"Limited Recursion wins by {(approximate2Score - approximate1Score) * 100d / approximate1Score:F1}%");
                            break;
                        case 0:
                            //Console.ForegroundColor = ConsoleColor.Gray;
                            //Console.Write($"Approximating algorithms draw!");
                            break;
                        case 1:
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"Randomized choice wins by {(approximate1Score - approximate2Score) * 100d / approximate2Score:F1}%");
                            break;
                    }
                    Console.ResetColor();
#endif

                    Console.WriteLine();
                    Console.WriteLine();
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

        private static TimeSpan BenchmarkIsomorphism(int algorithm, int n, double density, int seed, out int subgraphVertices, out int subgraphEdges, out double score, bool printGraphs = false, double timeout = 0d)
        {
            var sw = new Stopwatch();
            var initialSeed = new Random(seed).Next() ^ new Random(n).Next() ^ new Random((int)(density * int.MaxValue)).Next();
            var g = GraphFactory.GenerateRandom(n, density, new Random(0).Next() ^ initialSeed).Permute(2);
            var h = GraphFactory.GenerateRandom(n, density, new Random(1).Next() ^ initialSeed).Permute(3);
            var gToH = new Dictionary<int, int>();
            var hToG = new Dictionary<int, int>();

            Func<int, int, double> valuation = (v, e) => e + v;
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
                    false,
                    approximationRatio: 1d
                    );
                sw.Stop();

                subgraphVertices = gToH.Keys.Count;
            }
            else if (algorithm == 1)
            {
                sw.Start();
                SubgraphIsomorphismExactAlgorithm.SubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                    g,
                    h,
                    valuation,
                    out score,
                    out subgraphEdges,
                    out gToH,
                    out hToG,
                    disconnected,
                    false,
                    1000
                    , timeout
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
                    Math.Min(g.Vertices.Count, h.Vertices.Count) * 100,
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
