using System;
using System.Collections.Generic;
using System.Text;

namespace GraphDataStructure
{
    public interface UndirectedGraph
    {
        int VertexCount { get; }

        IEnumerable<KeyValuePair<int, HashSet<int>>> Connections { get; }

        IEnumerable<int> NeighboursOf(int gMatchingVertex);
        bool ExistsConnectionBetween(int gVertexInSubgraph, int gNeighbour);
        int Degree(int v);
        HashSet<int> RemoveVertex(int vertexToRemove);
        UndirectedGraph DeepClone();
        void RestoreVertex(int restoreVertex, HashSet<int> restoreNeighbours);
    }
}
