using System;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public class HashGraph : UndirectedGraph
    {
        // note: this does not copy it reassigns
        public HashGraph(Dictionary<int, HashSet<int>> neighbours)
        {
            this.neighbours = neighbours;
        }

        // todo: implement two data structures to optimize performance
        private Dictionary<int, HashSet<int>> neighbours = new Dictionary<int, HashSet<int>>();

        public int VertexCount => neighbours.Keys.Count;

        public IEnumerable<KeyValuePair<int, HashSet<int>>> Connections
        {
            get
            {
                foreach (var connection in neighbours)
                {
                    yield return connection;
                }
            }
        }
        public IEnumerable<int> NeighboursOf(int gMatchingVertex)
        {
            foreach (var neighbour in neighbours[gMatchingVertex])
                yield return neighbour;
        }
        public bool ExistsConnectionBetween(int gVertexInSubgraph, int gNeighbour) => neighbours[gVertexInSubgraph].Contains(gNeighbour);

        public int Degree(int v) => neighbours[v].Count;

        public HashSet<int> RemoveVertex(int vertexToRemove)
        {
            var toReturn = neighbours[vertexToRemove];
            neighbours.Remove(vertexToRemove);

            foreach (var neighbour in toReturn)
            {
                neighbours[neighbour].Remove(vertexToRemove);
            }

            return toReturn;
        }
        public UndirectedGraph DeepClone()
        {
            var neighboursCopy = new Dictionary<int, HashSet<int>>();
            foreach (var connection in neighbours)
            {
                neighboursCopy.Add(connection.Key, new HashSet<int>(connection.Value));
            }
            return new HashGraph(neighboursCopy);
        }
        // note: this does not copy it reassigns
        public void RestoreVertex(int restoreVertex, HashSet<int> restoreNeighbours)
        {
            neighbours.Add(restoreVertex, restoreNeighbours);
            foreach (var neighbour in restoreNeighbours)
            {
                neighbours[neighbour].Add(restoreVertex);
            }
        }
    }
}
