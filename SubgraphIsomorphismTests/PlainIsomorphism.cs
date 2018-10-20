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
        [InlineData(5, 500000, 0.9, 24, 41)]
        public void GraphIsomorphismConnnected(int n, int repetitions, double density, int generatingSeed, int permutingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                var max = repetitions;
                if (i == 2)
                {
                    max = 1;
                }
                for (int j = 0; j < max; j++)
                {

                    // randomize a graph of given n and density
                    var g = GraphFactory.GenerateRandom(i, density, generatingSeed + j * j);
                    var h = GraphFactory.GeneratePermuted(g, permutingSeed - j);

                    // run the algorithm
                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor<int>.ExtractOptimalSubgraph(g, h, (vertices, edges) => vertices, 0, false, out var score, out var gToH, out var hToG);

                    // verify the solution
                    var maximumConnectedComponentSize = g.ConnectedComponents().Max(cc => cc.Count);
                    Assert.Equal(maximumConnectedComponentSize, gToH.Count);
                    Assert.Equal(maximumConnectedComponentSize, hToG.Count);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }
        [Theory]
        [InlineData(5, 500000, 0.9, 24, 41)]
        public void GraphIsomorphismDisconnected(int n, int repetitions, double density, int generatingSeed, int permutingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                var max = repetitions;
                if (i == 2)
                {
                    max = 1;
                }
                for (int j = 0; j < max; j++)
                {

                    // randomize a graph of given n and density
                    var g = GraphFactory.GenerateRandom(i, density, generatingSeed + j * j);
                    var h = GraphFactory.GeneratePermuted(g, permutingSeed - j);

                    // run the algorithm
                    SubgraphIsomorphismExactAlgorithm.SerialSubgraphIsomorphismExtractor<int>.ExtractOptimalSubgraph(g, h, (vertices, edges) => vertices, 0, true, out var score, out var gToH, out var hToG);

                    // verify the solution
                    Assert.Equal(g.Vertices.Count, gToH.Count);
                    Assert.Equal(g.Vertices.Count, hToG.Count);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        [Theory]
        [InlineData(5, 50000, 0.9, 24)]
        public void GraphOfSizeAtMostDouble(int n, int repetitions, double density, int generatingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                var max = repetitions;
                if (i == 2)
                {
                    max = 1;
                }
                for (int j = 0; j < max; j++)
                {

                    // randomize a graph of given n and density
                    var g = GraphFactory.GenerateRandom(4 * i, density, generatingSeed + j * j);
                    var h = GraphFactory.GenerateRandom(i, density, generatingSeed * generatingSeed - j);

                    // run the algorithm
                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor<int>.ExtractOptimalSubgraph(g, h, (vertices, edges) => vertices, 0, false, out int score, out var gToH, out var hToG);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        private void AreTransitionsCorrect(Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            foreach (var transitionFunction in gToH)
            {
                Assert.Equal(transitionFunction.Key, hToG[transitionFunction.Value]);
            }
        }

        private void HasSubgraphCorrectIsomorphism(UndirectedGraph g, UndirectedGraph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            // for each pair of g there is and edge iff there is an edge in corresponding h equivalent pair
            foreach (var gVertex1 in gToH.Keys)
            {
                foreach (var gVertex2 in gToH.Keys.Where(vertex => vertex != gVertex1))
                {
                    Assert.Equal(g.ExistsConnectionBetween(gVertex1, gVertex2), h.ExistsConnectionBetween(gToH[gVertex1], gToH[gVertex2]));
                }
            }

            foreach (var hVertex1 in hToG.Keys)
            {
                foreach (var hVertex2 in hToG.Keys.Where(vertex => vertex != hVertex1))
                {
                    Assert.Equal(h.ExistsConnectionBetween(hVertex1, hVertex2), g.ExistsConnectionBetween(hToG[hVertex1], hToG[hVertex2]));
                }
            }
        }

        private void VerifyFullSubgraphIsomorphism(UndirectedGraph g, UndirectedGraph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            // all edges in g exist in h
            foreach (var connection in g.Neighbours)
            {
                var gFromVertex = connection.Key;
                foreach (var gToVertex in connection.Value)
                {
                    Assert.True(h.ExistsConnectionBetween(gToH[gFromVertex], gToH[gToVertex]));
                }
            }

            // all edges in h exsist in g
            foreach (var connection in h.Neighbours)
            {
                var hFromVertex = connection.Key;
                foreach (var hToVertex in connection.Value)
                {
                    Assert.True(g.ExistsConnectionBetween(hToG[hFromVertex], hToG[hToVertex]));
                }
            }
        }
    }
}
