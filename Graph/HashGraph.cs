using System;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public class Graph
    {
        // important note: this is a shallow copy
        public Graph(Dictionary<int, HashSet<int>> neighbours, HashSet<int> vertices, int edges)
        {
            Neighbours = neighbours;
            Vertices = vertices;
            EdgeCount = edges;
        }

        public Dictionary<int, HashSet<int>> Neighbours { get; private set; } = new Dictionary<int, HashSet<int>>();
        public HashSet<int> Vertices { get; private set; } = new HashSet<int>();
        private readonly HashSet<int> emptyHashSet = new HashSet<int>();
        public int EdgeCount { get; private set; } = 0;

        public HashSet<int> VertexNeighbours(int vertex)
        {
            if (Neighbours.ContainsKey(vertex))
                return Neighbours[vertex];
            else
                return emptyHashSet;
        }
        public bool AreVerticesConnected(int gVertexInSubgraph, int gNeighbour)
        {
            if (Neighbours.ContainsKey(gVertexInSubgraph))
            {
                return Neighbours[gVertexInSubgraph].Contains(gNeighbour);
            }
            else
            {
                return false;
            }
        }

        public int VertexDegree(int v)
        {
            if (Neighbours.ContainsKey(v))
            {
                return Neighbours[v].Count;
            }
            else
            {
                return 0;
            }
        }

        public HashSet<int> RemoveVertex(int vertexToRemove)
        {
            Vertices.Remove(vertexToRemove);
            if (Neighbours.ContainsKey(vertexToRemove))
            {
                var toReturn = Neighbours[vertexToRemove];
                Neighbours.Remove(vertexToRemove);

                foreach (var neighbour in toReturn)
                {
                    Neighbours[neighbour].Remove(vertexToRemove);
                }
                EdgeCount -= toReturn.Count;
                return toReturn;
            }
            else
            {
                return null;
            }
        }
        public Graph DeepClone()
        {
            var neighboursCopy = new Dictionary<int, HashSet<int>>();
            foreach (var connection in Neighbours)
            {
                neighboursCopy.Add(connection.Key, new HashSet<int>(connection.Value));
            }
            return new Graph(neighboursCopy, new HashSet<int>(Vertices), EdgeCount);
        }

        // note: this does not copy it reassigns
        public void AddVertex(int restoreVertex, HashSet<int> restoreNeighbours)
        {
            Vertices.Add(restoreVertex);
            if (restoreNeighbours != null)
            {
                Neighbours.Add(restoreVertex, restoreNeighbours);
                foreach (var neighbour in restoreNeighbours)
                {
                    Neighbours[neighbour].Add(restoreVertex);
                }
                EdgeCount += restoreNeighbours.Count;
            }
        }

    }
}
