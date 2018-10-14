﻿using System;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public class HashGraph : UndirectedGraph
    {
        // note: this does not copy it reassigns
        public HashGraph(Dictionary<int, HashSet<int>> neighbours, HashSet<int> vertices, int edges, int vertexCount)
        {
            Neighbours = neighbours;
            Vertices = vertices;
            EdgeCount = edges;
            VertexCount = vertexCount;
        }

        // todo: implement two data structures to optimize performance
        public Dictionary<int, HashSet<int>> Neighbours { get; private set; } = new Dictionary<int, HashSet<int>>();
        public HashSet<int> Vertices { get; private set; } = new HashSet<int>();
        private readonly HashSet<int> emptyHashSet = new HashSet<int>();
        public int VertexCount { get; private set; } = 0;
        public int EdgeCount { get; private set; } = 0;

        public HashSet<int> NeighboursOf(int vertex)
        {
            if (Neighbours.ContainsKey(vertex))
                return Neighbours[vertex];
            else
                return emptyHashSet;
        }
        public bool ExistsConnectionBetween(int gVertexInSubgraph, int gNeighbour) => Neighbours[gVertexInSubgraph].Contains(gNeighbour);

        public int Degree(int v) => Neighbours[v].Count;

        public HashSet<int> RemoveVertex(int vertexToRemove)
        {
            Vertices.Remove(vertexToRemove);
            VertexCount -= 1;
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
        public UndirectedGraph DeepClone()
        {
            var neighboursCopy = new Dictionary<int, HashSet<int>>();
            foreach (var connection in Neighbours)
            {
                neighboursCopy.Add(connection.Key, new HashSet<int>(connection.Value));
            }
            return new HashGraph(neighboursCopy, new HashSet<int>(Vertices), EdgeCount, VertexCount);
        }
        // note: this does not copy it reassigns
        public void RestoreVertex(int restoreVertex, HashSet<int> restoreNeighbours)
        {
            Vertices.Add(restoreVertex);
            VertexCount += 1;
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
