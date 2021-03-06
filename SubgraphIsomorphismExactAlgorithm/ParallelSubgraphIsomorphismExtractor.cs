﻿#define debug_
#define parallel

using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class ParallelSubgraphIsomorphismExtractor
    {
        public static void ExtractOptimalSubgraph(
            Graph gArgument,
            Graph hArgument,
            Func<int, int, double> graphScoringFunction,
            out double subgraphScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnectedComponents,
            bool findGraphGinH,
            int heuristicStepsAvailable = -1,
            int heuristicDeepnessToStartCountdown = 0,
            double approximationRatio = 1d,
            bool induced = true
            )
        {
            if (!analyzeDisconnectedComponents && findGraphGinH)
                throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");

            var initialScore = double.MinValue;
            var swappedGraphs = false;
            Graph g;
            Graph h;

            if (!induced && !findGraphGinH && hArgument.EdgeCount < gArgument.EdgeCount)
            {
                swappedGraphs = true;
                h = gArgument;
                g = hArgument;
            }
            else
            {
                g = gArgument;
                h = hArgument;
            }

            var gInitialVertices = new List<int>();
            var allowed = new List<HashSet<int>>();
            var allowedEdgesUpperBound = new List<int>();
            var removedVertices = new HashSet<int>();

            // repeat until the resulting graph is nonempty
            var random = new Random(0);
            var gToDeconstruct = g.DeepClone();
            while (gToDeconstruct.Vertices.Count > 0)
            {
                // choose a vertex that has the smallest degree, in case of ambiguity choose the one that has the least connections to those already removed
                var gMatchingCandidate = gToDeconstruct.Vertices.ArgMax(
                        v => -gToDeconstruct.VertexDegree(v),
                        v => -removedVertices.Count(r => g.AreVerticesConnected(r, v))
                        );

                if (analyzeDisconnectedComponents)
                {
                    allowed.Add(new HashSet<int>(gToDeconstruct.Vertices));
                }
                else
                {
                    allowed.Add(gToDeconstruct.ExtractConnectedComponent(gMatchingCandidate));
                }
                allowedEdgesUpperBound.Add(gToDeconstruct.EdgeCount);
                gInitialVertices.Add(gMatchingCandidate);

                // do not remove vertices from G if requested to find G within H
                if (findGraphGinH)
                    break;

                gToDeconstruct.RemoveVertex(gMatchingCandidate);
                removedVertices.Add(gMatchingCandidate);
            }

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();
            var localSubgraphEdges = 0;
            var threadSynchronizingObject = new object();
            var hVerticesOrdered = h.Vertices.ToArray();
            List<List<int>> ClassesOfAbstraction(Graph graph)
            {
                var leftOverVertices = new HashSet<int>(graph.Vertices);
                var localClassesOfAbstraction = new List<List<int>>();
                while (leftOverVertices.Count > 0)
                {
                    var considering = leftOverVertices.First();
                    leftOverVertices.Remove(considering);
                    var isomorphic = new List<int>() { considering };
                    foreach (var consideringVertex in leftOverVertices)
                    {
                        var found = false;
                        new CoreAlgorithm()
                        .InternalStateSetup(
                            considering,
                            consideringVertex,
                            graph,
                            graph,
                            null,
                            null,
                            optimizeForAutomorphism: true
                        ).RecurseAutomorphism(ref found);

                        if (found)
                            isomorphic.Add(consideringVertex);
                    }
                    foreach (var vertex in isomorphic)
                        leftOverVertices.Remove(vertex);

                    localClassesOfAbstraction.Add(isomorphic);
                }
                return localClassesOfAbstraction;
            }
            var hClassesOfAbstraction = ClassesOfAbstraction(h);
#if debug
            var left = new HashSet<int>(Enumerable.Range(0, gGraphs.Count * hClassesOfAbstraction.Count));
            var leftSync = new object();

            Console.WriteLine($"Total vertices: {h.Vertices.Count}");
            Console.WriteLine($"g classes of abstraction: {ClassesOfAbstraction(swappedGraphs ? hArgument : gArgument).Count}");
            Console.WriteLine($"h classes of abstraction: {hClassesOfAbstraction.Count}");
#endif
            // don't check for automorphism when there might be disconnected components!


#if parallel
            Parallel.For(0, allowed.Count * hClassesOfAbstraction.Count, i =>
#else
                for (int i = 0; i < gGraphs.Count * hClassesOfAbstraction.Count; i += 1)
#endif
            {
                var gIndex = i % allowed.Count;
                var hIndex = i / allowed.Count;

                if (graphScoringFunction(allowed[gIndex].Count, allowedEdgesUpperBound[gIndex]) * approximationRatio > localBestScore)
                {
                    new CoreAlgorithm().InternalStateSetup(
                        gInitialVertices[gIndex],
                        hClassesOfAbstraction[hIndex][0],
                        g,
                        h,
                        graphScoringFunction,
                        (newScore, ghMap, hgMap, edges) =>
                          {
                              if (newScore > localBestScore)
                                  // to increase the performance lock is performed only if there is a chance to improve the local result
                                  lock (threadSynchronizingObject)
                                      if (newScore > localBestScore)
                                      {
#if debug
                                              Console.WriteLine($"New score: {newScore} (previously {localBestScore})");
#endif
                                          localBestScore = newScore;
                                          // lazy evaluation for best performance
                                          ghLocalOptimalMapping = ghMap();
                                          hgLocalOptimalMapping = hgMap();
                                          localSubgraphEdges = edges;
                                      }
                          },
                        analyzeDisconnectedComponents,
                        findGraphGinH,
                        heuristicStepsAvailable,
                        heuristicDeepnessToStartCountdown,
                        approximationRatio: approximationRatio,
                        induced: induced,
                        gAllowedSubsetVertices: allowed[gIndex]
                    ).Recurse(ref localBestScore);
                }
#if debug
                    lock (leftSync)
                    {
                        left.Remove(i);
                        Console.WriteLine($"Left: {left.Count}");
                        if (left.Count < 6)
                        {
                            foreach (var item in left)
                            {
                                Console.WriteLine($"item left: g: {gInitialVertices[item % gGraphs.Count]} h: {hClassesOfAbstraction[item / gGraphs.Count][0]}");
                            }
                        }
                    }
#endif
#if parallel
            });
#else
                }
#endif


            // if requested to find G within H and could not find such then quit with dummy results
            if (findGraphGinH && ghLocalOptimalMapping.Count < gArgument.Vertices.Count)
            {
                subgraphScore = initialScore;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                // return found solution
                subgraphScore = localBestScore;
                subgraphEdges = localSubgraphEdges;

                // swap again to return correct answers
                if (swappedGraphs)
                {
                    ghOptimalMapping = hgLocalOptimalMapping;
                    hgOptimalMapping = ghLocalOptimalMapping;
                }
                else
                {
                    ghOptimalMapping = ghLocalOptimalMapping;
                    hgOptimalMapping = hgLocalOptimalMapping;
                }
            }
        }
    }
}
