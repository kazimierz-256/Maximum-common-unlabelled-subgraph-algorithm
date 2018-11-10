using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismExtractor
    {
        public static void ExtractOptimalSubgraph(
            Graph gArgument,
            Graph hArgument,
            Func<int, int, double> graphScoringFunction,
            out double bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findGraphGinH = false,
            int leftoverSteps = -1,
            int deepnessTakeawaySteps = 0
            )
        {
            if (!analyzeDisconnected && findGraphGinH)
                throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");

            var initialScore = double.MinValue;
            Graph g, h;
            var solver = new CoreAlgorithm();
            var swappedGraphs = false;

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

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();
            var localSubgraphEdges = 0;

            if (graphScoringFunction(h.Vertices.Count, h.EdgeCount).CompareTo(localBestScore) > 0d)
                while (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(localBestScore) > 0d)
                {
                    var gMatchingCandidate = g.Vertices.ArgMax(v => -g.VertexDegree(v));

                    foreach (var hMatchingVertex in h.Vertices)
                    {
                        solver.InternalStateSetup(
                            gMatchingCandidate,
                            hMatchingVertex,
                            g,
                            h,
                            graphScoringFunction,
                            (newScore, ghMap, hgMap, edges) =>
                            {
                                if (newScore.CompareTo(localBestScore) > 0)
                                {
                                    localBestScore = newScore;
                                    ghLocalOptimalMapping = ghMap();
                                    hgLocalOptimalMapping = hgMap();
                                    localSubgraphEdges = edges;
                                }
                            },
                            analyzeDisconnected,
                            findGraphGinH,
                            leftoverSteps,
                            deepnessTakeawaySteps
                            );
                        solver.Recurse(ref localBestScore);
                    }

                    if (findGraphGinH)
                        break;

                    // repeat the procedure but without the considered vertex
                    g.RemoveVertex(gMatchingCandidate);
                }

            if (findGraphGinH && ghLocalOptimalMapping.Count < gArgument.Vertices.Count)
            {
                // did not find an exact match
                bestScore = initialScore;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                // return found  solution
                bestScore = localBestScore;
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
