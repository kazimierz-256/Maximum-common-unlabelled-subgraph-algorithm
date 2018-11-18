using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class SubgraphIsomorphismGrouppedApproximability
    {
        public static void ApproximateOptimalSubgraph(
        Graph gArgument,
        Graph hArgument,
        Func<int, int, double> graphScoringFunction,
        out double bestScore,
        out int subgraphEdges,
        out Dictionary<int, int> ghOptimalMapping,
        out Dictionary<int, int> hgOptimalMapping,
        bool analyzeDisconnected,
        bool findExactMatch,
        int atLeastSteps,
        double milisecondTimeLimit = 0d,
        bool computeInParallel = true
        )
        {
            // initialize and precompute
            var maxScore = double.NegativeInfinity;

            var gMax = gArgument.Vertices.Max();
            var hMax = hArgument.Vertices.Max();

            var gConnectionExistence = new bool[gMax + 1, gMax + 1];
            var hConnectionExistence = new bool[hMax + 1, hMax + 1];

            foreach (var kvp in gArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    gConnectionExistence[kvp.Key, vertexTo] = true;

            foreach (var kvp in hArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    hConnectionExistence[kvp.Key, vertexTo] = true;


            var batches = Environment.ProcessorCount * 10;
            var max = atLeastSteps;
            if (milisecondTimeLimit > 0)
            {
                max = int.MaxValue - 1 - batches;
            }

            var theoreticalMaximumScoreValue = Math.Min(
                graphScoringFunction(gArgument.Vertices.Count, gArgument.EdgeCount),
                graphScoringFunction(hArgument.Vertices.Count, hArgument.EdgeCount)
                );

            var synchronizingObject = new object();
            var sw = new Stopwatch();
            sw.Start();
            TimeSpan getElapsedTimespan() => milisecondTimeLimit <= 0 ? TimeSpan.Zero : sw.Elapsed;
            var timespanlimit = milisecondTimeLimit <= 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(milisecondTimeLimit);

            var localBestScore = double.MinValue;
            var localSubgraphEdges = 0;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();

            // repeat randomized approximations

            void batchDo(int batch)
            {
                var part = (max + batches - 1) / batches;
                for (int valuationIndex = part * batch; valuationIndex < part * (batch + 1); valuationIndex += 1)
                {
                    ApproximateOptimalSubgraph(
                        gArgument.Vertices,
                        hArgument.Vertices,
                        gConnectionExistence,
                        hConnectionExistence,
                        graphScoringFunction,
                        new Random(valuationIndex),
                        out var localScore,
                        out var localEdges,
                        out var ghLocalMapping,
                        out var hgLocalMapping,
                        analyzeDisconnected,
                        findExactMatch
                        );

                    if (localScore > maxScore)
                    {
                        lock (synchronizingObject)
                        {

                            if (localScore > maxScore)
                            {
                                localBestScore = maxScore = localScore;
                                localSubgraphEdges = localEdges;
                                ghLocalOptimalMapping = new Dictionary<int, int>(ghLocalMapping);
                                hgLocalOptimalMapping = new Dictionary<int, int>(hgLocalMapping);
                            }
                        }
                    }
                    if (localBestScore == theoreticalMaximumScoreValue || getElapsedTimespan().CompareTo(timespanlimit) > 0)
                    {
                        break;
                    }
                }
            }

            if (computeInParallel)
                Parallel.For(0, batches, batch => batchDo(batch));
            else
                for (int batch = 0; batch < batches; batch+=1)
                    batchDo(batch);

            bestScore = localBestScore;
            subgraphEdges = localSubgraphEdges;
            ghOptimalMapping = ghLocalOptimalMapping;
            hgOptimalMapping = hgLocalOptimalMapping;
        }

        private static void ApproximateOptimalSubgraph(
            HashSet<int> gVertices,
            HashSet<int> hVertices,
            bool[,] gConnectionExistence,
            bool[,] hConnectionExistence,
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

            // make the best local choice
            var currentAlgorithmHoldingState = new CoreAlgorithm();
            // while there is an increase in result continue to approximate

            var step = 0;

            // quit the loop once a local maximum (scoring function) is reached
            while (true)
            {
                if (step == 0)
                {
                    var gSkip = random.Next(gVertices.Count);
                    var hSkip = random.Next(hVertices.Count);
                    var gCandidate = gVertices.Skip(gSkip).First();
                    var hCandidate = hVertices.Skip(hSkip).First();
                    var stateToImport = new CoreInternalState()
                    {
                        g = null,
                        h = null,
                        findGraphGinH = findExactMatch,
                        analyzeDisconnected = analyzeDisconnected,
                        subgraphScoringFunction = graphScoringFunction,
                        ghMapping = new Dictionary<int, int>(),
                        hgMapping = new Dictionary<int, int>(),
                        gEnvelope = new HashSet<int>() { gCandidate },
                        hEnvelope = new HashSet<int>() { hCandidate },
                        gOutsiders = new HashSet<int>(gVertices),
                        hOutsiders = new HashSet<int>(hVertices),
                        totalNumberOfEdgesInSubgraph = 0,

                        gConnectionExistence = gConnectionExistence,
                        hConnectionExistence = hConnectionExistence,
                    };

                    stateToImport.gOutsiders.Remove(gCandidate);
                    stateToImport.hOutsiders.Remove(hCandidate);
                    currentAlgorithmHoldingState.ImportShallowInternalState(stateToImport);
                    currentAlgorithmHoldingState.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate);
                }
                else
                {
                    var gRandomizedOrder = currentAlgorithmHoldingState.gExportEnvelope;
                    var hRandomizedOrder = currentAlgorithmHoldingState.hExportEnvelope;

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
            if (findExactMatch && finalState.ghMapping.Count < gVertices.Count)
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
