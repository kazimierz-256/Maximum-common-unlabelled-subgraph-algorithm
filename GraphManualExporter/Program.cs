using Application;
using GraphDataStructure;
using SubgraphIsomorphismExactAlgorithm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GraphManualExporter
{
    class Program
    {
        private static bool doExport = true;
        static void Main(string[] args)
        {
            Graph g, h;
            GenerateCliquesConnectedByChain(10, 4, out g, out h);
            //GenerateRandomWithCycle(100, 10, out g, out h);
            //GenerateRandom09Petersen(1000, out g, out h);
            //GenerateRandom0908(24, 23, out g, out h);
            //GenerateClebschPetersen(out g, out h);
            //GenerateCopyWithRedundant(15, 6, out g, out h);

            Func<int, int, double> valuation = (v, e) => v + e;
            var disconnected = true;
            var time = new Stopwatch();
            time.Start();
#if false
            ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                g,
                h,
                valuation,
                out var score,
                out var subgraphEdges,
                out var gToH,
                out var hToG,
                disconnected,
                false
                //, (Math.Min(g.EdgeCount, h.EdgeCount) + Math.Min(g.Vertices.Count, h.Vertices.Count)) * 3
                );
#else
            SubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                    g,
                    h,
                    valuation,
                    out var score,
                    out var subgraphEdges,
                    out var gToH,
                    out var hToG,
                    disconnected,
                    false,
                    1000
                    );
#endif
            time.Stop();
            Console.WriteLine($"Score {score}, Time {time.ElapsedMilliseconds:F2}ms");
            var order = gToH.Keys.ToArray();
            g.PrintSubgraph(order, gToH);
            h.PrintSubgraph(order.Select(key => gToH[key]).ToArray(), hToG);
        }

        private static void GenerateCopyWithRedundant(int n, int extra, out Graph g, out Graph h)
        {
            g = GraphFactory.GenerateRandom(n, 0.5, 0);
            h = g.Permute(1);
            var random = new Random(0);
            for (int j = 0; j < extra; j++)
            {
                h.AddVertex(n + j, new HashSet<int>(Enumerable.Range(0, n + j).Where(k => random.NextDouble() < 0.5)));
            }
            Export("Copy_With_redundant", g, h);
        }

        private static void GenerateClebschPetersen(out Graph g, out Graph h)
        {
            g = GraphFactory.Generate5RegularClebschGraph().Permute(0);
            h = GraphFactory.GeneratePetersenGraph().Permute(1);
            Export("Clebsch_Petersen", g, h);
        }

        private static void GenerateRandom09Petersen(int i, out Graph g, out Graph h)
        {
            g = GraphFactory.GeneratePetersenGraph();
            h = GraphFactory.GenerateRandom(i, 0.9, 1);
            Export("Random_Petersen_06", g, h);
        }
        private static void GenerateRandom0908(int i, int j, out Graph g, out Graph h)
        {
            g = GraphFactory.GenerateRandom(i, 0.9, 0);
            h = GraphFactory.GenerateRandom(j, 0.8, 1);
            Export("Random_08_09", g, h);
        }
        private static void GenerateRandomWithCycle(int i, int j, out Graph g, out Graph h)
        {
            // random graph and a cycle
            g = GraphFactory.GenerateRandom(i, 0.4, 0);
            h = GraphFactory.GenerateCycle(j).Permute(1);
            Export("Random_And_Cycle", g, h);
        }
        private static void GenerateCliquesConnectedByChain(int i, int j, out Graph g, out Graph h)
        {
            g = GraphFactory.GenerateCliquesConnectedByChain(i, j, 5);
            h = GraphFactory.GenerateCliquesConnectedByChain(i, j, 4);
            Export("Cliques_Connected_By_Chain", g, h);
        }

        private static void Export(string foldername, Graph g, Graph h)
        {
            if (doExport)
            {


                if (g.Vertices.Count > h.Vertices.Count)
                {
                    var tmp = h;
                    h = g;
                    g = tmp;
                }

                Directory.CreateDirectory(foldername);

                g.Write($"{foldername}/{g.Vertices.Count}_{h.Vertices.Count}_A_Wojciechowski.csv");
                h.Write($"{foldername}/{g.Vertices.Count}_{h.Vertices.Count}_B_Wojciechowski.csv");
            }
        }
    }
}
