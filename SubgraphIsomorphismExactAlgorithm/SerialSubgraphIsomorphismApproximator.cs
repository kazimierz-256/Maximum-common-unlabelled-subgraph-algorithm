using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    class SerialSubgraphIsomorphismApproximator
    {
        // let D be max{|G|,|H|}
        // upper bound polynomial is on the order of O(D^4)
        public static void ApproximateOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            bool[,] gConnectionExistance,
            bool[,] hConnectionExistance,
            Func<int, int, double> graphScoringFunction,
            Random random,
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
            
            CoreInternalState initialSetupPreMatch(int gMatchingVertex, int hMatchingVertex, int additionalOrder = 0)
            {
                var stateToImport = new CoreInternalState()
                {
                    g = gArgument,
                    h = hArgument,
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

            // make the best local choice
            var bestLocalSetup = new CoreInternalState();
            var archivedScore = double.MinValue;
            var archivedEdges = 0;
            // while there is an increase in result continue to approximate

            var step = 0;
            while (true)
            {
                #region PREDICTION
                bool makePrediction(int gCandidate, int hCandidate, int additionalOrder = 0)
                {
                    var localSetup = bestLocalSetup.Clone();
                    var predictor = new CoreAlgorithm();
                    predictor.ImportShallowInternalState(localSetup);

                    if (predictor.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate))
                    {
                        var score = graphScoringFunction(localSetup.ghMapping.Keys.Count, localSetup.totalNumberOfEdgesInSubgraph);

                        bestLocalSetup = predictor.ExportShallowInternalState().Clone();
                        archivedScore = score;
                        archivedEdges = localSetup.totalNumberOfEdgesInSubgraph;

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                #endregion

                if (step == 0)
                {
                    var gSkip = random.Next(gArgument.Vertices.Count);
                    var hSkip = random.Next(hArgument.Vertices.Count);
                    var gCandidate = gArgument.Vertices.Skip(gSkip).First();
                    var hCandidate = hArgument.Vertices.Skip(hSkip).First();
                    bestLocalSetup = initialSetupPreMatch(gCandidate, hCandidate);
                    makePrediction(gCandidate, hCandidate);
                }
                else
                {
                    var gRandomizedOrder = bestLocalSetup.gEnvelope.ToArray();
                    var hRandomizedOrder = bestLocalSetup.hEnvelope.ToArray();
                    var randomizingArray = Enumerable.Range(0, gRandomizedOrder.Length).Select(i => random.Next()).ToArray();

                    Array.Sort(randomizingArray, gRandomizedOrder);
                    randomizingArray = Enumerable.Range(0, hRandomizedOrder.Length).Select(i => random.Next()).ToArray();
                    Array.Sort(randomizingArray, hRandomizedOrder);

                    var stopIteration = false;
                    foreach (var gCandidate in gRandomizedOrder)
                    {
                        foreach (var hCandidate in hRandomizedOrder)
                            if (makePrediction(gCandidate, hCandidate))
                            {
                                stopIteration = true;
                                break;
                            }
                        if (stopIteration)
                            break;
                    }
                    if (!stopIteration)
                        break;
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
                bestScore = archivedScore;
                subgraphEdges = archivedEdges;
                ghOptimalMapping = bestLocalSetup.ghMapping;
                hgOptimalMapping = bestLocalSetup.hgMapping;
            }
        }
    }
}
