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
            Console.Write(string.Empty.PadLeft(4));
            for (int i = 0; i < connections.GetLength(0); i++)
            {
                var indexIsInSubgraph = i < gSubgraphVertexOrder.Length;
                Console.ForegroundColor = indexIsInSubgraph ? ConsoleColor.Green : ConsoleColor.Gray;
                Console.Write($"{ordering[i]} ".PadLeft(3));
                Console.ResetColor();
            }
            Console.WriteLine();
            for (int i = 0; i < g.Vertices.Count; i++)
            {
                var indexIsInSubgraph = i < gSubgraphVertexOrder.Length;
                if (indexIsInSubgraph)
                    Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{ordering[i]}:".PadLeft(4));
                Console.ResetColor();
                for (int j = 0; j < g.Vertices.Count; j++)
                {
                    var isInSubgraph = i < gSubgraphVertexOrder.Length && j < gSubgraphVertexOrder.Length;
                    if (i > j)
                    {
                        Console.Write(string.Empty.PadLeft(3));
                    }
                    else if (i == j)
                    {

                        Console.ForegroundColor = isInSubgraph ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;

                        Console.Write($"{g.Degree(ordering[i])} ".PadLeft(3));
                    }
                    else
                    {

                        if (connections[i, j])
                        {
                            if (isInSubgraph)
                            {
                                Console.BackgroundColor = ConsoleColor.Green;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            else
                            {
                                Console.BackgroundColor = ConsoleColor.DarkGray;
                                Console.ForegroundColor = ConsoleColor.Black;
                            }
                            Console.Write("1 ".PadLeft(3));
                        }
                        else
                        {
                            if (isInSubgraph)
                            {
                                Console.BackgroundColor = ConsoleColor.Black;
                                Console.ForegroundColor = ConsoleColor.Green;
                            }
                            Console.Write(string.Empty.PadLeft(3));
                        }
                    }
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
    }
}
