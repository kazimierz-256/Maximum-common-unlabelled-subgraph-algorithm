using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphDataStructure
{
    public class GraphFactory
    {
        public static Graph GenerateRandom(int n, double density, int generatingSeed)
        {
            var random = new Random(generatingSeed);
            var neighbours = new Dictionary<int, HashSet<int>>();
            var edges = 0;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (random.NextDouble() < density)
                    {
                        // add undirected edge
                        if (neighbours.ContainsKey(i))
                        {
                            neighbours[i].Add(j);
                            edges += 1;
                        }
                        else
                        {
                            neighbours.Add(i, new HashSet<int> { j });
                            edges += 1;
                        }

                        if (neighbours.ContainsKey(j))
                        {
                            neighbours[j].Add(i);
                        }
                        else
                        {
                            neighbours.Add(j, new HashSet<int> { i });
                        }
                    }
                }
            }

            return new Graph(neighbours, new HashSet<int>(Enumerable.Range(0, n).ToArray()), edges);
        }

        public static Graph GeneratePermuted(Graph g, int permutingSeed)
        {
            // permute the vertices and make another graph

            var permutedIntegers = Enumerable.Range(0, g.Vertices.Count).ToArray();
            Permute(permutingSeed, ref permutedIntegers);
            var translation = new Dictionary<int, int>();

            var neighbours = new Dictionary<int, HashSet<int>>();
            foreach (var kvp in g.Neighbours)
            {
                var fromVertex = kvp.Key;
                if (!translation.ContainsKey(fromVertex))
                {
                    translation.Add(fromVertex, permutedIntegers[translation.Count]);
                }
                foreach (var toVertex in kvp.Value)
                {
                    if (!translation.ContainsKey(toVertex))
                    {
                        translation.Add(toVertex, permutedIntegers[translation.Count]);
                    }
                    if (!neighbours.ContainsKey(translation[fromVertex]))
                    {
                        neighbours.Add(translation[fromVertex], new HashSet<int>());
                    }
                    neighbours[translation[fromVertex]].Add(translation[toVertex]);
                }
            }

            return new Graph(neighbours, new HashSet<int>(g.Vertices), g.EdgeCount);
        }

        // permuting helper
        private static void Permute(int seed, ref int[] vertices)
        {
            var random = new Random(seed);
            var randomValues = Enumerable.Range(0, vertices.Length).Select(i => random.Next()).ToArray();

            Array.Sort(randomValues, vertices);
        }

        // permuting helper
        private static void Permute(ref int[] vertices, Func<int, double> valuation)
        {
            var valuations = vertices.Select(v => valuation(v)).ToArray();

            Array.Sort(valuations, vertices);
        }
    }
}
