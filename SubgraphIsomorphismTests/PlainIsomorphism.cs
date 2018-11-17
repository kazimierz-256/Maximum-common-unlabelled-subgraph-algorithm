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
        [InlineData(9)]
        public void TwoCliquesConnectedByChain(int max)
        {
            for (int i = 4; i < max; i++)
            {
                for (int j = 3; j <= i; j++)
                {
                    var g = GraphFactory.GenerateCliquesConnectedByChain(i, j, 5).Permute(0);
                    var h = GraphFactory.GenerateCliquesConnectedByChain(i, j, 4).Permute(1);

                    // verify result

                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                        g,
                        h,
                        (vertices, edges) => vertices + edges,
                        out var score,
                        out var subgraphEdges,
                        out var gToH,
                        out var hToG,
                        true,
                        false
                        );

                    Assert.NotEmpty(gToH);
                    Assert.NotEmpty(hToG);
                    // verify the solution
                    Assert.Equal((i * (i - 1) / 2 + j * (j - 1) / 2 + 2) + (i + j + 2), score);
                    Assert.Equal(i * (i - 1) / 2 + j * (j - 1) / 2 + 2, subgraphEdges);
                    Assert.Equal(i + j + 2, gToH.Count);
                    Assert.Equal(i + j + 2, hToG.Count);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        [Theory]
        [InlineData(10)]
        public void TwoCliquesConnectedByChainWithTriangle(int max)
        {
            for (int i = 4; i < max; i++)
            {
                for (int j = 3; j <= i; j++)
                {
                    var vertices1 = new HashSet<int>(Enumerable.Range(0, i + j + 4));
                    var vertices2 = new HashSet<int>(Enumerable.Range(0, i + j + 3));

                    var edges1 = new Dictionary<int, HashSet<int>>();
                    var edges2 = new Dictionary<int, HashSet<int>>();

                    foreach (var vertex in vertices1)
                        edges1.Add(vertex, new HashSet<int>());
                    foreach (var vertex in vertices2)
                        edges2.Add(vertex, new HashSet<int>());

                    void connect(Dictionary<int, HashSet<int>> edges, int a, int b)
                    {
                        edges[a].Add(b);
                        edges[b].Add(a);
                    }

                    // construct clique 1 and 2
                    for (int i1 = 0; i1 < i; i1++)
                        for (int i1helper = 0; i1helper < i1; i1helper++)
                        {
                            connect(edges1, i1, i1helper);
                            connect(edges2, i1, i1helper);
                        }

                    for (int j1 = i; j1 < i + j; j1++)
                        for (int j1helper = i; j1helper < j1; j1helper++)
                        {
                            connect(edges1, j1, j1helper);
                            connect(edges2, j1, j1helper);
                        }

                    connect(edges1, 0, i + j);
                    connect(edges1, i + j, i + j + 1);
                    connect(edges1, i + j + 1, i + j + 2);
                    connect(edges1, i + j + 2, i + j + 3);
                    connect(edges1, i + j + 1, i + j + 3);
                    connect(edges1, i + j + 3, i);

                    connect(edges2, 0, i + j);
                    connect(edges2, i + j, i + j + 1);
                    connect(edges2, i + j + 1, i + j + 2);
                    connect(edges2, i + j + 2, i);

                    // shuffle them
                    var randomSeed = 12;
                    var g = new Graph(edges1, vertices1, i * (i - 1) / 2 + j * (j - 1) / 2 + 6).Permute(10 - randomSeed);
                    var h = new Graph(edges2, vertices2, i * (i - 1) / 2 + j * (j - 1) / 2 + 4).Permute(2 + randomSeed * randomSeed);

                    // verify result

                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                        g,
                        h,
                        (vertices, edges) => vertices * edges,
                        out var score,
                        out var subgraphEdges,
                        out var gToH,
                        out var hToG,
                        false,
                        false
                        );

                    Assert.NotEmpty(gToH);
                    Assert.NotEmpty(hToG);
                    // verify the solution
                    Assert.Equal(i + j + 3, gToH.Count);
                    Assert.Equal(i + j + 3, hToG.Count);
                    Assert.Equal(i * (i - 1) / 2 + j * (j - 1) / 2 + 4, subgraphEdges);
                    Assert.Equal((i * (i - 1) / 2 + j * (j - 1) / 2 + 4) * (i + j + 3), score);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        [Theory]
        [InlineData(5, 10000, 0.5, 24, 41)]
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
                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                        g,
                        h,
                        (vertices, edges) => vertices,
                        out var score,
                        out var subgraphEdges,
                        out var gToH,
                        out var hToG,
                        false,
                        false
                        );
                    Assert.NotEmpty(gToH);
                    Assert.NotEmpty(hToG);

                    // verify the solution
                    var maximumConnectedComponentSize = g.ExtractAllConnectedComponents().Max(cc => cc.Count);
                    Assert.Equal(maximumConnectedComponentSize, gToH.Count);
                    Assert.Equal(maximumConnectedComponentSize, hToG.Count);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }
        [Theory]
        [InlineData(7, 10000, 0.5, 24, 41)]
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
                    var random = new Random(j);

                    // randomize a graph of given n and density
                    var g = GraphFactory.GenerateRandom(i, density, generatingSeed + j * j);
                    var h = GraphFactory.GeneratePermuted(g, permutingSeed - j);

                    for (int removed = 0; removed < i; removed++)
                    {
                        // run the algorithm
                        SubgraphIsomorphismExactAlgorithm.SerialSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                            g,
                            h,
                            (vertices, edges) => vertices,
                            out var score,
                            out var subgraphEdges,
                            out var gToH,
                            out var hToG,
                            true,
                            true
                            );
                        Assert.NotEmpty(gToH);
                        Assert.NotEmpty(hToG);
                        // verify the solution
                        Assert.Equal(g.Vertices.Count, gToH.Count);
                        Assert.Equal(g.Vertices.Count, hToG.Count);
                        Assert.Equal(g.EdgeCount, subgraphEdges);

                        AreTransitionsCorrect(gToH, hToG);
                        HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                        g.RemoveVertex(g.Vertices.Skip(random.Next(g.Vertices.Count)).First());
                    }
                }
            }
        }

        [Theory]
        [InlineData(5, 10000, 0.5, 24)]
        public void GraphOfQuadrupleSize(int n, int repetitions, double density, int generatingSeed)
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
                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                        g,
                        h,
                        (vertices, edges) => edges,
                        out var score,
                        out var subgraphEdges,
                        out var gToH,
                        out var hToG,
                        false,
                        false
                        );
                    Assert.NotEmpty(gToH);
                    Assert.NotEmpty(hToG);
                    Assert.Equal(score, subgraphEdges);
                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        [Theory]
        [InlineData(5, 100, 0.5, 24)]
        public void Approximating1GraphOfSizeAtMostDouble(int n, int repetitions, double density, int generatingSeed)
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
                    SubgraphIsomorphismExactAlgorithm.SubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                        g,
                        h,
                        (vertices, edges) => vertices,
                        out var score,
                        out var subgraphEdges,
                        out var gToH,
                        out var hToG,
                        false,
                        false,
                        1000
                        );
                    Assert.NotEmpty(gToH);
                    Assert.NotEmpty(hToG);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        [Theory]
        [InlineData(6, 24)]
        public void ApproximatingAlgorithmIsNotBetterThanActual(int n, int generatingSeed)
        {
            for (int i = 1; i < n; i++)
            {
                for (int j = 1; j < i; j++)
                {
                    for (double density = 0.1; density < 1d; density += 0.1)
                    {
                        // randomize a graph of given n and density
                        var g = GraphFactory.GenerateRandom(j, density, generatingSeed + j * j + i);
                        var h = GraphFactory.GenerateRandom(i, density, generatingSeed * generatingSeed - j);
                        foreach (var valuation in new Func<int, int, double>[] { (v, e) => v })
                        {
                            // run the algorithm
                            SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                                g,
                                h,
                                valuation,
                                out var score,
                                out var subgraphEdges,
                                out var gToH,
                                out var hToG,
                                false,
                                false
                                );
                            SubgraphIsomorphismExactAlgorithm.SubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                                g,
                                h,
                                valuation,
                                out var approximateScore,
                                out var _,
                                out var __,
                                out var ___,
                                false,
                                false,
                                1000,
                                milisecondTimeLimit: 100d
                                );
                            Assert.True(approximateScore <= score);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(5, 100, 0.5, 24)]
        public void Approximating2GraphOfSizeAtMostDouble(int n, int repetitions, double density, int generatingSeed)
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
                    SubgraphIsomorphismExactAlgorithm.ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                        g,
                        h,
                        (vertices, edges) => vertices,
                        out var score,
                        out var subgraphEdges,
                        out var gToH,
                        out var hToG,
                        false,
                        false,
                        heuristicStepsAvailable: (g.EdgeCount + h.EdgeCount + g.Vertices.Count + h.Vertices.Count) * 20
                        );
                    Assert.NotEmpty(gToH);
                    Assert.NotEmpty(hToG);

                    AreTransitionsCorrect(gToH, hToG);
                    HasSubgraphCorrectIsomorphism(g, h, gToH, hToG);
                }
            }
        }

        private void AreTransitionsCorrect(Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            foreach (var transitionFunction in gToH)
                Assert.Equal(transitionFunction.Key, hToG[transitionFunction.Value]);
        }

        private void HasSubgraphCorrectIsomorphism(Graph g, Graph h, Dictionary<int, int> gToH, Dictionary<int, int> hToG)
        {
            // for each pair of g there is and edge iff there is an edge in corresponding h equivalent pair
            foreach (var gVertex1 in gToH.Keys)
                foreach (var gVertex2 in gToH.Keys.Where(vertex => vertex != gVertex1))
                    Assert.Equal(g.AreVerticesConnected(gVertex1, gVertex2), h.AreVerticesConnected(gToH[gVertex1], gToH[gVertex2]));

            foreach (var hVertex1 in hToG.Keys)
                foreach (var hVertex2 in hToG.Keys.Where(vertex => vertex != hVertex1))
                    Assert.Equal(h.AreVerticesConnected(hVertex1, hVertex2), g.AreVerticesConnected(hToG[hVertex1], hToG[hVertex2]));
        }
    }
}
