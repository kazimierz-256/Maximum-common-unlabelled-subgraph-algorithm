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
        [InlineData(11, 0.9, 0, 1)]
        public void GraphOfSizeAtMost(int n, double density, int generatingSeed, int permutingSeed)
        {
            // randomize a graph of given n and density
            var g = GraphFactory.GenerateRandom(n, density, generatingSeed);
            var h = GraphFactory.GeneratePermuted(g, permutingSeed);

            // run the algorithm
            var solver = new SubgraphIsomorphismExactAlgorithm.AlphaSubgraphIsomorphismExtractor<int>();
            solver.Extract(g, h, (vertices, edges) => vertices + edges, 0, out int score, out var gToH, out var hToG);

            // verify the solution
            Assert.True(VerifySubgraphIsomorphism(g, h, gToH, hToG));
            Assert.Equal(g.VertexCount, gToH.Count);
        }

        private bool VerifySubgraphIsomorphism(UndirectedGraph g, UndirectedGraph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {

            // all edges in g egsist in h
            foreach (var connection in g.EnumerateConnections())
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
            foreach (var connection in h.EnumerateConnections())
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
