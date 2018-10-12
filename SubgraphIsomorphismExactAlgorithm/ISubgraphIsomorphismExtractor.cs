using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public interface ISubgraphIsomorphismExtractor<T> where T : IComparable
    {
        void Extract(
               UndirectedGraph argG,
               UndirectedGraph argH,
               Func<int, int, T> graphScore,
               T initialScore,
               out T score,
               out Dictionary<int, int> gBestSolution,
               out Dictionary<int, int> hBestSolution
               );
    }
}