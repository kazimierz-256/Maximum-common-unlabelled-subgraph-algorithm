#define ultraPerformant
using System;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public class Graph
    {
        public Graph(Dictionary<int, HashSet<int>> neighbours)
        {
            this.neighbours = neighbours;
        }

        // todo: implement two data structures to optimize performance
        private Dictionary<int, HashSet<int>> neighbours = new Dictionary<int, HashSet<int>>();

        public int VertexCount => neighbours.Keys.Count;

        public IEnumerable<KeyValuePair<int, HashSet<int>>> EnumerateConnections()
        {
            foreach (var connection in neighbours)
            {
                yield return connection;
            }
        }

        public IEnumerable<int> NeighboursOf(int gMatchingVertex)
        {
#if !(ultraPerformant)
            if (neighbours.ContainsKey(gMatchingVertex))
#endif
            foreach (var neighbour in neighbours[gMatchingVertex])
            {
                yield return neighbour;
            }
        }
        public bool ExistsConnectionBetween(int gVertexInSubgraph, int gNeighbour)
        {
#if !(ultraPerformant)
            if (neighbours.ContainsKey(gVertexInSubgraph))
            {
#endif
            return neighbours[gVertexInSubgraph].Contains(gNeighbour);
#if !(ultraPerformant)
            }
            else
            {
                return false;
            }
#endif
        }
        public int Degree(int v) => neighbours[v].Count;
    }
}
