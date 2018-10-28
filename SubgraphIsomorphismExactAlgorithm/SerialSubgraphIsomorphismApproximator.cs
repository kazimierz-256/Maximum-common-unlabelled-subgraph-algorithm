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
            int orderOfPolynomial,
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, double> graphScoringFunction,
            Func<int, int, int> degreeValuation,
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
            if (findExactMatch)
                throw new Exception("Feature not yet supported.");

            var orderOfPolynomialMinus3 = orderOfPolynomial - 3;
            if (orderOfPolynomialMinus3 < 0)
                orderOfPolynomialMinus3 = 0;

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

            CoreInternalState<double> initialSetupPreMatch(int gMatchingVertex, int hMatchingVertex, int additionalOrder = 0)
            {
                // todo: cache more immutable!
                //setupCore.HighLevelSetup(gMatchingVertex, hMatchingVertex, gArgument, hArgument, graphScoringFunction, null, analyzeDisconnected, findExactMatch);
                var stateToImport = new CoreInternalState<double>()
                {
                    g = gArgument,
                    h = hArgument,
                    gInitialChoice = gMatchingVertex,
                    hInitialChoice = hMatchingVertex,
                    recursionDepth = orderOfPolynomialMinus3 + additionalOrder,
                    findExactMatch = findExactMatch,
                    analyzeDisconnected = analyzeDisconnected,
                    graphScoringFunction = graphScoringFunction,
                    ghMapping = new Dictionary<int, int>(),
                    hgMapping = new Dictionary<int, int>(),
                    gEnvelope = new HashSet<int>() { gMatchingVertex },
                    hEnvelope = new HashSet<int>() { hMatchingVertex },
                    gOutsiders = new HashSet<int>(gArgument.Vertices),
                    hOutsiders = new HashSet<int>(hArgument.Vertices),
                    totalNumberOfEdgesInSubgraph = 0,

                    gConnectionExistance = gConnectionExistance,
                    hConnectionExistance = hConnectionExistance,
                };

                stateToImport.gOutsiders.Remove(gMatchingVertex);
                stateToImport.hOutsiders.Remove(hMatchingVertex);

                return stateToImport;
            };

            // choose best vertex
            KeyValuePair<int, int> bestConnection;

            // make the best local choice
            var bestLocalSetup = new CoreInternalState<double>();
            var bestNextSetup = new CoreInternalState<double>();
            // while there is an increase in result continue to approximate

            var anybodyMatched = true;
            var localBestConnectionDetails = new Tuple<double, int>(double.MinValue, 0);
            var archivedBestConnectionDetails = localBestConnectionDetails;
            var step = 0;
            while (anybodyMatched)
            {
                anybodyMatched = false;
                localBestConnectionDetails = new Tuple<double, int>(double.MinValue, 0);
                var localBestScore = double.MinValue;
                var maxDegree = int.MinValue;

                #region PREDICTION
                void makePrediction(int gCandidate, int hCandidate, int additionalOrder = 0)
                {
                    var thisKVP = new KeyValuePair<int, int>(gCandidate, hCandidate);

                    // todo: check also equal
                    // todo: traverse in order: best to worst
                    var predictor = new CoreAlgorithm<double>();
                    var localSetup = bestLocalSetup.Clone();
                    var potentialImprovedState = new CoreInternalState<double>();
                    localSetup.recursionDepth = orderOfPolynomialMinus3 + additionalOrder;
                    localSetup.checkForEquality = true;
                    localSetup.checkStartingFromBest = true;
                    localSetup.newSolutionFound = (double score, Func<Dictionary<int, int>> ghLocalMap, Func<Dictionary<int, int>> hgLocalMap, int edges, int depth) =>
                    {
                        if (localBestConnectionDetails.Item1 < score)
                        {
                            bestNextSetup = potentialImprovedState;
                            bestConnection = thisKVP;
                            localBestConnectionDetails = new Tuple<double, int>(score, edges);
                        }
                        else if (localBestConnectionDetails.Item1 == score)
                        {
                            var localValuation = degreeValuation(gArgument.Degree(gCandidate), hArgument.Degree(hCandidate));
                            if (localValuation > maxDegree)
                            {
                                bestNextSetup = potentialImprovedState;
                                bestConnection = thisKVP;
                                maxDegree = localValuation;
                                localBestConnectionDetails = new Tuple<double, int>(score, edges);
                            }
                        }
                    };
                    predictor.ImportShallowInternalState(localSetup);

                    if (predictor.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate))
                    {
                        potentialImprovedState = predictor.ExportShallowInternalState().Clone();
                        anybodyMatched = true;
                        predictor.Recurse(ref localBestScore);
                    }
                }
                #endregion

                if (step == 0)
                {
                    foreach (var gCandidate in gArgument.Vertices.ToArray())
                        foreach (var hCandidate in hArgument.Vertices.ToArray())
                        {
                            bestLocalSetup = initialSetupPreMatch(gCandidate, hCandidate);

                            makePrediction(gCandidate, hCandidate, 1);
                        }
                }
                else
                {
                    foreach (var gCandidate in bestLocalSetup.gEnvelope)
                        foreach (var hCandidate in bestLocalSetup.hEnvelope)
                            makePrediction(gCandidate, hCandidate, step <= 1 ? 1 : 0);
                }

                if (anybodyMatched)
                {
                    bestLocalSetup = bestNextSetup;
                    archivedBestConnectionDetails = localBestConnectionDetails;
                }

                step += 1;
            }


            if (findExactMatch && bestLocalSetup.ghMapping.Count < gArgument.Vertices.Count)
            {
                // did not find an exact match
                bestScore = double.MinValue;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                // advance in recursion
                bestScore = archivedBestConnectionDetails.Item1;
                subgraphEdges = archivedBestConnectionDetails.Item2;
                ghOptimalMapping = bestLocalSetup.ghMapping;
                hgOptimalMapping = bestLocalSetup.hgMapping;
            }
        }
    }
}
