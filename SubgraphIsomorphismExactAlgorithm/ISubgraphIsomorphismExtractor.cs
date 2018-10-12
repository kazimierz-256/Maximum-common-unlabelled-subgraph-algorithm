using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public interface ISubgraphIsomorphismExtractor<T> where T : IComparable
    {
        void Extract(
               Graph argG,
               Graph argH,
               Func<Graph, int, T> vertexScore,
               T initialScore,
               out T score,
               out int[] gBestSolution,
               out int[] hBestSolution
               );
    }
}