﻿using System;
using System.Collections.Generic;

namespace GraphDataStructure
{
    public class Graph
    {
        public int VertexCount { get; }
        public IEnumerable<int> Vertices { get; }

        public Graph Clone() => throw new NotImplementedException();
        public Graph CloneWithoutVertex(int v) => throw new NotImplementedException();
        public IEnumerable<int> NeighboursOf(int gVertex) => throw new NotImplementedException();
        public void RemoveVertex(int v) => throw new NotImplementedException(); // not mandatory, can use cloning...
    }
}
