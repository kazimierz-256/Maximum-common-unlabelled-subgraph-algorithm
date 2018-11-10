﻿using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            bool analyzeDisconnectedComponents = false,
            bool findGraphGinH = false,
            int heuristicStepsAvailable = -1,
            int heuristicDeepnessToStartCountdown = 0
            )
        {
            if (!analyzeDisconnectedComponents && findGraphGinH)
                throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");

            var initialScore = double.MinValue;
            var swappedGraphs = false;
            Graph g;
            Graph h;

            if (!findGraphGinH && hArgument.EdgeCount < gArgument.EdgeCount)
            {
                swappedGraphs = true;
                h = gArgument;
                g = hArgument.DeepClone();
            }
            else
            {
                g = gArgument.DeepClone();
                h = hArgument;
            }

            var gGraphs = new List<Graph>();
            var gInitialVertices = new List<int>();
            var removedVertices = new HashSet<int>();

            // repeat until the resulting graph is nonempty
            while (g.Vertices.Count > 0)
            {
                // choose a vertex that has the smallest degree, in case of ambiguity choose the one that has the least connections to those already removed
                var gMatchingVertex = g.Vertices.ArgMax(
                    v => -g.Degree(v),
                    v => -removedVertices.Count(r => gArgument.ExistsConnectionBetween(r, v))
                    );

                gGraphs.Add(g.DeepClone());
                gInitialVertices.Add(gMatchingVertex);

                // do not remove vertices from G if requested to find G within H
                if (findGraphGinH)
                    break;

                g.RemoveVertex(gMatchingVertex);
                removedVertices.Add(gMatchingVertex);
            }

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();
            var localSubgraphEdges = 0;
            var threadSynchronizingObject = new object();
            var hVerticesOrdered = h.Vertices.ToArray();

            Parallel.For(0, gGraphs.Count * hVerticesOrdered.Length, i =>
            {
                var gIndex = i % gGraphs.Count;
                var hIndex = i / gGraphs.Count;

                var threadAlgorithm = new CoreAlgorithm();
                threadAlgorithm.InternalStateSetup(
                    gInitialVertices[gIndex],
                    hVerticesOrdered[hIndex],
                    gGraphs[gIndex].DeepClone(),
                    h,
                    graphScoringFunction,
                    (newScore, ghMap, hgMap, edges) =>
                      {
                          if (newScore.CompareTo(localBestScore) > 0)
                              // to increase the performance lock is performed only if there is a chance to improve the local result
                              lock (threadSynchronizingObject)
                                  if (newScore.CompareTo(localBestScore) > 0)
                                  {
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
                    heuristicDeepnessToStartCountdown
                );
                threadAlgorithm.Recurse(ref localBestScore);
            });

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
