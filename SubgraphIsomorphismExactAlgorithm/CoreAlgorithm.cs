﻿using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class CoreAlgorithm<T>
        where T : IComparable
    {
        private Func<int, int, T> graphScoringFunction = null;
        private Dictionary<int, int> ghOptimalMapping;
        private Dictionary<int, int> hgOptimalMapping;

        private UndirectedGraph g;
        private UndirectedGraph h;
        private bool[,] gConnectionExistance;
        private bool[,] hConnectionExistance;
        private Dictionary<int, int> ghMapping;
        private Dictionary<int, int> hgMapping;
        private HashSet<int> gEnvelope;
        private HashSet<int> hEnvelope;
        private HashSet<int> gOutsiders;
        private HashSet<int> hOutsiders;
        private int totalNumberOfEdgesInSubgraph;
        private Action<T, Dictionary<int, int>, Dictionary<int, int>> newSolutionFound;

        public void RecurseInitialMatch(
            int gMatchingVertex,
            int hMatchingVertex,
            UndirectedGraph g,
            UndirectedGraph h,
            Func<int, int, T> graphScoringFunction,
            T initialScore,
            Action<T, Dictionary<int, int>, Dictionary<int, int>> newSolutionFound,
            ref T bestScore
            )
        {
            this.g = g;
            this.h = h;

            this.newSolutionFound = newSolutionFound;

            this.graphScoringFunction = graphScoringFunction;
            ghOptimalMapping = new Dictionary<int, int>();
            hgOptimalMapping = new Dictionary<int, int>();
            ghMapping = new Dictionary<int, int>();
            hgMapping = new Dictionary<int, int>();
            gEnvelope = new HashSet<int>() { gMatchingVertex };
            hEnvelope = new HashSet<int>() { hMatchingVertex };
            gOutsiders = new HashSet<int>(g.Vertices);
            hOutsiders = new HashSet<int>(h.Vertices);
            gOutsiders.Remove(gMatchingVertex);// warning: gOutsider is not working correctly
            hOutsiders.Remove(hMatchingVertex);
            totalNumberOfEdgesInSubgraph = 0;
            var gMax = g.Vertices.Max();
            gConnectionExistance = new bool[gMax + 1, gMax + 1];
            
            foreach (var kvp in g.Neighbours)
            {
                foreach (var vertexTo in kvp.Value)
                {
                    gConnectionExistance[kvp.Key, vertexTo] = true;
                }
            }
            hConnectionExistance = new bool[h.Vertices.Count, h.Vertices.Count];
            foreach (var kvp in h.Neighbours)
            {
                foreach (var vertexTo in kvp.Value)
                {
                    hConnectionExistance[kvp.Key, vertexTo] = true;
                }
            }

            Recurse(ref bestScore);

            ghOptimalMapping = ghMapping;
            hgOptimalMapping = hgMapping;
        }

        private void Recurse(ref T bestScore)
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
                    ghOptimalMapping = new Dictionary<int, int>(ghMapping);
                    hgOptimalMapping = new Dictionary<int, int>(hgMapping);
                    newSolutionFound(resultingValuation, ghOptimalMapping, hgOptimalMapping);
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
                    if (gConnectionExistance[gMatchingVertex, gNeighbour])
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
                        var gConnection = gConnectionExistance[gMatchingVertex, gVertexInSubgraph];
                        var hConnection = hConnectionExistance[hMatchingCandidate, hVertexInSubgraph];
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

                        // spread the id to all neighbours on the envelope & discover new neighbours
                        foreach (var hNeighbour in hOutsiders.ToArray())
                        {
                            if (hConnectionExistance[hNeighbour, hMatchingCandidate])
                            {
                                hEnvelope.Add(hNeighbour);
                                hVerticesToRemoveFromEnvelope.Add(hNeighbour);
                                hOutsiders.Remove(hNeighbour);
                            }
                        }

                        Recurse(ref bestScore);

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

                Recurse(ref bestScore);

                g.RestoreVertex(gMatchingVertex, gRestoreOperation);
                gEnvelope.Add(gMatchingVertex);
            }
        }
    }
}
