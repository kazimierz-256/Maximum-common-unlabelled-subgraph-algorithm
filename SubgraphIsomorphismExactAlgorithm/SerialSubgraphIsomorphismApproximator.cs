using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismApproximator
    {
        // let D be max{|G|,|H|}
        // upper bound polynomial is on the order of O(D^5+D^{3+order})
        // it makes sense to make it at least 2
        public static void ApproximateOptimalSubgraph(
            int orderOfPolynomialMinus3,
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
            if (!analyzeDisconnected && findExactMatch)
                throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");

            if (orderOfPolynomialMinus3 < 2)
                orderOfPolynomialMinus3 = 2;

            #region Initial setup
            var gMax = gArgument.Vertices.Max();

            var gConnectionExistance = new bool[gMax + 1, gMax + 1];
            var hConnectionExistance = new bool[hArgument.Vertices.Count, hArgument.Vertices.Count];

            foreach (var kvp in gArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    gConnectionExistance[kvp.Key, vertexTo] = true;

            foreach (var kvp in hArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    hConnectionExistance[kvp.Key, vertexTo] = true;
            #endregion

            CoreInternalState<double> initialSetupAndMatch(int gMatchingVertex, int hMatchingVertex)
            {
                // todo: cache more immutable!
                //setupCore.HighLevelSetup(gMatchingVertex, hMatchingVertex, gArgument, hArgument, graphScoringFunction, null, analyzeDisconnected, findExactMatch);
                var stateToImport = new CoreInternalState<double>()
                {
                    g = gArgument,
                    h = hArgument,
                    gInitialChoice = gMatchingVertex,
                    hInitialChoice = hMatchingVertex,
                    recursionDepth = orderOfPolynomialMinus3,
                    findExactMatch = findExactMatch,
                    analyzeDisconnected = analyzeDisconnected,
                    graphScoringFunction = graphScoringFunction,
                    ghMapping = new Dictionary<int, int>() { { gMatchingVertex, hMatchingVertex } },
                    hgMapping = new Dictionary<int, int>() { { hMatchingVertex, gMatchingVertex } },
                    gEnvelope = new HashSet<int>(gArgument.NeighboursOf(gMatchingVertex)),
                    hEnvelope = new HashSet<int>(hArgument.NeighboursOf(hMatchingVertex)),
                    gOutsiders = new HashSet<int>(gArgument.Vertices),
                    hOutsiders = new HashSet<int>(hArgument.Vertices),
                    totalNumberOfEdgesInSubgraph = 0,

                    gConnectionExistance = gConnectionExistance,
                    hConnectionExistance = hConnectionExistance,
                };

                stateToImport.gOutsiders.Remove(gMatchingVertex);
                stateToImport.hOutsiders.Remove(hMatchingVertex);

                foreach (var gVertex in gArgument.NeighboursOf(gMatchingVertex))
                    stateToImport.gOutsiders.Remove(gVertex);
                foreach (var hVertex in hArgument.NeighboursOf(hMatchingVertex))
                    stateToImport.hOutsiders.Remove(hVertex);

                var setupCore = new CoreAlgorithm<double>();
                setupCore.ImportShallowInternalState(stateToImport);

                return setupCore.ExportShallowInternalState();
            };

            // choose best vertex
            KeyValuePair<int, int> bestConnection;
            var bestConnectionValue = double.MinValue;

            foreach (var gVertex in gArgument.Vertices.ToArray())
            {
                foreach (var hVertex in hArgument.Vertices.ToArray())
                {
                    var thisKVP = new KeyValuePair<int, int>(gVertex, hVertex);
                    var localResults = new List<double>();

                    var localInitialSetup = initialSetupAndMatch(gVertex, hVertex);

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
                    {
                        var thisResult = localResults.Max();
                        if (bestConnectionValue < thisResult)
                        {
                            bestConnectionValue = thisResult;
                            bestConnection = thisKVP;
                        }
                    }
                }
            }


            // make the best local choice
            var bestLocalSetup = initialSetupAndMatch(bestConnection.Key, bestConnection.Value);
            var bestNextSetup = bestLocalSetup;
            // while there is an increase in result continue to approximate

            var anybodyMatched = true;
            var localBestConnectionDetails = new Tuple<double, double, int>(double.MinValue, double.MinValue, 0);
            var archivedBestConnectionDetails = localBestConnectionDetails;
            while (anybodyMatched)
            {
                anybodyMatched = false;
                localBestConnectionDetails = new Tuple<double, double, int>(double.MinValue, double.MinValue, 0);
                foreach (var gCandidate in bestLocalSetup.gEnvelope)
                {
                    foreach (var hCandidate in bestLocalSetup.hEnvelope)
                    {
                        var thisKVP = new KeyValuePair<int, int>(gCandidate, hCandidate);

                        var nullBest = initialScore;

                        var predictor = new CoreAlgorithm<double>();
                        var localSetup = bestLocalSetup.Clone();
                        var potentialImprovedState = new CoreInternalState<double>();
                        localSetup.recursionDepth = orderOfPolynomialMinus3;
                        localSetup.newSolutionFound = (double score, Func<Dictionary<int, int>> ghLocalMap, Func<Dictionary<int, int>> hgLocalMap, int edges, int depth) =>
                        {
                            var realScore = Math.Pow(score, 3);
                            if (localBestConnectionDetails.Item1 < realScore)
                            {
                                bestNextSetup = potentialImprovedState;
                                bestConnection = thisKVP;
                                localBestConnectionDetails = new Tuple<double, double, int>(realScore, score, edges);
                            }
                        };
                        predictor.ImportShallowInternalState(localSetup);

                        if (predictor.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate))
                        {
                            potentialImprovedState = predictor.ExportShallowInternalState().Clone();
                            anybodyMatched = true;
                            predictor.Recurse(ref nullBest);
                        }
                    }
                }

                if (anybodyMatched)
                {
                    bestLocalSetup = bestNextSetup;
                    archivedBestConnectionDetails = localBestConnectionDetails;
                }
            }


            if (findExactMatch && bestLocalSetup.ghMapping.Count < gArgument.Vertices.Count)
            {
                // did not find an exact match
                bestScore = initialScore;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                // advance in recursion
                bestScore = archivedBestConnectionDetails.Item2;
                subgraphEdges = archivedBestConnectionDetails.Item3;
                ghOptimalMapping = bestLocalSetup.ghMapping;
                hgOptimalMapping = bestLocalSetup.hgMapping;
            }
        }
    }
}
