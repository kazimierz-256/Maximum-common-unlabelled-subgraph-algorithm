#define debug

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
            var random = new Random(0);
            while (g.Vertices.Count > 0)
            {
                // choose a vertex that has the smallest degree, in case of ambiguity choose the one that has the least connections to those already removed
                var gMatchingCandidate = -1;
                if (heuristicStepsAvailable == -1)
                    gMatchingCandidate = g.Vertices.Skip(random.Next(g.Vertices.Count)).First();
                else
                    gMatchingCandidate = g.Vertices.ArgMax(
                        v => -g.VertexDegree(v),
                        v => -removedVertices.Count(r => swappedGraphs ? hArgument.AreVerticesConnected(r, v) : gArgument.AreVerticesConnected(r, v))
                        );

                gGraphs.Add(g.DeepClone());
                gInitialVertices.Add(gMatchingCandidate);

                // do not remove vertices from G if requested to find G within H
                if (findGraphGinH)
                    break;

                g.RemoveVertex(gMatchingCandidate);
                removedVertices.Add(gMatchingCandidate);
            }

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();
            var localSubgraphEdges = 0;
            var threadSynchronizingObject = new object();
            var hVerticesOrdered = h.Vertices.ToArray();
#if debug
            var left = gGraphs.Count * hVerticesOrdered.Length;
#endif
            var automorphismAlgorithm = new CoreAlgorithm();
            var leftOverVertices = new HashSet<int>(h.Vertices);
            var classesOfAbstraction = new List<HashSet<int>>();
            while (leftOverVertices.Count > 0)
            {
                var considering = leftOverVertices.First();
                leftOverVertices.Remove(considering);
                var isomorphic = new HashSet<int>() { considering };
                foreach (var consideringVertex in leftOverVertices)
                {
                    var found = false;
                    automorphismAlgorithm.InternalStateSetup(
                            considering,
                            consideringVertex,
                            h,
                            h,
                            graphScoringFunction,
                            (newScore, ghMap, hgMap, edges) =>
                            {
                                found = true;
                            }
                        );
                    automorphismAlgorithm.Automorphism(ref found);
                    if (found)
                    {
                        isomorphic.Add(consideringVertex);
                    }
                }
                foreach (var vertex in isomorphic)
                {
                    leftOverVertices.Remove(vertex);
                }
                classesOfAbstraction.Add(isomorphic);
            }
#if debug

            Console.WriteLine($"Total vertices: {h.Vertices.Count}");
            Console.WriteLine(classesOfAbstraction.Count);
#endif

            if (graphScoringFunction(h.Vertices.Count, h.EdgeCount) > localBestScore)
                Parallel.For(0, gGraphs.Count * classesOfAbstraction.Count, i =>
                {
                    var gIndex = i % gGraphs.Count;
                    var hIndex = i / gGraphs.Count;

                    if (graphScoringFunction(gGraphs[gIndex].Vertices.Count, gGraphs[gIndex].EdgeCount) > localBestScore)
                    {
                        var threadAlgorithm = new CoreAlgorithm();
                        threadAlgorithm.InternalStateSetup(
                            gInitialVertices[gIndex],
                            classesOfAbstraction[hIndex].First(),
                            gGraphs[gIndex].DeepClone(),
                            h,
                            graphScoringFunction,
                            (newScore, ghMap, hgMap, edges) =>
                              {
                                  if (newScore > localBestScore)
                                      // to increase the performance lock is performed only if there is a chance to improve the local result
                                      lock (threadSynchronizingObject)
                                          if (newScore > localBestScore)
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
                    }
#if debug
                    Console.WriteLine($"Left: {Interlocked.Add(ref left, -1)}");
#endif
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
