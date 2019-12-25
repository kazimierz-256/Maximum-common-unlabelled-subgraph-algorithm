using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphDataStructure
{
    public class GraphFactory
    {

        private static void connect(Dictionary<int, HashSet<int>> edges, int a, int b)
        {
            if (!edges.ContainsKey(a))
                edges.Add(a, new HashSet<int>());
            if (!edges.ContainsKey(b))
                edges.Add(b, new HashSet<int>());

            edges[a].Add(b);
            edges[b].Add(a);
        }

        public static Graph Generate5RegularClebschGraph()
        {
            var vertices = new HashSet<int>(Enumerable.Range(0, 16));
            var edges = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < 5; i+=1)
            {
                connect(edges, i, (i + 1) % 5);
                connect(edges, i, i + 5);
            }

            connect(edges, 5, 7);
            connect(edges, 5, 8);
            connect(edges, 6, 8);
            connect(edges, 6, 9);
            connect(edges, 7, 9);


            for (int i = 10; i < 15; i+=1)
            {
                connect(edges, i, 15);
                connect(edges, i, 5 + (i - 10));
                connect(edges, i, 5 + ((i - 9) % 5));
                connect(edges, i, (4 + (i - 10)) % 5);
                connect(edges, i, (2 + (i - 10)) % 5);
            }

            return new Graph(edges, vertices, 5 + 5 + 5 + 5 * 5);
        }

        public static Graph GeneratePetersenGraph()
        {

            var vertices = new HashSet<int>(Enumerable.Range(0, 10));
            var edges = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < 5; i+=1)
            {
                connect(edges, i, (i + 1) % 5);
                connect(edges, i, i + 5);
            }

            connect(edges, 5, 7);
            connect(edges, 5, 8);
            connect(edges, 6, 8);
            connect(edges, 6, 9);
            connect(edges, 7, 9);

            return new Graph(edges, vertices, 15);
        }
        public static Graph GenerateCliquesConnectedByChain(int i, int j, int chainLength)
        {
            if (chainLength < 2)
                throw new Exception("The chain is too short, try at least 2 edges");

            var vertices = new HashSet<int>(Enumerable.Range(0, i + j + chainLength - 1));
            var edges = new Dictionary<int, HashSet<int>>();

            foreach (var vertex in vertices)
                edges.Add(vertex, new HashSet<int>());

            for (int i1 = 0; i1 < i; i1+=1)
                for (int i1helper = 0; i1helper < i1; i1helper+=1)
                    connect(edges, i1, i1helper);

            for (int j1 = i; j1 < i + j; j1+=1)
                for (int j1helper = i; j1helper < j1; j1helper+=1)
                    connect(edges, j1, j1helper);

            connect(edges, 0, i + j);
            for (int chain = 0; chain < chainLength - 2; chain+=1)
                connect(edges, i + j + chain, i + j + chain + 1);
            connect(edges, i + j + chainLength - 2, i);

            return new Graph(edges, vertices, i * (i - 1) / 2 + j * (j - 1) / 2 + chainLength);
        }
        public static Graph GenerateCycle(int n)
        {
            var vertices = new HashSet<int>(Enumerable.Range(0, n));
            var neighbours = new Dictionary<int, HashSet<int>>
            {
                { 0, new HashSet<int>() { 1 } }
            };
            for (int i = 0; i < n - 1; i+=1)
            {
                neighbours[i].Add(i + 1);
                neighbours.Add(i + 1, new HashSet<int>() { i });
            }
            neighbours[n - 1].Add(0);
            neighbours[0].Add(n - 1);
            return new Graph(neighbours, vertices, n);
        }
        public static Graph GenerateRandom(int n, double density, int generatingSeed)
        {
            var random = new Random(generatingSeed);
            var neighbours = new Dictionary<int, HashSet<int>>();
            var edges = 0;
            for (int i = 0; i < n - 1; i+=1)
            {
                for (int j = i + 1; j < n; j+=1)
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
