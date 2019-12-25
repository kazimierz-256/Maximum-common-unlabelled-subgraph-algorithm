using GraphDataStructure;
using System.Collections.Generic;

namespace GraphExtensionAlgorithms
{
    public static class ConnectedComponentsExtensions
    {
        public static List<HashSet<int>> ConnectedComponents(this UndirectedGraph g)
        {
            var connectedComponents = new List<HashSet<int>>();
            var analyzed = new HashSet<int>();
            foreach (var vertex in g.Vertices)
            {
                var cc = new HashSet<int>()
                {
                    vertex
                };
                BFSSearch(g, vertex, cc);
                connectedComponents.Add(cc);
            }
            return connectedComponents;
        }

        private static void BFSSearch(UndirectedGraph g, int vertex, HashSet<int> cc)
        {
            foreach (var neighbour in g.NeighboursOf(vertex))
            {
                if (!cc.Contains(neighbour))
                {
                    cc.Add(neighbour);
                    BFSSearch(g, neighbour, cc);
                }
            }
        }
    }
}
