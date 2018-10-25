using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class ParallelSubgraphIsomorphismExtractor<T> where T : IComparable
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
            var swappedGraphs = false;
            UndirectedGraph g;
            UndirectedGraph h;

            if (hArgument.EdgeCount < gArgument.EdgeCount)
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

            var gGraphs = new List<UndirectedGraph>();
            var gInitialVertices = new List<int>();

            while (g.Vertices.Count > 0)
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

                gGraphs.Add(g.DeepClone());
                gInitialVertices.Add(gMatchingVertex);

                if (findExactMatch)
                    break;
                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
            }

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();
            var localSubgraphEdges = 0;
            var lockingObject = new object();
            var hVertices = h.Vertices.ToArray();
            Parallel.For(0, gGraphs.Count * hVertices.Length, iter =>
            {
                var gIndex = iter % gGraphs.Count;
                var hIndex = iter / gGraphs.Count;
                // try matching all h's
                var subLeverager = new CoreAlgorithm<T>();
                subLeverager.HighLevelSetup(gInitialVertices[gIndex], hVertices[hIndex], gGraphs[gIndex].DeepClone(), h, graphScoringFunction, (newScore, ghMap, hgMap, edges) =>
                {
                    if (newScore.CompareTo(localBestScore) > 0)
                    {
                        lock (lockingObject)
                        {
                            if (newScore.CompareTo(localBestScore) > 0)
                            {
                                localBestScore = newScore;
                                ghLocalOptimalMapping = ghMap();
                                hgLocalOptimalMapping = hgMap();
                                localSubgraphEdges = edges;
                            }
                        }
                    }
                },
                analyzeDisconnected, findExactMatch);
                subLeverager.Recurse(ref localBestScore);
            });

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
