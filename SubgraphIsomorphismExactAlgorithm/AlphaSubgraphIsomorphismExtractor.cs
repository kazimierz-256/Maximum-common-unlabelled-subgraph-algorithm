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

            //var gCC = g.ConnectedComponents();
            //var hCC = h.ConnectedComponents();
            //Console.WriteLine($"G cc: {gCC.Count}, H cc: {hCC.Count}");

            this.graphScore = graphScore;
            bestScore = initialScore;

            while (graphScore(g.VertexCount, g.EdgeCount).CompareTo(bestScore) > 0)
            {
#if false
                var maxDegree = int.MinValue;
                var gVertex = -1;
                foreach (var vertex in g.Vertices)
                {
                    gVertex = vertex;
                    //break;
                    if (g.Degree(vertex) > maxDegree)
                    {
                        maxDegree = g.Degree(vertex);
                        gVertex = vertex;
                    }
                }
                //gVertex = g.Vertices.First();
#else
                var gMatchingVertex = g.Vertices.First();
#endif

                var gEnvelopeWithHashes = new HashSet<int>();
                foreach (var gNeighbour in g.NeighboursOf(gMatchingVertex))
                    gEnvelopeWithHashes.Add(gNeighbour);
                foreach (var hMatchingVertex in h.Vertices)
                {

                    HMatchAndExpand(
                        gMatchingVertex,
                        hMatchingVertex,
                        g,
                        h,
                        new Dictionary<int, int>(),
                        new Dictionary<int, int>(),
                        gEnvelopeWithHashes,
                        new HashSet<int>() { hMatchingVertex },
                        0
                        );
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

        // modifies subgraph structure
        // does not modify ignore-data structure
        // checks by the way the extremum condition
        private void HMatchAndExpand(
            int gMatchingVertex,
            int hMatchingVertex,
            UndirectedGraph g,
            UndirectedGraph h,
            Dictionary<int, int> ghSubgraphTransitionFunction,
            Dictionary<int, int> hgSubgraphTransitionFunction,
            HashSet<int> gEnvelopeWithHashes,
            HashSet<int> hEnvelopeWithHashes,
            int edgeCountInSubgraph
            )
        {
            var localEdgeCount = edgeCountInSubgraph;

            // by definition add the transition functions (which means adding to the subgraph)
            ghSubgraphTransitionFunction.Add(gMatchingVertex, hMatchingVertex);
            hgSubgraphTransitionFunction.Add(hMatchingVertex, gMatchingVertex);

            // if the matching vertex was on the envelope then remove it
            hEnvelopeWithHashes.Remove(hMatchingVertex);

            var hToRemove = new List<int>();

            // toconsider: pass on a dictionary of edges from subgraph to the envelope for more performance (somewhere else...)!
            // spread the id to all neighbours on the envelope & discover new neighbours

            // spread the id to all neighbours on the envelope & discover new neighbours
            // todo: redesign UndirectedGraph Data Structure to include levels
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
                var gMatchingVertex = gEnvelopeWithHashes.First();

                var hCandidates = hEnvelopeWithHashes.ToArray();

                #region PREPARE
                gEnvelopeWithHashes.Remove(gMatchingVertex);
                var localEdgeCount = edgeCountInSubgraph;
                var gToRemove = new List<int>();
                // todo: change iteration to two levelled iterations
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
                #endregion
                foreach (var hMatchingCandidate in hCandidates)
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
                        // connections are isomorphic, go on with the recursion
                        HMatchAndExpand(
                            gMatchingVertex,
                            hMatchingCandidate,
                            g,
                            h,
                            ghSubgraphTransitionFunction,
                            hgSubgraphTransitionFunction,
                            gEnvelopeWithHashes,
                            hEnvelopeWithHashes,
                            localEdgeCount
                            );
                    }
                }
                #region FINISH
                foreach (var gVertex in gToRemove)
                    gEnvelopeWithHashes.Remove(gVertex);
                #endregion
                // now consider the problem once the best candidate vertex has been removed
                // remove vertex from graph and then restore it
                var restoreOperation = g.RemoveVertex(gMatchingVertex);
                Analyze(
                    g,
                    h,
                    ghSubgraphTransitionFunction,
                    hgSubgraphTransitionFunction,
                    gEnvelopeWithHashes,
                    hEnvelopeWithHashes,
                    edgeCountInSubgraph
                    );
                gEnvelopeWithHashes.Add(gMatchingVertex);
                g.RestoreVertex(gMatchingVertex, restoreOperation);
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
