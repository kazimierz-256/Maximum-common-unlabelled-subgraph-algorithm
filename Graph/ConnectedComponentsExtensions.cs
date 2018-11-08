using GraphDataStructure;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public static class ConnectedComponentsExtensions
    {
        public static List<HashSet<int>> ConnectedComponents(this Graph g)
        {
            var connectedComponents = new List<HashSet<int>>();
            var analyzed = new HashSet<int>();
            foreach (var vertex in g.Vertices)
            {
                if (!analyzed.Contains(vertex))
                {
                    // new vertex
                    var cc = new HashSet<int>()
                    {
                        vertex
                    };
                    analyzed.Add(vertex);
                    BFSSearch(g, vertex, cc, analyzed);
                    connectedComponents.Add(cc);
                }
            }
            return connectedComponents;
        }

        private static void BFSSearch(Graph g, int vertex, HashSet<int> cc, HashSet<int> analyzed)
        {
            foreach (var neighbour in g.NeighboursOf(vertex))
            {
                if (!cc.Contains(neighbour))
                {
                    // new withing connected component
                    cc.Add(neighbour);
                    analyzed.Add(neighbour);
                    BFSSearch(g, neighbour, cc, analyzed);
                }
            }
        }


        public static Graph GraphOfConnectedComponent(this Graph g, int ccVertex)
        {
            var vertices = new HashSet<int>() { ccVertex };
            var analyzed = new HashSet<int>() { ccVertex };
            BFSSearch(g, ccVertex, vertices, analyzed);

            var edges = new Dictionary<int, HashSet<int>>();
            var directedEdgeCount = 0;

            foreach (var vertex in vertices)
            {
                foreach (var neighbour in g.NeighboursOf(vertex))
                {
                    if (vertices.Contains(neighbour))
                    {
                        if (!edges.ContainsKey(vertex))
                            edges.Add(vertex, new HashSet<int>());

                        edges[vertex].Add(neighbour);
                        directedEdgeCount += 1;
                    }
                }
            }


            return new Graph(edges, vertices, directedEdgeCount);
        }
    }
}
