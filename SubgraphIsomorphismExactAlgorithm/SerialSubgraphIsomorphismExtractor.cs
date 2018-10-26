using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismExtractor<T> where T : IComparable
    {
        public static void ExtractOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, T> graphScoringFunction,
            T initialScore,
            out T bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            UndirectedGraph g, h;
            var solver = new CoreAlgorithm<T>();
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
                var gMatchingVertex = -1;
                var gMatchingScore = int.MaxValue;

                foreach (var gCandidate in g.Vertices)
                {
                    if (g.Degree(gCandidate) < gMatchingScore)
                    {
                        gMatchingScore = g.Degree(gCandidate);
                        gMatchingVertex = gCandidate;
                    }
                }

                foreach (var hMatchingVertex in h.Vertices)
                {
                    solver.HighLevelSetup(gMatchingVertex, hMatchingVertex, g, h, graphScoringFunction, (newScore, ghMap, hgMap, edges, depth) =>
                    {
                        if (newScore.CompareTo(localBestScore) > 0)
                        {
                            localBestScore = newScore;
                            ghLocalOptimalMapping = ghMap();
                            hgLocalOptimalMapping = hgMap();
                            localSubgraphEdges = edges;
                        }
                    },
                    analyzeDisconnected, findExactMatch);
                    solver.Recurse(ref localBestScore);
                }

                if (findExactMatch)
                    break;
                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
            }


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
