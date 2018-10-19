using GraphDataStructure;
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

        private UndirectedGraph g;
        private UndirectedGraph h;
        private Action<T, Dictionary<int, int>, Dictionary<int, int>> depthReached;
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
        private bool analyzeDisconnected;

        public void RecurseInitialMatch(
            int gMatchingVertex,
            int hMatchingVertex,
            UndirectedGraph g,
            UndirectedGraph h,
            Func<int, int, T> graphScoringFunction,
            T initialScore,
            Action<T, Dictionary<int, int>, Dictionary<int, int>> newSolutionFound,
            ref T bestScore,
            bool analyzeDisconnected,
            int recursionDepth = int.MaxValue,
            Action<T, Dictionary<int, int>, Dictionary<int, int>> depthReached = null
            )
        {
            this.g = g;
            this.h = h;

            this.depthReached = depthReached;
            this.newSolutionFound = newSolutionFound;
            this.analyzeDisconnected = analyzeDisconnected;

            this.graphScoringFunction = graphScoringFunction;
            ghMapping = new Dictionary<int, int>();
            hgMapping = new Dictionary<int, int>();
            gEnvelope = new HashSet<int>() { gMatchingVertex };
            hEnvelope = new HashSet<int>() { hMatchingVertex };
            gOutsiders = new HashSet<int>(g.Vertices);
            hOutsiders = new HashSet<int>(h.Vertices);
            gOutsiders.Remove(gMatchingVertex);
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

            Recurse(ref bestScore, recursionDepth);
        }

        private void Recurse(ref T bestScore, int recursionDepth)
        {
            if (recursionDepth == 0)
                depthReached?.Invoke(graphScoringFunction(ghMapping.Keys.Count, totalNumberOfEdgesInSubgraph), ghMapping, hgMapping);
            else if (gEnvelope.Count == 0 || hEnvelope.Count == 0)
            {
                // no more connections could be found
                // check for optimality

                var vertices = ghMapping.Keys.Count;
                // count the number of edges in subgraph
                var resultingValuation = graphScoringFunction(vertices, totalNumberOfEdgesInSubgraph);
                if (resultingValuation.CompareTo(bestScore) > 0)
                {
                    newSolutionFound(resultingValuation, ghMapping, hgMapping);
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

                        Recurse(ref bestScore, recursionDepth - 1);
                        if (analyzeDisconnected)
                            DisconnectComponent(ref bestScore, recursionDepth - 1);

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

                Recurse(ref bestScore, recursionDepth - 1);

                g.RestoreVertex(gMatchingVertex, gRestoreOperation);
                gEnvelope.Add(gMatchingVertex);
            }
        }

        private void DisconnectComponent(ref T bestScore, int recursionDepth)
        {
            if (gOutsiders.Count > 0 && hOutsiders.Count > 0)
            {
                var currentVertices = ghMapping.Keys.Count;
                var currentEdges = totalNumberOfEdgesInSubgraph;
                var gOutSiderGraph = g.DeepCloneIntersecting(gOutsiders);
                var hOutSiderGraph = h.DeepCloneIntersecting(hOutsiders);

                var subSolver = new CoreAlgorithm<T>()
                {
                    g = gOutSiderGraph,
                    h = hOutSiderGraph,
                    depthReached = depthReached,
                    // todo: how to value disconnected components?
                    graphScoringFunction = (int vertices, int edges) => graphScoringFunction(vertices + currentVertices, edges + currentEdges),
                    newSolutionFound = (newScore, ghMap, hgMap) =>
                    {
                        var ghExtended = new Dictionary<int, int>(ghMap);
                        var hgExtended = new Dictionary<int, int>(hgMap);

                        foreach (var myMapping in ghMapping)
                            ghExtended.Add(myMapping.Key, myMapping.Value);
                        foreach (var myMapping in hgMapping)
                            hgExtended.Add(myMapping.Key, myMapping.Value);

                        newSolutionFound(newScore, ghExtended, hgExtended);
                        Console.WriteLine($"A sub solution found, how cute");
                    },
                    ghMapping = new Dictionary<int, int>(),
                    hgMapping = new Dictionary<int, int>(),
                    gEnvelope = new HashSet<int>() { gOutsiders.First() },
                    hEnvelope = new HashSet<int>() { hOutsiders.First() },
                    gOutsiders = new HashSet<int>(gOutsiders.Skip(1)),
                    hOutsiders = new HashSet<int>(hOutsiders.Skip(1)),
                    totalNumberOfEdgesInSubgraph = 0,
                    gConnectionExistance = gConnectionExistance,
                    hConnectionExistance = hConnectionExistance,
                    analyzeDisconnected = true
                };
                subSolver.Recurse(ref bestScore, recursionDepth);
            }
        }
    }
}
