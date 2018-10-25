using System;
using System.Collections.Generic;
using System.Text;

namespace GraphDataStructure
{
    public static class PrintingExtensions
    {
        public static void PrintSubgraph(this UndirectedGraph g, int[] gSubgraphVertexOrder, Dictionary<int, int> ghMap)
        {
            var ordering = new int[g.Vertices.Count];
            gSubgraphVertexOrder.CopyTo(ordering, 0);
            var insertionIndex = gSubgraphVertexOrder.Length;
            foreach (var vertex in g.Vertices)
            {
                if (!ghMap.ContainsKey(vertex))
                {
                    ordering[insertionIndex] = vertex;
                    insertionIndex += 1;
                }
            }

            var connections = new bool[g.Vertices.Count, g.Vertices.Count];

            for (int i = 0; i < g.Vertices.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    connections[i, j] = connections[j, i] = g.ExistsConnectionBetween(ordering[i], ordering[j]);
                }
            }

            // print the array
            Console.Write(string.Empty.PadLeft(5));
            for (int i = 0; i < connections.GetLength(0); i++)
            {
                var indexIsInSubgraph = i < gSubgraphVertexOrder.Length;
                if (indexIsInSubgraph)
                    Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($" {ordering[i]:D2}");
                Console.ResetColor();
            }
            Console.WriteLine();
            for (int i = 0; i < connections.GetLength(0); i++)
            {
                var indexIsInSubgraph = i < gSubgraphVertexOrder.Length;
                if (indexIsInSubgraph)
                    Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"  {ordering[i]:D2}: ");
                Console.ResetColor();
                for (int j = 0; j < connections.GetLength(1); j++)
                {
                    if (i >= j)
                    {
                        Console.Write("   ");
                    }
                    else
                    {
                        var isInSubgraph = i < gSubgraphVertexOrder.Length && j < gSubgraphVertexOrder.Length;

                        if (connections[i, j])
                        {
                            if (isInSubgraph)
                            {
                                Console.BackgroundColor = ConsoleColor.Green;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            Console.Write($" 1 ");
                        }
                        else
                        {
                            if (isInSubgraph)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.BackgroundColor = ConsoleColor.Black;
                            }
                            Console.Write($" 0 ");
                        }
                        Console.ResetColor();
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
