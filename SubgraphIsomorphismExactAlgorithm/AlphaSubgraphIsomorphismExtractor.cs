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
        private Func<UndirectedGraph, int, T> vertexScore = null;
        private T bestScore = default(T);
        private Dictionary<int, int> gToH = null;
        private Dictionary<int, int> hToG = null;

        public void Extract(
            UndirectedGraph argG,
            UndirectedGraph argH,
            Func<int, int, T> graphScore,
            Func<UndirectedGraph, int, T> vertexScore,
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
                h = argG.DeepClone();
                g = argH.DeepClone();
            }
            else
            {
                g = argG.DeepClone();
                h = argH.DeepClone();
            }

            this.vertexScore = vertexScore;
            this.graphScore = graphScore;
            bestScore = initialScore;

            // note: work even faster without this fancy order...

            while (g.VertexCount > 0)
            {
                // get largest vertex according to smallest-last order
                var gCopy = g.DeepClone();
                while (gCopy.VertexCount > 1)
                {
                    // delete smallest vertex
                    var minDegree = int.MaxValue;
                    var minVertex = -1;

                    foreach (var connection in gCopy.EnumerateConnections())
                    {
                        if (connection.Value.Count < minDegree)
                        {
                            minDegree = connection.Value.Count;
                            minVertex = connection.Key;
                        }
                    }

                    gCopy.RemoveVertex(minVertex);
                }
                var gVertex = gCopy.EnumerateConnections().First().Key;

                foreach (var hConnection in h.EnumerateConnections())
                {
                    MatchAndExpand(
                        gVertex,
                        hConnection.Key,
                        g,
                        h,
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>(),
                        new Dictionary<int, List<int>>() { { gVertex, new List<int>() } },
                        new Dictionary<int, long>() { { gVertex, 0L } },
                        new Dictionary<int, long>() { { hConnection.Key, 0L } },
                        new Dictionary<int, int>(),
                        0
                        );
                }

                // ignore previous g-vertices
                // todo: remove vertices based on extremum condition itself!
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
            Dictionary<int, long> gEnvelopeWithHashes,
            Dictionary<int, long> hEnvelopeWithHashes,
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
            var gEnvelopeToRestore = gEnvelopeWithHashes[gMatchingVertex];
            var hEnvelopeToRestore = hEnvelopeWithHashes[hMatchingVertex];
            gEnvelopeWithHashes.Remove(gMatchingVertex);
            hEnvelopeWithHashes.Remove(hMatchingVertex);

            var gToDivide = new List<Tuple<int, int>>();
            var gToRemove = new List<int>();
            var hToDivide = new List<Tuple<int, int>>();
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
                else
                {
                    // if the neighbour is outside of the subgraph and is not ignored

                    if (gEnvelopeWithHashes.ContainsKey(gNeighbour))
                    {
                        // if it is already on the envelope
                        gEnvelopeWithHashes[gNeighbour] *= prime;
                        gToDivide.Add(new Tuple<int, int>(gNeighbour, prime));
                    }
                    else
                    {
                        gToRemove.Add(gNeighbour);
                        // if it is new to the envelope
                        gEnvelopeWithHashes.Add(gNeighbour, 1L);
                        // the new neighbour needs to update their hash
                        foreach (var gVertexInSubgraph in ghSubgraphTransitionFunction.Keys)
                        {
                            if (g.ExistsConnectionBetween(gVertexInSubgraph, gNeighbour))
                            {
                                // BUG: reassign primes
                                gEnvelopeWithHashes[gNeighbour] *= gSubgraphPrimes[gVertexInSubgraph];
                            }
                        }
                    }
                }
            }

            // verify extremum condition right now!
            if (VerifyExtremumCondition(gMatchingVertex, ghSubgraphTransitionFunction, gEdgeConnections))
            {
                // spread the id to all neighbours on the envelope & discover new neighbours
                foreach (var hNeighbour in h.NeighboursOf(hMatchingVertex))
                {
                    // if the neighbour is outside the subgraph
                    if (!hgSubgraphTransitionFunction.ContainsKey(hNeighbour))
                    {
                        if (hEnvelopeWithHashes.ContainsKey(hNeighbour))
                        {
                            // if it is already on the envelope
                            hEnvelopeWithHashes[hNeighbour] *= prime;
                            hToDivide.Add(new Tuple<int, int>(hNeighbour, prime));
                        }
                        else
                        {
                            hToRemove.Add(hNeighbour);
                            // if it is new to the envelope
                            hEnvelopeWithHashes.Add(hNeighbour, 1L);
                            // the new neighbour needs to update their hash
                            foreach (var hVertexInSubgraph in hgSubgraphTransitionFunction.Keys)
                            {
                                if (h.ExistsConnectionBetween(hVertexInSubgraph, hNeighbour))
                                {
                                    hEnvelopeWithHashes[hNeighbour] *= gSubgraphPrimes[hgSubgraphTransitionFunction[hVertexInSubgraph]];
                                }
                            }
                        }
                    }
                }

                Analyze(g, h, ghSubgraphTransitionFunction, hgSubgraphTransitionFunction, gEdgeConnections, gEnvelopeWithHashes, hEnvelopeWithHashes, gSubgraphPrimes, localEdgeCount);
            }

            // restore
            ghSubgraphTransitionFunction.Remove(gMatchingVertex);
            hgSubgraphTransitionFunction.Remove(hMatchingVertex);
            gSubgraphPrimes.Remove(gMatchingVertex);

            var toCleanse = gEdgeConnections[gMatchingVertex];
            gEdgeConnections.Remove(gMatchingVertex);
            foreach (var neighbour in toCleanse)
                gEdgeConnections[neighbour].Remove(gMatchingVertex);

            foreach (var tuple in gToDivide)
                gEnvelopeWithHashes[tuple.Item1] /= tuple.Item2;
            foreach (var gVertex in gToRemove)
                gEnvelopeWithHashes.Remove(gVertex);
            gEnvelopeWithHashes.Add(gMatchingVertex, gEnvelopeToRestore);

            foreach (var tuple in hToDivide)
                hEnvelopeWithHashes[tuple.Item1] /= tuple.Item2;
            foreach (var hVertex in hToRemove)
                hEnvelopeWithHashes.Remove(hVertex);
            hEnvelopeWithHashes.Add(hMatchingVertex, hEnvelopeToRestore);
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
            Dictionary<int, long> gEnvelopeWithHashes,
            Dictionary<int, long> hEnvelopeWithHashes,
            Dictionary<int, int> gLocalSubgraphPrimes,
            int edgeCountInSubgraph
            )
        {
            // toconsider: the hash analysis is indepentent of the envelope, then the analysis should be made only once
            var gHashRepresentatives = new Dictionary<long, List<int>>();
            var hHashRepresentatives = new Dictionary<long, List<int>>();

            // place vertices into hash baskets
            foreach (var gHashMapping in gEnvelopeWithHashes)
            {
                if (gHashRepresentatives.ContainsKey(gHashMapping.Value))
                {
                    gHashRepresentatives[gHashMapping.Value].Add(gHashMapping.Key);
                }
                else
                {
                    gHashRepresentatives[gHashMapping.Value] = new List<int>()
                    {
                        gHashMapping.Key
                    };
                }
            }
            foreach (var hHashMapping in hEnvelopeWithHashes)
            {
                if (hHashRepresentatives.ContainsKey(hHashMapping.Value))
                {
                    hHashRepresentatives[hHashMapping.Value].Add(hHashMapping.Key);
                }
                else
                {
                    hHashRepresentatives[hHashMapping.Value] = new List<int>()
                    {
                        hHashMapping.Key
                    };
                }
            }

            var gBestCandidate = -1;

            // prepare to sort h hashes based on collisions
            var hHashes = new long[hHashRepresentatives.Keys.Count];
            hHashRepresentatives.Keys.CopyTo(hHashes, 0);
            var hRepetitions = hHashes.Select(hash => hHashRepresentatives[hash].Count).ToArray();
            // should be in ascending order
            Array.Sort(hRepetitions, hHashes);

            foreach (var hHashCondidate in hHashes)
            {
                // make sure the extremum condition is satisfied
                if (gHashRepresentatives.ContainsKey(hHashCondidate))
                {
                    // just get the first one from the list
                    gBestCandidate = gHashRepresentatives[hHashCondidate][0];
                }
            }

            if (gBestCandidate == -1)
            {
                // no more connections could be found
                // check for optimality

                LocalMaximumEnding(ghSubgraphTransitionFunction, hgSubgraphTransitionFunction, gEdgeConnections, edgeCountInSubgraph);
            }
            else
            {

                var hCandidates = hHashRepresentatives[gEnvelopeWithHashes[gBestCandidate]];
                foreach (var hCandidate in hCandidates)
                {
                    // verify mutual agreement connections of neighbours
                    var disagree = false;

                    // toconsider: maybe all necessary edges should be precomputed ahead of time, or not?
                    foreach (var ghTransition in ghSubgraphTransitionFunction)
                    {
                        var gVertexInSubgraph = ghTransition.Key;
                        var hVertexInSubgraph = ghTransition.Value;
                        if (g.ExistsConnectionBetween(gBestCandidate, gVertexInSubgraph) != h.ExistsConnectionBetween(hCandidate, hVertexInSubgraph))
                        {
                            // connection is wrong! despite same hash
                            disagree = true;
                            break;
                        }
                    }

                    if (!disagree)
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
                var gEnvelopeHashestoRestore = gEnvelopeWithHashes[gBestCandidate];
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

                gEnvelopeWithHashes.Add(gBestCandidate, gEnvelopeHashestoRestore);

                g.RestoreVertex(gBestCandidate, restoreOperation);
            }
        }

        private bool VerifyExtremumCondition(int gBestCandidate, Dictionary<int, int> ghSubgraphTransitionFunction, Dictionary<int, List<int>> gEdgeConnections)
        {
            return true;
            return ExtractExtremumVertices(ghSubgraphTransitionFunction, gEdgeConnections).Contains(gBestCandidate);
        }

        private HashSet<int> ExtractExtremumVertices(Dictionary<int, int> ghSubgraphTransitionFunction, Dictionary<int, List<int>> gEdgeConnections)
        {
            // assume gBestCandidate is indeed part of graph
            var assumedMinimum = int.MaxValue;
            var minimumSet = new HashSet<int>();

            // todo: use vertexScore
            foreach (var gVertex in gEdgeConnections)
            {
                if (gVertex.Value.Count < assumedMinimum)
                {
                    minimumSet = new HashSet<int>()
                    {
                        gVertex.Key
                    };
                }
                else if (gVertex.Value.Count == assumedMinimum)
                {
                    minimumSet.Add(gVertex.Key);
                }
            }

            return minimumSet;
        }

        private void LocalMaximumEnding(
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            Dictionary<int, List<int>> gEdgeConnections,
            int edgesInSubgraph
            )
        {
            var vertices = ghSubgraphTransitionFunction.Count;
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
