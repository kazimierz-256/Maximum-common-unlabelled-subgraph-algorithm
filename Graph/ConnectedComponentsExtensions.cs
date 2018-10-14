using GraphDataStructure;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public static class ConnectedComponentsExtensions
    {
        public static List<HashSet<int>> ConnectedComponents(this UndirectedGraph g)
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

        private static void BFSSearch(UndirectedGraph g, int vertex, HashSet<int> cc, HashSet<int> analyzed)
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
    }
}
