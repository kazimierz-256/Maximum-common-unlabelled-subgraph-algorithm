using System;
using System.Collections.Generic;
using System.Text;

namespace GraphDataStructure
{
    public interface LevelledUndirectedGraph
    {
        int VertexCount { get; }
        int EdgeCount { get; }

        Dictionary<int, HashSet<int>[]> Neighbours { get; }
        HashSet<int> Vertices { get; }

        HashSet<int> NeighboursOf(int vertex, int level);
        void ChangeLevel(int vertex, int newLevel);
        bool ExistsConnectionBetween(int vertex1, int vertex2);
        int Degree(int v);
        HashSet<int>[] RemoveVertex(int vertex);
        UndirectedGraph DeepClone();
        void RestoreVertex(int vertex, HashSet<int>[] restoreOperation);
    }
}
