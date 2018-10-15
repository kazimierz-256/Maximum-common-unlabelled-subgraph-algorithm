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

        private UndirectedGraph g;
        private UndirectedGraph h;
        private Dictionary<int, int> ghSubgraphTransitionFunction;
        private Dictionary<int, int> hgSubgraphTransitionFunction;
        private HashSet<int> gEnvelopeWithHashes;
        private HashSet<int> hEnvelopeWithHashes;
        private int edgeCountInSubgraph;

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
            var swapped = false;

            this.graphScore = graphScore;
            bestScore = initialScore;
            
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

            while (graphScore(g.VertexCount, g.EdgeCount).CompareTo(bestScore) > 0)
            {
                var gMatchingVertex = g.Vertices.First();

                foreach (var hMatchingVertex in h.Vertices)
                {
                    ghSubgraphTransitionFunction = new Dictionary<int, int>();
                    hgSubgraphTransitionFunction = new Dictionary<int, int>();
                    gEnvelopeWithHashes = new HashSet<int>() { gMatchingVertex };
                    hEnvelopeWithHashes = new HashSet<int>() { hMatchingVertex };
                    edgeCountInSubgraph = 0;
                    Analyze();
                }

                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
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

        // makes logical connections
        // currently it is based on hash collisions
        // nevertheless it might be also ok to make choice based on 'most extremum condition' although removing vertices might become a hassle
        // ignores vertices
        // does not modify subgraph structure
        private void Analyze()
        {
            if (gEnvelopeWithHashes.Count == 0 || hEnvelopeWithHashes.Count == 0)
            {
                // no more connections could be found
                // check for optimality

                var vertices = ghSubgraphTransitionFunction.Keys.Count;
                var result = graphScore(vertices, edgeCountInSubgraph);
                if (result.CompareTo(bestScore) > 0)
                {
                    bestScore = result;
                    gToH = new Dictionary<int, int>(ghSubgraphTransitionFunction);
                    hToG = new Dictionary<int, int>(hgSubgraphTransitionFunction);
                }
            }
            else if (graphScore(g.VertexCount, g.EdgeCount).CompareTo(bestScore) > 0)
            {
                var gMatchingVertex = gEnvelopeWithHashes.First();

                #region PREPARE
                gEnvelopeWithHashes.Remove(gMatchingVertex);
                var edgeCountInSubgraphBackup = edgeCountInSubgraph;
                var gToRemove = new List<int>();
                // todo: change iteration to two levelled iterations
                foreach (var gNeighbour in g.NeighboursOf(gMatchingVertex))
                {
                    // if the neighbour is in the subgraph
                    if (ghSubgraphTransitionFunction.ContainsKey(gNeighbour))
                    {
                        edgeCountInSubgraph += 1;
                    }
                    else if (!gEnvelopeWithHashes.Contains(gNeighbour))
                    {
                        // if it is new to the envelope
                        gEnvelopeWithHashes.Add(gNeighbour);
                        gToRemove.Add(gNeighbour);
                    }
                }
                #endregion

                foreach (var hMatchingCandidate in hEnvelopeWithHashes.ToArray())
                {
                    // verify mutual agreement connections of neighbours
                    var agree = true;

                    // toconsider: maybe all necessary edges should be precomputed ahead of time, or not?
                    foreach (var ghTransition in ghSubgraphTransitionFunction)
                    {
                        var gVertexInSubgraph = ghTransition.Key;
                        var hVertexInSubgraph = ghTransition.Value;
                        if (g.ExistsConnectionBetween(gMatchingVertex, gVertexInSubgraph) != h.ExistsConnectionBetween(hMatchingCandidate, hVertexInSubgraph))
                        {
                            agree = false;
                            break;
                        }
                    }

                    if (agree)
                    {
                        // modifies subgraph structure
                        // does not modify ignore-data structure
                        // checks by the way the extremum condition

                        // by definition add the transition functions (which means adding to the subgraph)
                        ghSubgraphTransitionFunction.Add(gMatchingVertex, hMatchingCandidate);
                        hgSubgraphTransitionFunction.Add(hMatchingCandidate, gMatchingVertex);

                        // if the matching vertex was on the envelope then remove it
                        hEnvelopeWithHashes.Remove(hMatchingCandidate);

                        var hToRemove = new List<int>();

                        // toconsider: pass on a dictionary of edges from subgraph to the envelope for more performance (somewhere else...)!
                        // spread the id to all neighbours on the envelope & discover new neighbours

                        // spread the id to all neighbours on the envelope & discover new neighbours
                        foreach (var hNeighbour in h.NeighboursOf(hMatchingCandidate))
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
                        Analyze();

                        // CLEANUP
                        ghSubgraphTransitionFunction.Remove(gMatchingVertex);
                        hgSubgraphTransitionFunction.Remove(hMatchingCandidate);

                        foreach (var hVertex in hToRemove)
                            hEnvelopeWithHashes.Remove(hVertex);

                        hEnvelopeWithHashes.Add(hMatchingCandidate);
                    }
                }
                #region FINISH
                foreach (var gVertex in gToRemove)
                    gEnvelopeWithHashes.Remove(gVertex);
                edgeCountInSubgraph = edgeCountInSubgraphBackup;
                #endregion
                // now consider the problem once the best candidate vertex has been removed
                // remove vertex from graph and then restore it
                var restoreOperation = g.RemoveVertex(gMatchingVertex);

                Analyze();

                gEnvelopeWithHashes.Add(gMatchingVertex);
                g.RestoreVertex(gMatchingVertex, restoreOperation);
            }
        }
    }
}
