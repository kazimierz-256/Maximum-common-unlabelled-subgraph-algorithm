using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismApproximator<T> where T : IComparable
    {
        public static void ApproximateOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, T> graphScoringFunction,
            T initialScore,
            out T bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            CoreInternalState<T> initialSetup(int gMatchingVertex, int hMatchingVertex)
            {
                var setupCore = new CoreAlgorithm<T>();
                setupCore.HighLevelSetup(gMatchingVertex, hMatchingVertex, gArgument, hArgument, graphScoringFunction, null, analyzeDisconnected, findExactMatch);
                return setupCore.ExportShallowInternalState();
            };

            // choose best vertex
            // gather statistical data about different choices
            // make the best local choice
            // advance in recursion
            throw new NotImplementedException();
        }
    }
}
