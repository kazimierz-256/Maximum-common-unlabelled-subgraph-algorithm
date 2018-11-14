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
            //ExportGraphs1(9, 8, out var g, out var h);
            //ExportGraphs2(50, 14, out var g, out var h);
            ExportGraphs3(11, 11000, out var g, out var h);

            ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                g,
                h,
                (vertices, edges) => vertices + edges,
                out var score,
                out var subgraphEdges,
                out var gToH,
                out var hToG,
                false,
                false
                );

            var order = gToH.Keys.ToArray();
            g.PrintSubgraph(order, gToH);
            h.PrintSubgraph(order.Select(key => gToH[key]).ToArray(), hToG);
        }
        private static void ExportGraphs3(int i, int j, out Graph g, out Graph h)
        {
            // random graph and a cycle
            g = GraphFactory.GenerateRandom(i, 0.4, 0);
            h = GraphFactory.GenerateRandom(j, 0.6, 1);

            Export("Random", g, h);
        }
        private static void ExportGraphs2(int i, int j, out Graph g, out Graph h)
        {
            // random graph and a cycle
            g = GraphFactory.GenerateRandom(i, 0.4, 0);
            h = GraphFactory.GenerateCycle(j);

            Export("Finding_cycles", g, h);
        }
        private static void ExportGraphs1(int i, int j, out Graph g, out Graph h)
        {
            g = GraphFactory.GenerateCliquesConnectedByChain(i, j, 5);
            h = GraphFactory.GenerateCliquesConnectedByChain(i, j, 4);

            Export("Discovering_cliques_connected_by_a_chain", g, h);
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
