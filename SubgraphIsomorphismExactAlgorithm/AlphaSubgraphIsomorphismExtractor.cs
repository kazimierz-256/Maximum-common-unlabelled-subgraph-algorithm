using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

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
                var gVertex = g.Connections.First().Key;

                foreach (var hConnection in h.Connections)
                {
                    MatchAndExpand(
                        gVertex,
                        hConnection.Key,
                        g,
                        h,
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>(),
                        new Dictionary<int, List<int>>() { { gVertex, new List<int>() } },
                        new HashSet<int>() { gVertex },
                        new HashSet<int>() { hConnection.Key },
                        new Dictionary<int, int>(),
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
            Dictionary<int, List<int>> gEdgeConnections,
            HashSet<int> gEnvelopeWithHashes,
            HashSet<int> hEnvelopeWithHashes,
            Dictionary<int, int> gSubgraphPrimes,
            int edgeCount
            )
        {
            // get a unique id number to send out
            var prime = Primes.GetNthPrime(gSubgraphPrimes.Count);

            // make a modifiable copy of arguments
            gSubgraphPrimes.Add(gMatchingVertex, prime);
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
                    // increase both 'degrees' of vertices
                    if (gEdgeConnections.ContainsKey(gMatchingVertex))
                    {
                        gEdgeConnections[gMatchingVertex].Add(gNeighbour);
                    }
                    else
                    {
                        gEdgeConnections.Add(gMatchingVertex, new List<int>() { gNeighbour });
                    }

                    gEdgeConnections[gNeighbour].Add(gMatchingVertex);
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
            Analyze(g, h, ghSubgraphTransitionFunction, hgSubgraphTransitionFunction, gEdgeConnections, gEnvelopeWithHashes, hEnvelopeWithHashes, gSubgraphPrimes, localEdgeCount);

            // CLEANUP
            ghSubgraphTransitionFunction.Remove(gMatchingVertex);
            hgSubgraphTransitionFunction.Remove(hMatchingVertex);
            gSubgraphPrimes.Remove(gMatchingVertex);

            var toCleanse = gEdgeConnections[gMatchingVertex];
            gEdgeConnections.Remove(gMatchingVertex);
            foreach (var neighbour in toCleanse)
                gEdgeConnections[neighbour].Remove(gMatchingVertex);

            gEnvelopeWithHashes.Add(gMatchingVertex);

            foreach (var hVertex in hToRemove)
                hEnvelopeWithHashes.Remove(hVertex);
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
            Dictionary<int, List<int>> gEdgeConnections,
            HashSet<int> gEnvelopeWithHashes,
            HashSet<int> hEnvelopeWithHashes,
            Dictionary<int, int> gLocalSubgraphPrimes,
            int edgeCountInSubgraph
            )
        {
            if (gEnvelopeWithHashes.Count == 0)
            {
                // no more connections could be found
                // check for optimality

                LocalMaximumEnding(ghSubgraphTransitionFunction, hgSubgraphTransitionFunction, gEdgeConnections, edgeCountInSubgraph);
            }
            else
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
                            // connection is wrong! despite same hash
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
                            gEdgeConnections,
                            gEnvelopeWithHashes,
                            hEnvelopeWithHashes,
                            gLocalSubgraphPrimes,
                            edgeCountInSubgraph
                            );
                    }
                }

                // now consider the problem once the best candidate vertex has been removed
                // remove vertex from graph and then restore it
                var restoreOperation = g.RemoveVertex(gBestCandidate);
                if (graphScore(g.VertexCount, g.EdgeCount).CompareTo(bestScore) > 0)
                {
                    gEnvelopeWithHashes.Remove(gBestCandidate);
                    Analyze(
                        g,
                        h,
                        ghSubgraphTransitionFunction,
                        hgSubgraphTransitionFunction,
                        gEdgeConnections,
                        gEnvelopeWithHashes,
                        hEnvelopeWithHashes,
                        gLocalSubgraphPrimes,
                        edgeCountInSubgraph
                        );
                    gEnvelopeWithHashes.Add(gBestCandidate);
                }
                g.RestoreVertex(gBestCandidate, restoreOperation);
            }
        }

        private void LocalMaximumEnding(
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            Dictionary<int, List<int>> gEdgeConnections,
            int edgesInSubgraph
            )
        {
            var vertices = gEdgeConnections.Keys.Count;
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
