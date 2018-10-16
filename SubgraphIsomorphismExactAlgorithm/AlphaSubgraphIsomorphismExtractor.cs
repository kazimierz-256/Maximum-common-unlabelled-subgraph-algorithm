using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class AlphaSubgraphIsomorphismExtractor<T> : ISubgraphIsomorphismExtractor<T>
        where T : IComparable
    {
        private Func<int, int, T> graphScoringFunction = null;
        private T bestScore = default;
        private Dictionary<int, int> ghOptimalMapping = new Dictionary<int, int>();
        private Dictionary<int, int> hgOptimalMapping = new Dictionary<int, int>();

        private UndirectedGraph g;
        private UndirectedGraph h;
        private Dictionary<int, int> ghMapping;
        private Dictionary<int, int> hgMapping;
        private HashSet<int> gEnvelope;
        private HashSet<int> hEnvelope;
        private HashSet<int> gOutsiders;
        private HashSet<int> hOutsiders;
        private int totalNumberOfEdgesInSubgraph;

        public void ExtractOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, T> graphScoringFunction,
            T initialScore,
            out T bestScore,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping
            )
        {
            var swappedGraphs = false;

            this.graphScoringFunction = graphScoringFunction;
            this.bestScore = initialScore;

            if (hArgument.Vertices.Count < gArgument.Vertices.Count)
            {
                swappedGraphs = true;
                h = gArgument;
                g = hArgument.DeepClone();
            }
            else
            {
                g = gArgument.DeepClone();
                h = hArgument;
            }

            while (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(this.bestScore) > 0)
            {
                var gMatchingVertex = g.Vertices.First();

                foreach (var hMatchingVertex in h.Vertices)
                {
                    ghMapping = new Dictionary<int, int>();
                    hgMapping = new Dictionary<int, int>();
                    gEnvelope = new HashSet<int>() { gMatchingVertex };
                    hEnvelope = new HashSet<int>() { hMatchingVertex };
                    gOutsiders = new HashSet<int>(g.Vertices);
                    hOutsiders = new HashSet<int>(h.Vertices);
                    gOutsiders.Remove(gMatchingVertex);// warning: gOutsider is not working correctly
                    hOutsiders.Remove(hMatchingVertex);
                    totalNumberOfEdgesInSubgraph = 0;
                    Recurse();
                }

                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
            }


            // return the solution
            bestScore = this.bestScore;
            if (swappedGraphs)
            {
                ghOptimalMapping = this.hgOptimalMapping;
                hgOptimalMapping = this.ghOptimalMapping;
            }
            else
            {
                ghOptimalMapping = this.ghOptimalMapping;
                hgOptimalMapping = this.hgOptimalMapping;
            }
        }

        // makes logical connections
        // currently it is based on hash collisions
        // nevertheless it might be also ok to make choice based on 'most extremum condition' although removing vertices might become a hassle
        // ignores vertices
        // does not modify subgraph structure
        private void Recurse()
        {
            if (gEnvelope.Count == 0)
            {
                // no more connections could be found
                // check for optimality

                var vertices = ghMapping.Keys.Count;
                // count the number of edges in subgraph
                var resultingValuation = graphScoringFunction(vertices, totalNumberOfEdgesInSubgraph);
                if (resultingValuation.CompareTo(bestScore) > 0)
                {
                    bestScore = resultingValuation;
                    ghOptimalMapping = new Dictionary<int, int>(ghMapping);
                    hgOptimalMapping = new Dictionary<int, int>(hgMapping);
                }
            }
            else if (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(bestScore) > 0)
            {
                var gMatchingVertex = gEnvelope.First();

                #region prepare to recurse
                gEnvelope.Remove(gMatchingVertex);
                var edgeCountInSubgraphBackup = totalNumberOfEdgesInSubgraph;
                var gVerticesToRemoveFromEnvelope = new List<int>();

                // todo: simplify to ghTransitionFunctions and a NEW set of outsiders

                foreach (var gNeighbour in gOutsiders.ToArray())
                {
                    // if the neighbour is in the subgraph
                    if (g.ExistsConnectionBetween(gMatchingVertex, gNeighbour))
                    {
                        // if it is new to the envelope
                        gEnvelope.Add(gNeighbour);
                        gVerticesToRemoveFromEnvelope.Add(gNeighbour);
                        gOutsiders.Remove(gNeighbour);
                    }
                }
                #endregion

                // a workaround since hEnvelope is modified during recursion
                foreach (var hMatchingCandidate in hEnvelope.ToArray())
                {
                    // verify mutual agreement connections of neighbours
                    var verticesTrulyIsomorphic = true;
                    var potentialNumberOfNewEdges = 0;

                    // toconsider: maybe all necessary edges should be precomputed ahead of time, or not?
                    foreach (var ghSingleMapping in ghMapping)
                    {
                        var gVertexInSubgraph = ghSingleMapping.Key;
                        var hVertexInSubgraph = ghSingleMapping.Value;
                        var gConnection = g.ExistsConnectionBetween(gMatchingVertex, gVertexInSubgraph);
                        var hConnection = h.ExistsConnectionBetween(hMatchingCandidate, hVertexInSubgraph);
                        if (gConnection != hConnection)
                        {
                            verticesTrulyIsomorphic = false;
                            break;
                        }
                        else if (gConnection)
                        {
                            potentialNumberOfNewEdges += 1;
                        }
                    }

                    if (verticesTrulyIsomorphic)
                    {
                        totalNumberOfEdgesInSubgraph += potentialNumberOfNewEdges;
                        // by definition add the transition functions (which means adding to the subgraph)
                        ghMapping.Add(gMatchingVertex, hMatchingCandidate);
                        hgMapping.Add(hMatchingCandidate, gMatchingVertex);

                        // if the matching vertex was on the envelope then remove it
                        hEnvelope.Remove(hMatchingCandidate);

                        var hVerticesToRemoveFromEnvelope = new List<int>();

                        // todo: consider only the set of outsiders
                        // toconsider: pass on a dictionary of edges from subgraph to the envelope for more performance (somewhere else...)!
                        // spread the id to all neighbours on the envelope & discover new neighbours
                        foreach (var hNeighbour in hOutsiders.ToArray())
                        {
                            if (h.ExistsConnectionBetween(hNeighbour, hMatchingCandidate))
                            {
                                hEnvelope.Add(hNeighbour);
                                hVerticesToRemoveFromEnvelope.Add(hNeighbour);
                                hOutsiders.Remove(hNeighbour);
                            }
                        }

                        Recurse();

                        #region cleanup

                        foreach (var hVertex in hVerticesToRemoveFromEnvelope)
                        {
                            hEnvelope.Remove(hVertex);
                            hOutsiders.Add(hVertex);
                        }

                        hEnvelope.Add(hMatchingCandidate);

                        ghMapping.Remove(gMatchingVertex);
                        hgMapping.Remove(hMatchingCandidate);

                        totalNumberOfEdgesInSubgraph = edgeCountInSubgraphBackup;
                        #endregion
                    }
                }
                #region finalize recursion
                foreach (var gVertex in gVerticesToRemoveFromEnvelope)
                {
                    gEnvelope.Remove(gVertex);
                    gOutsiders.Add(gVertex);
                }
                #endregion
                // now consider the problem once the best candidate vertex has been removed
                // remove vertex from graph and then restore it
                var gRestoreOperation = g.RemoveVertex(gMatchingVertex);

                Recurse();

                g.RestoreVertex(gMatchingVertex, gRestoreOperation);
                gEnvelope.Add(gMatchingVertex);
            }
        }
    }
}
