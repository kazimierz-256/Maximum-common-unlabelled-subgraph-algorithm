using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SubgraphIsomorphismExtractor<T> where T : IComparable
    {
        public void Extract(
            Graph argG,
            Graph argH,
            Func<int, int, T> graphScore,
            Func<Graph, int, T> vertexScore,
            T defaultScore,
            out T score,
            out int[] gBestSolution,
            out int[] hBestSolution
            )
        {
            score = defaultScore;
            gBestSolution = new int[0];
            hBestSolution = new int[0];
            Graph g;
            Graph h;

            // make sure graph G is smaller than graph H (?)
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
                    var hAlreadyInSubgraph = Enumerable.Repeat(false, g.VertexCount).ToArray();

                    gAlreadyInSubgraph[gOrder[0]] = true;
                    hAlreadyInSubgraph[hOrder[i]] = true;

                    Match(
                        0,
                        gAlreadyInSubgraph,
                        hAlreadyInSubgraph,
                        Enumerable.Repeat(1L, g.VertexCount).ToArray(),
                        Enumerable.Repeat(1L, h.VertexCount).ToArray(),
                        gOrder[0],
                        hOrder[i],
                        g.Clone(),
                        h.Clone(),
                        graphScore,
                        vertexScore,
                        ref score,
                        ref gBestSolution,
                        ref hBestSolution
                        );
                }

                // consider the problem without g[0]
                g.RemoveVertex(gOrder[0]);
            }

            // todo: make sure the arrays are sorted according to vertex index in G to
        }

        private void SmallestLastOrder(Graph g, out int[] gOrder, out int[] gVertexOrderIndex) => throw new NotImplementedException();

        private void Match(
            int recursionDepth,
            bool[] gAlreadyInSubgraph,
            bool[] hAlreadyInSubgraph,
            long[] gNeighbourHashes,
            long[] hNeighbourHashes,
            int gVertex,
            int hVertex,
            Graph g,
            Graph h,
            Func<int, int, T> graphScore,
            Func<Graph, int, T> vertexScore,
            ref T score,
            ref int[] gBestSolution,
            ref int[] hBestSolution
            )
        {
            var prime = Primes.GetNthPrime(recursionDepth);

            // TODO: get neighbourSet from parent or recompute now
            var gExistingNeighbourSet = new SortedDictionary<T, int>();
            var hExistingNeighbourSet = new SortedDictionary<T, int>();
            Graph gExistingSubgraph = null;
            Graph hExistingSubgraph = null;
            var gShouldOmit = new bool[0];
            var hShouldOmit = new bool[0];

            // generate fresh neighbourhood
            foreach (var vertex in gExistingSubgraph.Vertices)
            {
                foreach (var neighbour in gExistingSubgraph.NeighboursOf(vertex))
                {
                    gExistingNeighbourSet.Add(vertexScore(gExistingSubgraph, vertex), vertex);
                }
            }

            // no particular order
            foreach (var gNeighbour in g.NeighboursOf(gVertex))
            {
                // append the new prime to neighbours if they're not already in subgraph
                // todo: make sure gNeighbour is not INACTIVE or keep removing...
                if (!gAlreadyInSubgraph[gNeighbour])
                {
                    gNeighbourHashes[gNeighbour] *= prime;
                }
                // count the number of hash collisions
                gExistingNeighbourSet.Add(vertexScore(gExistingSubgraph, gNeighbour), gNeighbour);
            }
            foreach (var hNeighbour in h.NeighboursOf(hVertex))
            {
                // append the new prime to neighbours if they're not already in subgraph
                // todo: make sure hNeighbour is not INACTIVE or keep removing...
                if (!hAlreadyInSubgraph[hNeighbour])
                {
                    hNeighbourHashes[hNeighbour] *= prime;
                }
                // toverify: can neighbours be pruned right here?
                // count the number of hash collisions
                hExistingNeighbourSet.Add(vertexScore(hExistingSubgraph, hNeighbour), hNeighbour);
            }



            // while gVertex 
            // toconsider: at each step choose either neighbours of g or neighbours of h whichever is more performant
            var gBestVertex = gExistingNeighbourSet.
                foreach (var item in gExistingNeighbourSet)
            {

            }

            // todo: order neighbours of gVertex according to the extremum function and matching hash (and immediately check whether they are really isomorphic), smallest number of collisions
            // -t-o-d-o-: order neighbours of gVertex according to smallest-last
            var gConsidering = -1;
            var hConsidering = new List<int>(0);
            // choose the first one that fits
            // match it with the first h that fits

            // if no more vertices can be added check for score of common subgraph (or do it all the time)

            // perform more recursive match if applicable
            // filter only those that satisfy maximum criterion

            // at the end assume the vertex doesnt exist
            // assume gConsidering doesn't exist... can this be done faster?

            // RECURSE WITHOUT A VERTEX
            GForgetAbout(gConsidering);

            // restore everything back to original state
            // retore
        }
    }
}
