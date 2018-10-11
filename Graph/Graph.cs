using System;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public class Graph
    {
        public int VertexCount { get; set; }

        public Graph Clone() => throw new NotImplementedException();
        public Graph CloneWithoutVertex(int v) => throw new NotImplementedException();
        public IEnumerable<int> NeighboursOf(int gVertex) => throw new NotImplementedException();
    }
}
