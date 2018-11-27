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
                for (int batch = 0; batch < batches; batch += 1)
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
            CoreAlgorithm currentAlgorithmHoldingState = null;
            // while there is an increase in result continue to approximate

            var step = 0;
#if true
            // quit the loop once a local maximum (scoring function) is reached
            while (true)
            {
                if (step == 0)
                {
                    var gSkip = random.Next(gVertices.Count);
                    var hSkip = random.Next(hVertices.Count);
                    var gCandidate = gVertices.Skip(gSkip).First();
                    var hCandidate = hVertices.Skip(hSkip).First();
                    currentAlgorithmHoldingState = new CoreAlgorithm()
                    {
                        g = null,
                        h = null,
                        findGraphGinH = findExactMatch,
                        analyzeDisconnected = analyzeDisconnected,
                        subgraphScoringFunction = graphScoringFunction,
                        gMapping = new int[Math.Min(gVertices.Count, hVertices.Count)],
                        hMapping = new int[Math.Min(gVertices.Count, hVertices.Count)],
                        gEnvelope = new int[gVertices.Count],
                        gEnvelopeLimit = 1,
                        hEnvelope = new int[hVertices.Count],
                        hEnvelopeLimit = 1,
                        gOutsiders = gVertices.Where(v => v != gCandidate).ToArray(),
                        hOutsiders = hVertices.Where(v => v != hCandidate).ToArray(),
                        totalNumberOfEdgesInSubgraph = 0,

                        gConnectionExistence = gConnectionExistence,
                        hConnectionExistence = hConnectionExistence,
                    };
                    currentAlgorithmHoldingState.gEnvelope[0] = gCandidate;
                    currentAlgorithmHoldingState.hEnvelope[0] = hCandidate;
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
            if (findExactMatch && currentAlgorithmHoldingState.mappingCount < gVertices.Count)
            {
#endif
                // did not find an exact match, simply return initial values
                bestScore = double.MinValue;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
#if true
            }
            else
            {
                bestScore = graphScoringFunction(currentAlgorithmHoldingState.mappingCount, currentAlgorithmHoldingState.totalNumberOfEdgesInSubgraph);
                subgraphEdges = currentAlgorithmHoldingState.totalNumberOfEdgesInSubgraph;
                ghOptimalMapping = currentAlgorithmHoldingState.gGetDictionaryOutOfMapping();
                hgOptimalMapping = currentAlgorithmHoldingState.hGetDictionaryOutOfMapping();
            }
#endif
        }
    }
}
