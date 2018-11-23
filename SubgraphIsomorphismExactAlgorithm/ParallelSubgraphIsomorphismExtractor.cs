#define debug_

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
                var gMatchingCandidate = ClassesOfAbstraction(g).ArgMax(
                        classOfAbstraction => classOfAbstraction.Count,
                        classOfAbstraction => -g.VertexDegree(classOfAbstraction[0]),
                        classOfAbstraction => -removedVertices.Count(r => swappedGraphs ? hArgument.AreVerticesConnected(r, classOfAbstraction[0]) : gArgument.AreVerticesConnected(r, classOfAbstraction[0]))
                        )[0];

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
                            null
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
            var left = new HashSet<int>(Enumerable.Range(0, gGraphs.Count * classesOfAbstraction.Count));
            var leftSync = new object();

            Console.WriteLine($"Total vertices: {h.Vertices.Count}");
            Console.WriteLine($"g classes of abstraction: {ClassesOfAbstraction(swappedGraphs ? hArgument : gArgument).Count}");
            Console.WriteLine($"h classes of abstraction: {classesOfAbstraction.Count}");
#endif

            if (graphScoringFunction(h.Vertices.Count, h.EdgeCount) > localBestScore)
                Parallel.For(0, gGraphs.Count * hClassesOfAbstraction.Count, i =>
                {
                    var gIndex = i % gGraphs.Count;
                    var hIndex = i / gGraphs.Count;

                    if (graphScoringFunction(gGraphs[gIndex].Vertices.Count, gGraphs[gIndex].EdgeCount) > localBestScore)
                    {
                        new CoreAlgorithm()
                        .InternalStateSetup(
                            gInitialVertices[gIndex],
                            hClassesOfAbstraction[hIndex][0],
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
                            heuristicDeepnessToStartCountdown,
                            checkForAutomorphism: hClassesOfAbstraction.Count < h.Vertices.Count && !analyzeDisconnectedComponents
                        )
                        .Recurse(ref localBestScore);
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
                                Console.WriteLine($"item left: g: {gInitialVertices[item % gGraphs.Count]} h: {classesOfAbstraction[item / gGraphs.Count][0]}");
                            }
                        }
                    }
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
