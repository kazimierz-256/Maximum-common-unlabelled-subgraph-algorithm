using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
//using GraphExtensionAlgorithms;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class AlphaSubgraphIsomorphismExtractor<T> : ISubgraphIsomorphismExtractor<T>
        where T : IComparable
    {
        private Func<int, int, T> graphScore = null;
        private T bestScore = default;
        private Dictionary<int, int> gToH = new Dictionary<int, int>();
        private Dictionary<int, int> hToG = new Dictionary<int, int>();

        public void Extract(
            UndirectedGraph argG,
            UndirectedGraph argH,
            Func<int, int, T> graphScore,
            T initialScore,
            out T score,
            out Dictionary<int, int> gToH,
            out Dictionary<int, int> hToG
            )
        {
            UndirectedGraph g = null;
            UndirectedGraph h = null;
            var swapped = false;

            //var cc = g.ConnectedComponents();

            // todo: verify performance benefit
            if (argH.VertexCount < argG.VertexCount)
            {
                swapped = true;
                h = argG;
                g = argH.DeepClone();
            }
            else
            {
                g = argG.DeepClone();
                h = argH;
            }

            this.graphScore = graphScore;
            bestScore = initialScore;

            while (graphScore(g.VertexCount, g.EdgeCount).CompareTo(bestScore) > 0)
            {
                var gVertex = g.Vertices.First();

                foreach (var hVertex in h.Vertices)
                {
                    MatchAndExpand(
                        gVertex,
                        hVertex,
                        g,
                        h,
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>(),
                        new HashSet<int>() { gVertex },
                        new HashSet<int>() { hVertex },
                        0
                        );
                }

                // ignore previous g-vertices
                g.RemoveVertex(gVertex);
            }


            // return the solution
            score = bestScore;
            if (swapped)
            {
                gToH = this.hToG;
                hToG = this.gToH;
            }
            else
            {
                gToH = this.gToH;
                hToG = this.hToG;
            }
        }

        // modifies subgraph structure
        // does not modify ignore-data structure
        // checks by the way the extremum condition
        private void MatchAndExpand(
            int gMatchingVertex,
            int hMatchingVertex,
            UndirectedGraph g,
            UndirectedGraph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            HashSet<int> gEnvelopeWithHashes,
            HashSet<int> hEnvelopeWithHashes,
            int edgeCount
            )
        {
            var localEdgeCount = edgeCount;

            // by definition add the transition functions (which means adding to the subgraph)
            ghSubgraphTransitionFunction.Add(gMatchingVertex, hMatchingVertex);
            hgSubgraphTransitionFunction.Add(hMatchingVertex, gMatchingVertex);

            // if the matching vertex was on the envelope then remove it
            gEnvelopeWithHashes.Remove(gMatchingVertex);
            hEnvelopeWithHashes.Remove(hMatchingVertex);

            var gToRemove = new List<int>();
            var hToRemove = new List<int>();

            // toconsider: pass on a dictionary of edges from subgraph to the envelope for more performance (somewhere else...)!
            // spread the id to all neighbours on the envelope & discover new neighbours
            foreach (var gNeighbour in g.NeighboursOf(gMatchingVertex))
            {
                // if the neighbour is in the subgraph
                if (ghSubgraphTransitionFunction.ContainsKey(gNeighbour))
                {
                    localEdgeCount += 1;
                }
                else if (!gEnvelopeWithHashes.Contains(gNeighbour))
                {
                    // if it is new to the envelope
                    gEnvelopeWithHashes.Add(gNeighbour);
                    gToRemove.Add(gNeighbour);
                }
            }

            // spread the id to all neighbours on the envelope & discover new neighbours
            foreach (var hNeighbour in h.NeighboursOf(hMatchingVertex))
            {
                // if the neighbour is outside the subgraph
                if (!hgSubgraphTransitionFunction.ContainsKey(hNeighbour) && !hEnvelopeWithHashes.Contains(hNeighbour))
                {
                    // if it is new to the envelope
                    hEnvelopeWithHashes.Add(hNeighbour);
                    hToRemove.Add(hNeighbour);
                }
            }

            // RECURSE DOWN
            Analyze(g, h, ghSubgraphTransitionFunction, hgSubgraphTransitionFunction, gEnvelopeWithHashes, hEnvelopeWithHashes, localEdgeCount);

            // CLEANUP
            ghSubgraphTransitionFunction.Remove(gMatchingVertex);
            hgSubgraphTransitionFunction.Remove(hMatchingVertex);

            foreach (var gVertex in gToRemove)
                gEnvelopeWithHashes.Remove(gVertex);
            foreach (var hVertex in hToRemove)
                hEnvelopeWithHashes.Remove(hVertex);

            gEnvelopeWithHashes.Add(gMatchingVertex);
            hEnvelopeWithHashes.Add(hMatchingVertex);
        }

        // makes logical connections
        // currently it is based on hash collisions
        // nevertheless it might be also ok to make choice based on 'most extremum condition' although removing vertices might become a hassle
        // ignores vertices
        // does not modify subgraph structure
        private void Analyze(
            UndirectedGraph g,
            UndirectedGraph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            HashSet<int> gEnvelopeWithHashes,
            HashSet<int> hEnvelopeWithHashes,
            int edgeCountInSubgraph
            )
        {
            if (gEnvelopeWithHashes.Count == 0 || hEnvelopeWithHashes.Count == 0)
            {
                // no more connections could be found
                // check for optimality

                LocalMaximumEnding(ghSubgraphTransitionFunction, hgSubgraphTransitionFunction, edgeCountInSubgraph);
            }
            else if (graphScore(g.VertexCount, g.EdgeCount).CompareTo(bestScore) > 0)
            {
                var gBestCandidate = gEnvelopeWithHashes.First();

                var hCandidates = hEnvelopeWithHashes.ToArray();
                foreach (var hCandidate in hCandidates)
                {
                    // verify mutual agreement connections of neighbours
                    var agree = true;

                    // toconsider: maybe all necessary edges should be precomputed ahead of time, or not?
                    foreach (var ghTransition in ghSubgraphTransitionFunction)
                    {
                        var gVertexInSubgraph = ghTransition.Key;
                        var hVertexInSubgraph = ghTransition.Value;
                        if (g.ExistsConnectionBetween(gBestCandidate, gVertexInSubgraph) != h.ExistsConnectionBetween(hCandidate, hVertexInSubgraph))
                        {
                            agree = false;
                            break;
                        }
                    }

                    if (agree)
                    {
                        // connections are isomorphic, go on with the recursion
                        MatchAndExpand(
                            gBestCandidate,
                            hCandidate,
                            g,
                            h,
                            ghSubgraphTransitionFunction,
                            hgSubgraphTransitionFunction,
                            gEnvelopeWithHashes,
                            hEnvelopeWithHashes,
                            edgeCountInSubgraph
                            );
                    }
                }

                // now consider the problem once the best candidate vertex has been removed
                // remove vertex from graph and then restore it
                var restoreOperation = g.RemoveVertex(gBestCandidate);
                gEnvelopeWithHashes.Remove(gBestCandidate);
                Analyze(
                    g,
                    h,
                    ghSubgraphTransitionFunction,
                    hgSubgraphTransitionFunction,
                    gEnvelopeWithHashes,
                    hEnvelopeWithHashes,
                    edgeCountInSubgraph
                    );
                gEnvelopeWithHashes.Add(gBestCandidate);
                g.RestoreVertex(gBestCandidate, restoreOperation);
            }
        }

        private void LocalMaximumEnding(
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            int edgesInSubgraph
            )
        {
            var vertices = ghSubgraphTransitionFunction.Keys.Count;
            var result = graphScore(vertices, edgesInSubgraph);
            var comparison = result.CompareTo(bestScore);
            if (comparison > 0)
            {
                bestScore = result;
                gToH = new Dictionary<int, int>(ghSubgraphTransitionFunction);
                hToG = new Dictionary<int, int>(hgSubgraphTransitionFunction);
            }
        }
    }
}
