using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SubgraphIsomorphismTests
{
    public class PlainIsomorphism
    {
        [Theory]
        [InlineData(18, 0.7, 24, 41)]
        public void GraphOfSizeAtMost(int n, double density, int generatingSeed, int permutingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                // randomize a graph of given n and density
                var g = GraphFactory.GenerateRandom(i, density, generatingSeed);
                var h = GraphFactory.GeneratePermuted(g, permutingSeed);

                // run the algorithm
                var solver = new SubgraphIsomorphismExactAlgorithm.AlphaSubgraphIsomorphismExtractor<int>();
                solver.ExtractOptimalSubgraph(g, h, (vertices, edges) => vertices, 0, out int score, out var gToH, out var hToG);

                // verify the solution
                Assert.True(HasSubgraphCorrectIsomorphism(g, h, gToH, hToG));
                var maximumConnectedComponentSize = g.ConnectedComponents().Max(cc => cc.Count);
                Assert.Equal(maximumConnectedComponentSize, gToH.Count);
                Assert.Equal(maximumConnectedComponentSize, hToG.Count);

                Assert.True(AreTransitionsCorrect(gToH, hToG));
                Assert.True(HasSubgraphCorrectIsomorphism(g, h, gToH, hToG));
            }
        }
        [Theory]
        [InlineData(15, 0.7, 24)]
        public void GraphOfSizeAtMostDouble(int n, double density, int generatingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                // randomize a graph of given n and density
                var g = GraphFactory.GenerateRandom(4 * i, density, generatingSeed);
                var h = GraphFactory.GenerateRandom(i, density, generatingSeed * generatingSeed);

                // run the algorithm
                var solver = new SubgraphIsomorphismExactAlgorithm.ParallelLeverager<int>();
                solver.ExtractOptimalSubgraph(g, h, (vertices, edges) => vertices, 0, out int score, out var gToH, out var hToG);

                Assert.True(AreTransitionsCorrect(gToH, hToG));
                Assert.True(HasSubgraphCorrectIsomorphism(g, h, gToH, hToG));
            }
        }

        private bool AreTransitionsCorrect(Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            foreach (var transitionFunction in gToH)
            {
                if (transitionFunction.Key != hToG[transitionFunction.Value])
                {
                    return false;
                }
            }
            return true;
        }

        private bool HasSubgraphCorrectIsomorphism(UndirectedGraph g, UndirectedGraph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            // for each pair of g there is and edge iff there is an edge in corresponding h equivalent pair
            foreach (var gVertex1 in gToH.Keys)
            {
                foreach (var gVertex2 in gToH.Keys.Where(vertex => vertex != gVertex1))
                {
                    if (g.ExistsConnectionBetween(gVertex1, gVertex2) != h.ExistsConnectionBetween(gToH[gVertex1], gToH[gVertex2]))
                    {
                        return false;
                    }
                }
            }

            foreach (var hVertex1 in hToG.Keys)
            {
                foreach (var hVertex2 in hToG.Keys.Where(vertex => vertex != hVertex1))
                {
                    if (h.ExistsConnectionBetween(hVertex1, hVertex2) != g.ExistsConnectionBetween(hToG[hVertex1], hToG[hVertex2]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool VerifyFullSubgraphIsomorphism(UndirectedGraph g, UndirectedGraph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {

            // all edges in g egsist in h
            foreach (var connection in g.Neighbours)
            {
                var gFromVertex = connection.Key;
                foreach (var gToVertex in connection.Value)
                {
                    if (!h.ExistsConnectionBetween(gToH[gFromVertex], gToH[gToVertex]))
                    {
                        return false;
                    }
                }
            }

            // all edges in h egsist in g
            foreach (var connection in h.Neighbours)
            {
                var hFromVertex = connection.Key;
                foreach (var hToVertex in connection.Value)
                {
                    if (!g.ExistsConnectionBetween(hToG[hFromVertex], hToG[hToVertex]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
