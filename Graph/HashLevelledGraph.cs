using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphDataStructure
{
    public class HashLevelledGraph : LevelledUndirectedGraph
    {
        public HashLevelledGraph()
        {

        }

        public Dictionary<int, HashSet<int>[]> Neighbours { get; private set; } = new Dictionary<int, HashSet<int>[]>();
        private Dictionary<int, int> Levels = new Dictionary<int, int>();
        public HashSet<int> Vertices { get; private set; } = new HashSet<int>();
        private readonly HashSet<int> emptyHashSet = new HashSet<int>();
        public HashSet<int> NeighboursOf(int vertex, int level)
        {
            if (Neighbours.ContainsKey(vertex))
            {
                return Neighbours[vertex][level];
            }
            else
            {
                return emptyHashSet;
            }

        }

        public void ChangeLevel(int vertex, int newLevel)
        {
            if (Neighbours.ContainsKey(vertex))
            {
                foreach (var neighbourHashSet in Neighbours[vertex])
                {
                    foreach (var neighbour in neighbourHashSet)
                    {
                        // fix neighbourhood with neighbour
                    }
                }
            }
        }
        public int VertexCount => throw new NotImplementedException();

        public int EdgeCount => throw new NotImplementedException();

        public UndirectedGraph DeepClone() => throw new NotImplementedException();
        public int Degree(int v) => throw new NotImplementedException();
        public bool ExistsConnectionBetween(int vertex1, int vertex2) => throw new NotImplementedException();
        public HashSet<int>[] RemoveVertex(int vertex) => throw new NotImplementedException();
        public void RestoreVertex(int vertex, HashSet<int>[] restoreOperation) => throw new NotImplementedException();
    }
}
