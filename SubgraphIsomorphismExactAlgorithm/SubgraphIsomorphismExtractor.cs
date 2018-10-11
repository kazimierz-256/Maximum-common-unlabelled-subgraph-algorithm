using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SubgraphIsomorphismExtractor<T> where T : IComparable
    {
        public int[][] Extract(
            Graph argG,
            Graph argH,
            Func<int, int, T> graphScore,
            Func<Graph, int, T> vertexScore,
            T defaultScore,
            out T score
            )
        {
            score = defaultScore;
            var bestSolutionVertices = new int[0][];
            Graph g;
            Graph h;

            // make sure graph G is smaller than graph H
            if (argG.VertexCount > argH.VertexCount)
            {
                g = argH.Clone();
                h = argG.Clone();
            }
            else
            {
                g = argG.Clone();
                h = argH.Clone();
            }

            SmallestLastOrder(h, out var hOrder, out var hVertexOrderIndex);
            while (g.VertexCount > 0)
            {
                SmallestLastOrder(g, out var gOrder, out var gVertexOrderIndex);

                // match g[0] with all possible h
                for (int i = 0; i < h.VertexCount; i++)
                {
                    var gAlreadyInSubgraph = Enumerable.Repeat(false, g.VertexCount).ToArray();
                    gAlreadyInSubgraph[gOrder[0]] = true;
                    var hAlreadyInSubgraph = Enumerable.Repeat(false, g.VertexCount).ToArray();
                    hAlreadyInSubgraph[hOrder[i]] = true;
                    Match(
                        0,
                        gAlreadyInSubgraph,
                        hAlreadyInSubgraph,
                        Enumerable.Repeat(1, g.VertexCount).ToArray(),
                        Enumerable.Repeat(1, h.VertexCount).ToArray(),
                        gOrder[0],
                        hOrder[i],
                        g,
                        h,
                        graphScore,
                        vertexScore,
                        ref score,
                        ref bestSolutionVertices
                        );
                }

                // consider the problem without g[0]
                g = g.CloneWithoutVertex(gOrder[0]);
            }

            // todo: make sure the arrays are sorted according to vertex index in G to
            return bestSolutionVertices;
        }

        private void SmallestLastOrder(Graph g, out int[] gOrder, out int[] gVertexOrderIndex) => throw new NotImplementedException();

        private void Match(
            int recursionDepth,
            bool[] gAlreadyInSubgraph,
            bool[] hAlreadyInSubgraph,
            int[] gNeighbourHashes,
            int[] hNeighbourHashes,
            int gVertex,
            int hVertex,
            Graph g,
            Graph h,
            Func<int, int, T> graphScore,
            Func<Graph, int, T> vertexScore,
            ref T score,
            ref int[][] bestSolutionVertices
            )
        {
            var prime = Primes.GetNthPrime(recursionDepth);

            // no particular order
            foreach (var neighbour in g.NeighboursOf(gVertex))
            {
                // append the new prime to neighbours if they're not already in subgraph
                if (!gAlreadyInSubgraph[neighbour])
                {
                    gNeighbourHashes[neighbour] *= prime;
                }
            }
            foreach (var neighbour in h.NeighboursOf(hVertex))
            {
                // append the new prime to neighbours if they're not already in subgraph
                if (!hAlreadyInSubgraph[neighbour])
                {
                    hNeighbourHashes[neighbour] *= prime;
                }
            }

            // toconsider: at each step choose either neighbours of g or neighbours of h whichever is more performant

            // todo: order neighbours of gVertex according to the function
            // -t-o-d-o-: order neighbours of gVertex according to smallest-last
            // choose the first one
            // match it with

            // try to match gNext to some other 

            // if no more vertices can be added check for score of common subgraph (or do it all the time)

            // perform more recursive match if applicable
            // filter only those that satisfy maximum criterion

            // at the end assume the vertex doesnt exist

        }
    }
}
