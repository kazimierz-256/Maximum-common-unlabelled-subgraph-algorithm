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
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping
            )
        {
            var swappedGraphs = false;
            UndirectedGraph g;
            UndirectedGraph h;

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

            var gGraphs = new UndirectedGraph[g.Vertices.Count];
            var gInitialVertices = new int[g.Vertices.Count];

            while (g.Vertices.Count > 0)
            {
                gInitialVertices[g.Vertices.Count - 1] = g.Vertices.First();
                gGraphs[g.Vertices.Count - 1] = g.DeepClone();

                // ignore previous g-vertices
                g.RemoveVertex(gInitialVertices[g.Vertices.Count - 1]);
            }

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();
            var lockingObject = new object();

            Parallel.For(0, gGraphs.Length, iter =>
            {
                // try matching all h's
                foreach (var hVertex in h.Vertices)
                {
                    var subLeverager = new CoreAlgorithm<T>();
                    subLeverager.RecurseInitialMatch(gInitialVertices[iter], hVertex, gGraphs[iter], h, graphScoringFunction, initialScore, (newScore, ghMap, hgMap) =>
                    {
                        if (newScore.CompareTo(localBestScore) > 0)
                        {
                            lock (lockingObject)
                            {
                                if (newScore.CompareTo(localBestScore) > 0)
                                {
                                    localBestScore = newScore;
                                    ghLocalOptimalMapping = ghMap;
                                    hgLocalOptimalMapping = hgMap;
                                }
                            }
                        }
                    },
                    () => localBestScore);
                }
            });

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
