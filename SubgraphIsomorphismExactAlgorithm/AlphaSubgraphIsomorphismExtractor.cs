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
        private Func<Graph, int, T> vertexScore = null;
        private T bestScore = default(T);
        private Dictionary<int, int> gToH = null;
        private Dictionary<int, int> hToG = null;

        public void Extract(
            Graph argG,
            Graph argH,
            Func<int, int, T> graphScore,
            Func<Graph, int, T> vertexScore,
            T initialScore,
            out T score,
            out Dictionary<int, int> gToH,
            out Dictionary<int, int> hToG
            )
        {
            this.vertexScore = vertexScore;
            this.graphScore = graphScore;
            bestScore = initialScore;

            var ignoredHashSet = new HashSet<int>();

            // todo: generate the proper order (each time smallest-last)
            var vertices = Enumerable.Range(0, argG.VertexCount).ToArray();
            // note: the mapping is onto negative degrees
            var degrees = Enumerable.Range(0, argG.VertexCount).Select(v => -1 * argG.Degree(v)).ToArray();
            Array.Sort(degrees, vertices);

            foreach (var gVertex in vertices)
            {
                for (int j = 0; j < argH.VertexCount; j++)
                {
                    MatchAndExpand(
                        gVertex,
                        j,
                        argG,
                        argH,
                        new Dictionary<int, int>()
                        {
                            { gVertex, j }
                        },
                        new Dictionary<int, int>()
                        {
                            { j, gVertex }
                        },
                        new Dictionary<int, List<int>>(),
                        new Dictionary<int, long>(),
                        new Dictionary<int, long>(),
                        0,
                        ignoredHashSet
                        );
                }

                // ignore previous g-vertices
                // todo: remove vertices based on extremum condition itself!
                ignoredHashSet.Add(gVertex);
            }


            // return the solution
            score = bestScore;
            gToH = this.gToH;
            hToG = this.hToG;
        }

        // modifies subgraph structure
        // does not modify ignore-data structure
        // checks by the way the extremum condition
        private void MatchAndExpand(
            int gMatchingVertex,
            int hMatchingVertex,
            Graph g,
            Graph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            Dictionary<int, List<int>> gEdgeConnections,
            Dictionary<int, long> gEnvelopeWithHashes,
            Dictionary<int, long> hEnvelopeWithHashes,
            int edgeCount,
            HashSet<int> gIgnoredVertices
            )
        {
            // get a unique id number to send out
            var prime = Primes.GetNthPrime(ghSubgraphTransitionFunction.Count);

            // make a modifiable copy of arguments
            var ghLocalSubgraphTransitionFunction = new Dictionary<int, int>(ghSubgraphTransitionFunction);
            var hgLocalSubgraphTransitionFunction = new Dictionary<int, int>(hgSubgraphTransitionFunction);
            var gLocalEnvelope = new Dictionary<int, long>(gEnvelopeWithHashes);
            var hLocalEnvelope = new Dictionary<int, long>(hEnvelopeWithHashes);
            var gLocalEdgeConnections = new Dictionary<int, List<int>>(gEdgeConnections);
            var localEdgeCount = edgeCount;

            // by definition add the transition functions (which means adding to the subgraph)
            ghLocalSubgraphTransitionFunction.Add(gMatchingVertex, hMatchingVertex);
            hgLocalSubgraphTransitionFunction.Add(hMatchingVertex, gMatchingVertex);

            // if the matching vertex was on the envelope then remove it
            gLocalEnvelope.Remove(gMatchingVertex);
            hLocalEnvelope.Remove(hMatchingVertex);

            // toconsider: pass on a dictionary of edges from subgraph to the envelope for more performance (somewhere else...)!
            // spread the id to all neighbours on the envelope & discover new neighbours
            foreach (var gNeighbour in g.NeighboursOf(gMatchingVertex))
            {
                // if the neighbour is in the subgraph
                if (ghLocalSubgraphTransitionFunction.ContainsKey(gNeighbour))
                {
                    localEdgeCount += 1;
                    // increase both 'degrees' of vertices

                    gLocalEdgeConnections[gMatchingVertex].Add(gNeighbour);
                    gLocalEdgeConnections[gNeighbour].Add(gMatchingVertex);
                }
                else if (!gIgnoredVertices.Contains(gNeighbour))
                {
                    // if the neighbour is outside of the subgraph and is not ignored

                    if (gLocalEnvelope.ContainsKey(gNeighbour))
                    {
                        // if it is already on the envelope
                        gLocalEnvelope[gNeighbour] *= prime;
                    }
                    else
                    {
                        // if it is new to the envelope
                        gLocalEnvelope.Add(gNeighbour, 1L);
                        // the new neighbour needs to update their hash
                        foreach (var gVertexInSubgraph in ghLocalSubgraphTransitionFunction.Keys)
                        {
                            if (g.ExistsConnectionBetween(gVertexInSubgraph, gNeighbour))
                            {
                                gLocalEnvelope[gNeighbour] *= gLocalEnvelope[gVertexInSubgraph];
                            }
                        }
                    }
                }
            }

            // verify extremum condition right now!
            if (!VerifyExtremumCondition(gMatchingVertex, ghLocalSubgraphTransitionFunction, gLocalEdgeConnections))
                return;

            // spread the id to all neighbours on the envelope & discover new neighbours
            foreach (var hNeighbour in h.NeighboursOf(hMatchingVertex))
            {
                // if the neighbour is outside the subgraph
                if (!hgLocalSubgraphTransitionFunction.ContainsKey(hNeighbour))
                {
                    if (hLocalEnvelope.ContainsKey(hNeighbour))
                    {
                        // if it is already on the envelope
                        hLocalEnvelope[hNeighbour] *= prime;
                    }
                    else
                    {
                        // if it is new to the envelope
                        hLocalEnvelope.Add(hNeighbour, 1L);
                        // the new neighbour needs to update their hash
                        foreach (var hVertexInSubgraph in hgLocalSubgraphTransitionFunction.Keys)
                        {
                            if (h.ExistsConnectionBetween(hVertexInSubgraph, hNeighbour))
                            {
                                hLocalEnvelope[hNeighbour] *= hLocalEnvelope[hVertexInSubgraph];
                            }
                        }
                    }
                }
            }

            Analyze(g, h, ghLocalSubgraphTransitionFunction, hgLocalSubgraphTransitionFunction, gLocalEdgeConnections, gLocalEnvelope, hLocalEnvelope, localEdgeCount, gIgnoredVertices);
        }

        // makes logical connections
        // currently it is based on hash collisions
        // nevertheless it might be also ok to make choice based on 'most extremum condition' although removing vertices might become a hassle
        // ignores vertices
        // does not modify subgraph structure
        private void Analyze(
            Graph g,
            Graph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            Dictionary<int, List<int>> gEdgeConnections,
            Dictionary<int, long> gEnvelopeWithHashes,
            Dictionary<int, long> hEnvelopeWithHashes,
            int edgeCountInSubgraph,
            HashSet<int> gIgnoredVertices
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
                    gHashRepresentatives[gHashMapping.Value] = new List<int>()
                    {
                        gHashMapping.Key
                    };
                }
                else
                {
                    gHashRepresentatives[gHashMapping.Value].Add(gHashMapping.Key);
                }
            }
            foreach (var hHashMapping in hEnvelopeWithHashes)
            {
                if (hHashRepresentatives.ContainsKey(hHashMapping.Value))
                {
                    hHashRepresentatives[hHashMapping.Value] = new List<int>()
                    {
                        hHashMapping.Key
                    };
                }
                else
                {
                    hHashRepresentatives[hHashMapping.Value].Add(hHashMapping.Key);
                }
            }
            // prepare to sort h hashes based on collisions
            var hHashes = new long[0];
            hHashRepresentatives.Keys.CopyTo(hHashes, 0);
            var hRepetitions = hHashes.Select(hash => hHashRepresentatives[hash].Count).ToArray();
            // should be in ascending order
            Array.Sort(hRepetitions, hHashes);
            var gBestCandidate = -1;
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
                        // checking for extremum condition is done here, since the further in recursion the more information we have at our disposal
                        if (VerifyExtremumCondition(gBestCandidate, ghSubgraphTransitionFunction, gEdgeConnections))
                        {
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
                                edgeCountInSubgraph,
                                gIgnoredVertices
                                );
                        }
                    }
                }


                // now consider the problem once the best candidate vertex has been removed
                // toconsider: instead of copying just remove the vertex (make sure to do this on a local copy of graph g)
                var gLocalEnvelopeWithHashes = new Dictionary<int, long>(gEnvelopeWithHashes);
                gLocalEnvelopeWithHashes.Remove(gBestCandidate);

                var gLocallyIgnoredVertices = new HashSet<int>(gIgnoredVertices);
                gLocallyIgnoredVertices.Add(gBestCandidate);

                Analyze(
                    g,
                    h,
                    ghSubgraphTransitionFunction,
                    hgSubgraphTransitionFunction,
                    gEdgeConnections,
                    gLocalEnvelopeWithHashes,
                    hEnvelopeWithHashes,
                    edgeCountInSubgraph,
                    gLocallyIgnoredVertices
                    );
            }
        }

        private bool VerifyExtremumCondition(int gBestCandidate, Dictionary<int, int> ghSubgraphTransitionFunction, Dictionary<int, List<int>> gEdgeConnections)
        {
            //// assume gBestCandidate is indeed part of graph
            //var assumedMinimum = gEdgeConnections[gBestCandidate].Count;

            //// todo: use vertexScore
            //foreach (var gVertex in gEdgeConnections)
            //{
            //    if (gVertex.Key != gBestCandidate && gVertex.Value.Count < assumedMinimum)
            //    {
            //        return false;
            //    }
            //}

            //return true;
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
