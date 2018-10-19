using System;
using System.Collections.Generic;
using System.Text;

namespace GraphDataStructure
{
    public interface UndirectedGraph
    {
        int EdgeCount { get; }

        Dictionary<int, HashSet<int>> Neighbours { get; }
        HashSet<int> Vertices { get; }

        HashSet<int> NeighboursOf(int gMatchingVertex);
        bool ExistsConnectionBetween(int gVertexInSubgraph, int gNeighbour);
        int Degree(int v);
        HashSet<int> RemoveVertex(int vertexToRemove);
        UndirectedGraph DeepClone();
        void RestoreVertex(int restoreVertex, HashSet<int> restoreNeighbours);
        UndirectedGraph DeepCloneIntersecting(HashSet<int> gOutsiders);
    }
}
