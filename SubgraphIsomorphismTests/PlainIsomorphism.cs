using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using GraphExtensionAlgorithms;

namespace SubgraphIsomorphismTests
{
    public class PlainIsomorphism
    {
        [Theory]
        [InlineData(13, 0.7, 24, 41)]
        public void GraphOfSizeAtMost(int n, double density, int generatingSeed, int permutingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                // randomize a graph of given n and density
                var g = GraphFactory.GenerateRandom(i, density, generatingSeed);
                var h = GraphFactory.GeneratePermuted(g, permutingSeed);

                // run the algorithm
                var solver = new SubgraphIsomorphismExactAlgorithm.AlphaSubgraphIsomorphismExtractor<int>();
                solver.Extract(g, h, (vertices, edges) => vertices, 0, out int score, out var gToH, out var hToG);

                // verify the solution
                Assert.True(VerifySubgraphIsomorphism(g, h, gToH, hToG));
                var maximumConnectedComponentSize = g.ConnectedComponents().Max(cc => cc.Count);
                Assert.Equal(maximumConnectedComponentSize, gToH.Count);
                Assert.Equal(maximumConnectedComponentSize, hToG.Count);

                foreach (var transitionFunction in gToH)
                {
                    Assert.Equal(transitionFunction.Key, hToG[transitionFunction.Value]);
                }
            }
        }

        private bool VerifySubgraphIsomorphism(UndirectedGraph g, UndirectedGraph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
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
