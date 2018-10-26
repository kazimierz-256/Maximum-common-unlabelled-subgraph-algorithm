using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismApproximator
    {
        public static void ApproximateOptimalSubgraph(
            int orderOfPolynomial,
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, double> graphScoringFunction,
            double initialScore,
            out double bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            SerialSubgraphIsomorphismExtractor<double>.ExtractOptimalSubgraph(gArgument, hArgument, graphScoringFunction, initialScore, out var localBestScore, out var localSubgrahEdges, out var ghExactMapping, out var hgExactMapping, analyzeDisconnected, findExactMatch);

            CoreInternalState<double> initialSetup(int gMatchingVertex, int hMatchingVertex)
            {
                var setupCore = new CoreAlgorithm<double>();
                setupCore.HighLevelSetup(gMatchingVertex, hMatchingVertex, gArgument, hArgument, graphScoringFunction, null, analyzeDisconnected, findExactMatch);
                return setupCore.ExportShallowInternalState();
            };

            // choose best vertex
            var results = new Dictionary<KeyValuePair<int, int>, double>();
            foreach (var gVertex in gArgument.Vertices.ToArray())
            {
                foreach (var hVertex in hArgument.Vertices.ToArray())
                {
                    var thisKVP = new KeyValuePair<int, int>(gVertex, hVertex);

                    var ghInitialSetup = initialSetup(gVertex, hVertex);
                    // todo: verify constant here...
                    ghInitialSetup.recursionDepth = orderOfPolynomial;
                    var localResults = new List<double>();
                    ghInitialSetup.depthReached = (int depthAboveRequired, double score, Dictionary<int, int> ghLocalMap, Dictionary<int, int> hgLocalMap, int edges) =>
                    {
                        var safeDepth = depthAboveRequired + 1;
                        localResults.Add(Math.Pow(score, 3));
                    };

                    var nullBest = initialScore;
                    var predictor = new CoreAlgorithm<double>();
                    predictor.ImportShallowInternalState(ghInitialSetup);
                    predictor.Recurse(ref nullBest);

                    if (!results.ContainsKey(thisKVP))
                    {
                        results.Add(thisKVP, 0);
                    }
                    results[thisKVP] = localResults.Count > 0 ? localResults.Sum() : double.NegativeInfinity;
                }
            }

            // gather statistical data about different choices
            KeyValuePair<int, int> bestConnection;
            var bestConnectionValue = double.MinValue;
            foreach (var kvp in results)
            {
                if (kvp.Value > bestConnectionValue)
                {
                    bestConnectionValue = kvp.Value;
                    bestConnection = kvp.Key;
                }
            }

            if (ghExactMapping.ContainsKey(bestConnection.Key) && ghExactMapping[bestConnection.Key] == bestConnection.Value)
                Console.Beep();

            


            // make the best local choice
            // advance in recursion
            // herezje:
            bestScore = initialScore;
            subgraphEdges = 0;
            ghOptimalMapping = null;
            hgOptimalMapping = null;
        }
    }
}
