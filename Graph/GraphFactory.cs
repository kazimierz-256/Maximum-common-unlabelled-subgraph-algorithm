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
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (random.NextDouble() < density)
                    {
                        // add undirected edge
                        if (neighbours.ContainsKey(i))
                        {
                            neighbours[i].Add(j);
                        }
                        else
                        {
                            neighbours.Add(i, new HashSet<int> { j });
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

            return new Graph(neighbours);
        }

        public static Graph GeneratePermuted(Graph g, int permutingSeed)
        {
            // permute the vertices and make another graph

            var permutedIntegers = Enumerable.Range(0, g.VertexCount).ToArray();
            Permute(permutingSeed, ref permutedIntegers);
            var translation = new Dictionary<int, int>();

            var neighbours = new Dictionary<int, HashSet<int>>();
            foreach (var kvp in g.EnumerateConnections())
            {
                var fromVertex = kvp.Key;
                if (!translation.ContainsKey(fromVertex))
                {
                    translation.Add(fromVertex, permutedIntegers[translation.Count]);
                }
                if (!neighbours.ContainsKey(translation[fromVertex]))
                {
                    neighbours.Add(translation[fromVertex], new HashSet<int>());
                }
                foreach (var toVertex in kvp.Value)
                {
                    if (!translation.ContainsKey(toVertex))
                    {
                        translation.Add(toVertex, permutedIntegers[translation.Count]);
                    }
                    neighbours[translation[fromVertex]].Add(translation[toVertex]);
                }
            }

            return new Graph(neighbours);
        }

        private static void Permute(int seed, ref int[] vertices)
        {
            var random = new Random(seed);
            var randomValues = Enumerable.Range(0, vertices.Length).Select(i => random.Next()).ToArray();

            Array.Sort(randomValues, vertices);
        }
    }
}
