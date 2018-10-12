using System;
using System.Collections.Generic;
using System.Text;
using GraphDataStructure;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class AlphaSubgraphIsomorphismExtractor<T> : ISubgraphIsomorphismExtractor<T>
        where T : IComparable
    {
        private Func<Graph, int, T> vertexScore;
        private T bestScore;
        private int[] gBestSolution = new int[0];
        private int[] hBestSolution = new int[0];

        public void Extract(
            Graph argG,
            Graph argH,
            Func<Graph, int, T> vertexScore,
            T initialScore,
            out T score,
            out int[] gBestSolution,
            out int[] hBestSolution
            )
        {
            this.vertexScore = vertexScore;
            bestScore = initialScore;

            // todo: make the initial connection

            // return
            score = bestScore;
            gBestSolution = this.gBestSolution;
            hBestSolution = this.hBestSolution;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gMatchingVertex"></param>
        /// <param name="hMatchingVertex"></param>
        /// <param name="g"></param>
        /// <param name="h"></param>
        /// <param name="ghSubgraphTransitionFunction"></param>
        /// <param name="hgSubgraphTransitionFunction"></param>
        /// <param name="gEnvelopeWithHashes"></param>
        /// <param name="hEnvelopeWithHashes"></param>
        private void MatchAndExpand(
            int gMatchingVertex,
            int hMatchingVertex,
            Graph g,
            Graph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            Dictionary<int, long> gEnvelopeWithHashes,
            Dictionary<int, long> hEnvelopeWithHashes
            )
        {
            // get a unique id number to send out
            var prime = Primes.GetNthPrime(ghSubgraphTransitionFunction.Count);

            // make a modifiable copy of arguments
            var ghLocalSubgraphTransitionFunction = new Dictionary<int, int>(ghSubgraphTransitionFunction);
            var hgLocalSubgraphTransitionFunction = new Dictionary<int, int>(hgSubgraphTransitionFunction);
            var gLocalEnvelope = new Dictionary<int, long>(gEnvelopeWithHashes);
            var hLocalEnvelope = new Dictionary<int, long>(hEnvelopeWithHashes);

            // by definition add the transition functions (which means adding to the subgraph)
            ghLocalSubgraphTransitionFunction.Add(gMatchingVertex, hMatchingVertex);
            hgLocalSubgraphTransitionFunction.Add(hMatchingVertex, gMatchingVertex);

            // if the matching vertex was on the envelope then remove it
            gLocalEnvelope.Remove(gMatchingVertex);
            hLocalEnvelope.Remove(hMatchingVertex);

            // spread the id to all neighbours on the envelope & discover new neighbours
            foreach (var gNeighbour in g.NeighboursOf(gMatchingVertex))
            {
                // if the neighbour is outside the subgraph
                if (!ghLocalSubgraphTransitionFunction.ContainsKey(gNeighbour))
                {
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
                        // toconsider: this could be a great time to compute extremum condition
                    }
                }
            }
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
                        // toconsider: this could be a great time to compute extremum condition
                    }
                }
            }

            // cannot modify only g and h
            Analyze(g, h, ghLocalSubgraphTransitionFunction, hgLocalSubgraphTransitionFunction, gLocalEnvelope, hLocalEnvelope);
        }

        private void Analyze(
            Graph g,
            Graph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            Dictionary<int, long> gEnvelopeWithHashes,
            Dictionary<int, long> hEnvelopeWithHashes
            )
        {
        }
    }
}
