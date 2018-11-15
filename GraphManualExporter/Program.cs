using Application;
using GraphDataStructure;
using SubgraphIsomorphismExactAlgorithm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GraphManualExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            Graph g, h;
            GenerateCliquesConnectedByChain(9, 8, out g, out h);
            //GenerateRandomWithCycle(50, 14, out g, out h);
            //GenerateRandom0406(11, 11000, out g, out h);

            //Export("Random", g, h);
            Func<int, int, double> valuation = (v, e) => v + e;
            var disconnected = true;
#if true
            ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                g,
                h,
                valuation,
                out var score,
                out var subgraphEdges,
                out var gToH,
                out var hToG,
                disconnected,
                false,
                (Math.Min(g.EdgeCount, h.EdgeCount) + Math.Min(g.Vertices.Count, h.Vertices.Count)) * 3
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
                    100000
                    );
#endif

            var order = gToH.Keys.ToArray();
            g.PrintSubgraph(order, gToH);
            h.PrintSubgraph(order.Select(key => gToH[key]).ToArray(), hToG);
        }
        private static void GenerateRandom0406(int i, int j, out Graph g, out Graph h)
        {
            // random graph and a cycle
            g = GraphFactory.GenerateRandom(i, 0.4, 0);
            h = GraphFactory.GenerateRandom(j, 0.6, 1);
        }
        private static void GenerateRandomWithCycle(int i, int j, out Graph g, out Graph h)
        {
            // random graph and a cycle
            g = GraphFactory.GenerateRandom(i, 0.4, 0);
            h = GraphFactory.GenerateCycle(j);
        }
        private static void GenerateCliquesConnectedByChain(int i, int j, out Graph g, out Graph h)
        {
            g = GraphFactory.GenerateCliquesConnectedByChain(i, j, 5);
            h = GraphFactory.GenerateCliquesConnectedByChain(i, j, 4);
        }

        private static void Export(string foldername, Graph g, Graph h)
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
