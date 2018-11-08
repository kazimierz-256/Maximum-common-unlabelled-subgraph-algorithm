using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class SerialSubgraphIsomorphismGrouppedApproximability
    {
        // complexity upper bound order of this algorithm is O(D^3 log D) where D is the maximum of the sizes of two input graphs
        public static void ApproximateOptimalSubgraph(
        Graph gArgument,
        Graph hArgument,
        Func<int, int, double> graphScoringFunction,
        out double bestScore,
        out int subgraphEdges,
        out Dictionary<int, int> ghOptimalMapping,
        out Dictionary<int, int> hgOptimalMapping,
        bool analyzeDisconnected = false,
        bool findExactMatch = false
        )
        {
            bestScore = double.MinValue;
            subgraphEdges = 0;
            ghOptimalMapping = new Dictionary<int, int>();
            hgOptimalMapping = new Dictionary<int, int>();

            var maxScore = double.NegativeInfinity;
            double localScore;
            int localEdges;
            Dictionary<int, int> ghLocalMapping;
            Dictionary<int, int> hgLocalMapping;

            var gMax = gArgument.Vertices.Max();
            var hMax = hArgument.Vertices.Max();

            var gConnectionExistance = new bool[gMax + 1, gMax + 1];
            var hConnectionExistance = new bool[hMax + 1, hMax + 1];

            foreach (var kvp in gArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    gConnectionExistance[kvp.Key, vertexTo] = true;

            foreach (var kvp in hArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    hConnectionExistance[kvp.Key, vertexTo] = true;

            var random = new Random(0);
            int plateau = 10 * Math.Max(gArgument.Vertices.Count, hArgument.Vertices.Count);
            var max = 200 + plateau;
            var maxInTheory = Math.Min(
                graphScoringFunction(gArgument.Vertices.Count, gArgument.EdgeCount),
                graphScoringFunction(hArgument.Vertices.Count, hArgument.EdgeCount)
                );
            for (int valuationIndex = 0; valuationIndex < max; valuationIndex += 1)
            {
                SerialSubgraphIsomorphismApproximator.ApproximateOptimalSubgraph(
                    gArgument,
                    hArgument,
                    gConnectionExistance,
                    hConnectionExistance,
                    graphScoringFunction,
                    random,
                    out localScore,
                    out localEdges,
                    out ghLocalMapping,
                    out hgLocalMapping,
                    analyzeDisconnected,
                    findExactMatch
                    );

                if (localScore > maxScore)
                {
                    bestScore = maxScore = localScore;
                    subgraphEdges = localEdges;
                    ghOptimalMapping = new Dictionary<int, int>(ghLocalMapping);
                    hgOptimalMapping = new Dictionary<int, int>(hgLocalMapping);
                    max += plateau;
                    if (bestScore == maxInTheory)
                    {
                        break;
                    }
                }
            }
        }
    }
}
