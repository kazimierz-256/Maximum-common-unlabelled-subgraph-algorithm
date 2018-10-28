using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class ParallelPreparedSubgraphIsomorphismExtractor
    {
        public static void ExtractOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, double> graphScoringFunction,
            out double bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            SerialSubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                5,
                gArgument,
                hArgument,
                graphScoringFunction,
                out var score,
                out var edges,
                out var gToH,
                out var hToG,
                analyzeDisconnected,
                findExactMatch
                );

            ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                gArgument,
                hArgument,
                graphScoringFunction,
                score,
                out bestScore,
                out subgraphEdges,
                out ghOptimalMapping,
                out hgOptimalMapping,
                analyzeDisconnected,
                findExactMatch
                );
        }

    }
}
