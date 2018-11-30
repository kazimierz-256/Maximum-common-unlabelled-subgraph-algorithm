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
                var random = new Random(-batch);
                for (int k = 0; k < part; k += 1)
                {
                    #region Approximate

                    if (!analyzeDisconnected && findExactMatch)
                        throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");
                    if (findExactMatch)
                        throw new Exception("Feature not yet supported.");

                    // make the best local choice
                    var currentAlgorithmHoldingState = new CoreAlgorithm()
                    {
                        g = null,
                        h = null,
                        findGraphGinH = findExactMatch,
                        analyzeDisconnected = analyzeDisconnected,
                        subgraphScoringFunction = graphScoringFunction,
                        gMapping = new int[Math.Min(gArgument.Vertices.Count, hArgument.Vertices.Count)],
                        hMapping = new int[Math.Min(gArgument.Vertices.Count, hArgument.Vertices.Count)],
                        gEnvelope = new int[gArgument.Vertices.Count],
                        hEnvelope = new int[hArgument.Vertices.Count],
                        gOutsiders = new int[gArgument.Vertices.Count],
                        hOutsiders = new int[hArgument.Vertices.Count],
                        gConnectionExistence = gConnectionExistence,
                        hConnectionExistence = hConnectionExistence,
                    };
                    // while there is an increase in result continue to approximate

                    var step = 0;
                    // quit the loop once a local maximum (scoring function) is reached
                    while (true)
                    {
                        if (step == 0)
                        {
                            var gSkip = random.Next(gArgument.Vertices.Count);
                            var hSkip = random.Next(hArgument.Vertices.Count);

                            var i = 0;
                            foreach (var gVertex in gArgument.Vertices)
                            {
                                if (i == gSkip)
                                {
                                    currentAlgorithmHoldingState.gEnvelope[0] = gVertex;
                                    gSkip = -1;
                                }
                                else
                                {
                                    currentAlgorithmHoldingState.gOutsiders[i] = gVertex;
                                    i += 1;
                                }
                            }
                            i = 0;
                            foreach (var hVertex in hArgument.Vertices)
                            {
                                if (i == hSkip)
                                {
                                    currentAlgorithmHoldingState.hEnvelope[0] = hVertex;
                                    hSkip = -1;
                                }
                                else
                                {
                                    currentAlgorithmHoldingState.hOutsiders[i] = hVertex;
                                    i += 1;
                                }
                            }

                            currentAlgorithmHoldingState.mappingCount = 0;
                            currentAlgorithmHoldingState.gEnvelopeLimit = 1;
                            currentAlgorithmHoldingState.hEnvelopeLimit = 1;
                            currentAlgorithmHoldingState.gOutsidersLimit = gArgument.Vertices.Count - 1;
                            currentAlgorithmHoldingState.hOutsidersLimit = hArgument.Vertices.Count - 1;
                            currentAlgorithmHoldingState.totalNumberOfEdgesInSubgraph = 0;
                            currentAlgorithmHoldingState.TryMatchFromEnvelopeMutateInternalState(0, 0);
                        }
                        else
                        {
                            var gRandomizedIndexOrder = Enumerable.Range(0, currentAlgorithmHoldingState.gEnvelopeLimit).ToArray();
                            var hRandomizedIndexOrder = Enumerable.Range(0, currentAlgorithmHoldingState.hEnvelopeLimit).ToArray();

                            var randomizingArray = Enumerable.Range(0, gRandomizedIndexOrder.Length).Select(i => random.Next()).ToArray();
                            Array.Sort(randomizingArray, gRandomizedIndexOrder);
                            randomizingArray = Enumerable.Range(0, hRandomizedIndexOrder.Length).Select(i => random.Next()).ToArray();
                            Array.Sort(randomizingArray, hRandomizedIndexOrder);

                            var stopIteration = false;
                            // the greedy step
                            foreach (var gCandidateIndex in gRandomizedIndexOrder)
                            {
                                foreach (var hCandidateIndex in hRandomizedIndexOrder)
                                    if (currentAlgorithmHoldingState.TryMatchFromEnvelopeMutateInternalState(gCandidateIndex, hCandidateIndex))
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
                    if (!findExactMatch || currentAlgorithmHoldingState.mappingCount == gArgument.Vertices.Count)
                    {
                        var localScore = graphScoringFunction(currentAlgorithmHoldingState.mappingCount, currentAlgorithmHoldingState.totalNumberOfEdgesInSubgraph);
                        if (localScore > maxScore)
                        {
                            lock (synchronizingObject)
                            {
                                if (localScore > maxScore)
                                {
                                    localBestScore = maxScore = localScore;
                                    localSubgraphEdges = currentAlgorithmHoldingState.totalNumberOfEdgesInSubgraph;
                                    ghLocalOptimalMapping = new Dictionary<int, int>(currentAlgorithmHoldingState.gGetDictionaryOutOfMapping());
                                    hgLocalOptimalMapping = new Dictionary<int, int>(currentAlgorithmHoldingState.hGetDictionaryOutOfMapping());
                                }
                            }
                        }
                    }
                    #endregion

                    if (localBestScore == theoreticalMaximumScoreValue || getElapsedTimespan().CompareTo(timespanlimit) > 0)
                        break;
                }
            }

            if (computeInParallel)
                Parallel.For(0, batches, batch => batchDo(batch));
            else
                for (int batch = 0; batch < batches; batch += 1)
                    batchDo(batch);

            bestScore = localBestScore;
            subgraphEdges = localSubgraphEdges;
            ghOptimalMapping = ghLocalOptimalMapping;
            hgOptimalMapping = hgLocalOptimalMapping;
        }
    }
}
