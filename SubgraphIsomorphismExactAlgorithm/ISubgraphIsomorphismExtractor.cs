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
               Func<int, int, T> graphScore,
               Func<Graph, int, T> extremumConditionClassifier,
               T initialScore,
               out T score,
               out Dictionary<int, int> gBestSolution,
               out Dictionary<int, int> hBestSolution
               );
    }
}