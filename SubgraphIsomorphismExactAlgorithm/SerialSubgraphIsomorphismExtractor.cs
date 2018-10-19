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
            bool analyzeDisconnected,
            out T bestScore,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping
            )
        {
            UndirectedGraph g, h;
            var solver = new CoreAlgorithm<T>();
            var swappedGraphs = false;

            if (hArgument.Vertices.Count < gArgument.Vertices.Count)
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

            while (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(localBestScore) > 0)
            {
                var gMatchingVertex = g.Vertices.First();

                foreach (var hMatchingVertex in h.Vertices)
                {
                    solver.RecurseInitialMatch(gMatchingVertex, hMatchingVertex, g, h, graphScoringFunction, (newScore, ghMap, hgMap) =>
                    {
                        if (newScore.CompareTo(localBestScore) > 0)
                        {
                            localBestScore = newScore;
                            ghLocalOptimalMapping = new Dictionary<int, int>(ghMap);
                            hgLocalOptimalMapping = new Dictionary<int, int>(hgMap);
                        }
                    },
                    ref localBestScore, analyzeDisconnected);

                }

                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
            }


            // return the solution
            bestScore = localBestScore;
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
