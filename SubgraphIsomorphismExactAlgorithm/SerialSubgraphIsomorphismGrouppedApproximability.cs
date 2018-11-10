using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class SerialSubgraphIsomorphismGrouppedApproximability
    {
        public static void ApproximateOptimalSubgraph(
        Graph gArgument,
        Graph hArgument,
        Func<int, int, double> graphScoringFunction,
        out double bestScore,
        out int subgraphEdges,
        out Dictionary<int, int> ghOptimalMapping,
        out Dictionary<int, int> hgOptimalMapping,
        bool analyzeDisconnected = false,
        bool findExactMatch = false,
        int plateauMultiplier = 10,
        int atLeastSteps = 200
        )
        {
            // initialize and precompute
            bestScore = double.MinValue;
            subgraphEdges = 0;
            ghOptimalMapping = new Dictionary<int, int>();
            hgOptimalMapping = new Dictionary<int, int>();

            var maxScore = double.NegativeInfinity;

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
            var plateau = plateauMultiplier * Math.Min(gArgument.Vertices.Count, hArgument.Vertices.Count);
            var max = atLeastSteps + plateau;
            var theoreticalMaximum = Math.Min(
                graphScoringFunction(gArgument.Vertices.Count, gArgument.EdgeCount),
                graphScoringFunction(hArgument.Vertices.Count, hArgument.EdgeCount)
                );

            // repeat randomized approximations
            for (int valuationIndex = 0; valuationIndex < max; valuationIndex += 1)
            {
                ApproximateOptimalSubgraph(
                    gArgument,
                    hArgument,
                    gConnectionExistance,
                    hConnectionExistance,
                    graphScoringFunction,
                    random,
                    out var localScore,
                    out var localEdges,
                    out var ghLocalMapping,
                    out var hgLocalMapping,
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
                    if (bestScore == theoreticalMaximum)
                    {
                        break;
                    }
                }
            }
        }

        private static void ApproximateOptimalSubgraph(
            Graph gArgument,
            Graph hArgument,
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
                    findGraphGinH = findExactMatch,
                    analyzeDisconnected = analyzeDisconnected,
                    subgraphScoringFunction = graphScoringFunction,
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
            var currentAlgorithmHoldingState = new CoreAlgorithm();
            // while there is an increase in result continue to approximate

            var step = 0;

            // quit the loop once a local maximum (scoring function) is reached
            while (true)
            {
                if (step == 0)
                {
                    var gSkip = random.Next(gArgument.Vertices.Count);
                    var hSkip = random.Next(hArgument.Vertices.Count);
                    var gCandidate = gArgument.Vertices.Skip(gSkip).First();
                    var hCandidate = hArgument.Vertices.Skip(hSkip).First();
                    currentAlgorithmHoldingState.ImportShallowInternalState(initialSetupPreMatch(gCandidate, hCandidate));
                    currentAlgorithmHoldingState.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate);
                }
                else
                {
                    var shallowInternalState = currentAlgorithmHoldingState.ExportShallowInternalState();
                    var gRandomizedOrder = shallowInternalState.gEnvelope.ToArray();
                    var hRandomizedOrder = shallowInternalState.hEnvelope.ToArray();

                    var randomizingArray = Enumerable.Range(0, gRandomizedOrder.Length).Select(i => random.Next()).ToArray();
                    Array.Sort(randomizingArray, gRandomizedOrder);
                    randomizingArray = Enumerable.Range(0, hRandomizedOrder.Length).Select(i => random.Next()).ToArray();
                    Array.Sort(randomizingArray, hRandomizedOrder);

                    var stopIteration = false;
                    // the greedy step
                    foreach (var gCandidate in gRandomizedOrder)
                    {
                        foreach (var hCandidate in hRandomizedOrder)
                            if (currentAlgorithmHoldingState.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate))
                            {
                                // once the prediction turns out successful only then will the internal state be modified
                                stopIteration = true;
                                break;
                            }
                        // successful matching, skip all other possible matchings
                        if (stopIteration)
                            break;
                    }
                    // no successful matching, reached local maximum
                    if (!stopIteration)
                        break;
                }

                step += 1;
            }

            var finalState = currentAlgorithmHoldingState.ExportShallowInternalState();
            if (findExactMatch && finalState.ghMapping.Count < gArgument.Vertices.Count)
            {
                // did not find an exact match, simply return initial values
                bestScore = double.MinValue;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                bestScore = graphScoringFunction(finalState.ghMapping.Keys.Count, finalState.totalNumberOfEdgesInSubgraph);
                subgraphEdges = finalState.totalNumberOfEdgesInSubgraph;
                ghOptimalMapping = finalState.ghMapping;
                hgOptimalMapping = finalState.hgMapping;
            }
        }
    }
}
