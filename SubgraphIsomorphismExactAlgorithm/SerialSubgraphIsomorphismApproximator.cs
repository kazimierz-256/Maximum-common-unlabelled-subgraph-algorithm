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
            if (analyzeDisconnected)
            {
                throw new NotImplementedException("Jeszcze nie obsługuję różnych składowych... muszę najpierw sprawdzić sprawność w prostszym przypadku");
            }
            SerialSubgraphIsomorphismExtractor<double>.ExtractOptimalSubgraph(gArgument, hArgument, graphScoringFunction, initialScore, out var localBestScore, out var localSubgrahEdges, out var ghExactMapping, out var hgExactMapping, analyzeDisconnected, findExactMatch);

            CoreInternalState<double> initialSetup(int gMatchingVertex, int hMatchingVertex)
            {
                // todo: cache more immutable!
                var setupCore = new CoreAlgorithm<double>();
                setupCore.HighLevelSetup(gMatchingVertex, hMatchingVertex, gArgument, hArgument, graphScoringFunction, null, analyzeDisconnected, findExactMatch);
                return setupCore.ExportShallowInternalState();
            };

            // choose best vertex
            var results = new Dictionary<KeyValuePair<int, int>, double>();
            foreach (var gVertex in gArgument.Vertices)
                foreach (var hVertex in hArgument.Vertices)
                    results.Add(new KeyValuePair<int, int>(gVertex, hVertex), 0d);

            foreach (var gVertex in gArgument.Vertices.ToArray())
            {
                foreach (var hVertex in hArgument.Vertices.ToArray())
                {
                    var thisKVP = new KeyValuePair<int, int>(gVertex, hVertex);
                    var localResults = new List<double>();

                    var localInitialSetup = initialSetup(gVertex, hVertex);
                    localInitialSetup.recursionDepth = orderOfPolynomial;
                    // todo: verify constant here...
                    localInitialSetup.newSolutionFound = (double score, Func<Dictionary<int, int>> ghLocalMap, Func<Dictionary<int, int>> hgLocalMap, int edges, int depth) =>
                    {
                        var realScore = Math.Pow(score, 3);
                        //foreach (var coolKVP in ghLocalMap)
                        //{
                        //    results[coolKVP] += realScore;
                        //}
                        //var safeDepth = depthAboveRequired + 1;
                        localResults.Add(realScore);
                    };

                    var nullBest = initialScore;
                    var predictor = new CoreAlgorithm<double>();
                    predictor.ImportShallowInternalState(localInitialSetup);
                    predictor.Recurse(ref nullBest);

                    // different combination of min/max...
                    // maybe try different valuations for the same graphs just to make sure various greedy strategies work:
                    // max, min/max combination, sum, average
                    // in the future make a large statistical rank of great valuations
                    if (localResults.Count > 0)
                        results[thisKVP] = localResults.Average();
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

            // make the best local choice
            var bestLocalSetup = initialSetup(bestConnection.Key, bestConnection.Value);
            var updater = new CoreAlgorithm<double>();
            updater.ImportShallowInternalState(bestLocalSetup);
            updater.TryMatchFromEnvelopeMutateInternalState(bestConnection.Key, bestConnection.Value);
            bestLocalSetup = updater.ExportShallowInternalState();
            // while there is an increase in result continue to approximate

            var anybodyMatched = true;
            var envelopeResults = new Dictionary<KeyValuePair<int, int>, Tuple<double, double, int>>();
            var bestConnectionDetails = new Tuple<double, double, int>(0, bestConnectionValue, 0);
            while (anybodyMatched)
            {
                envelopeResults.Clear();
                foreach (var gVertex in bestLocalSetup.gEnvelope)
                    foreach (var hVertex in bestLocalSetup.hEnvelope)
                        envelopeResults.Add(new KeyValuePair<int, int>(gVertex, hVertex), new Tuple<double, double, int>(0d, 0d, 0));

                anybodyMatched = false;
                foreach (var gCandidate in bestLocalSetup.gEnvelope)
                {
                    foreach (var hCandidate in bestLocalSetup.hEnvelope)
                    {
                        var thisKVP = new KeyValuePair<int, int>(gCandidate, hCandidate);

                        var nullBest = initialScore;

                        var predictor = new CoreAlgorithm<double>();
                        var localSetup = bestLocalSetup.Clone();
                        localSetup.recursionDepth = orderOfPolynomial;
                        localSetup.newSolutionFound = (double score, Func<Dictionary<int, int>> ghLocalMap, Func<Dictionary<int, int>> hgLocalMap, int edges, int depth) =>
                        {
                            var realScore = Math.Pow(score, 3);
                            if (envelopeResults[thisKVP].Item1<realScore)
                            {
                                envelopeResults[thisKVP] = new Tuple<double, double, int>( realScore, score, edges);
                            }
                        };
                        predictor.ImportShallowInternalState(localSetup);

                        if (predictor.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate))
                        {
                            anybodyMatched = true;
                            predictor.Recurse(ref nullBest);
                        }
                        else
                        {
                            // vertices not locally isomorphic, sorry
                        }
                    }
                }
                // detect the most profitable connection
                bestConnectionValue = double.MinValue;
                foreach (var kvp in envelopeResults)
                {
                    if (kvp.Value.Item1 > bestConnectionValue)
                    {
                        bestConnectionValue = kvp.Value.Item1;
                        bestConnection = kvp.Key;
                        bestConnectionDetails = kvp.Value;
                    }
                }

                updater = new CoreAlgorithm<double>();
                updater.ImportShallowInternalState(bestLocalSetup);
                updater.TryMatchFromEnvelopeMutateInternalState(bestConnection.Key, bestConnection.Value);
                bestLocalSetup = updater.ExportShallowInternalState();
            }


            // advance in recursion

            bestScore = bestConnectionDetails.Item2;
            subgraphEdges = bestConnectionDetails.Item3;
            ghOptimalMapping = bestLocalSetup.ghMapping;
            hgOptimalMapping = bestLocalSetup.hgMapping;
        }
    }
}
