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
            bool findExactMatch = false,
            int leftoverSteps = -1,
            int deepnessTakeawaySteps = 0
            )
        {
            if (!analyzeDisconnected && findExactMatch)
                throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");

            var initialScore = double.MinValue;
            Graph g, h;
            var solver = new CoreAlgorithm();
            var swappedGraphs = false;

            if (!findExactMatch && hArgument.EdgeCount < gArgument.EdgeCount)
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

            while (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(localBestScore) > 0)
            {
                var gMatchingVertex = g.Vertices.ArgMax(v => -g.Degree(v));

                foreach (var hMatchingVertex in h.Vertices)
                {
                    solver.InternalStateSetup(
                        gMatchingVertex,
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
                        findExactMatch,
                        leftoverSteps,
                        deepnessTakeawaySteps
                        );
                    solver.Recurse(ref localBestScore);
                }

                if (findExactMatch)
                    break;
                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
            }

            if (findExactMatch && ghLocalOptimalMapping.Count < gArgument.Vertices.Count)
            {
                // did not find an exact match
                bestScore = initialScore;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                // return the solution
                bestScore = localBestScore;
                subgraphEdges = localSubgraphEdges;
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
