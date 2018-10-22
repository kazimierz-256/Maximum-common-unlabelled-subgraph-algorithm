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
        private Action<int, T, Dictionary<int, int>, Dictionary<int, int>> depthReached;
        private bool[,] gConnectionExistance;
        private bool[,] hConnectionExistance;
        private Dictionary<int, int> ghMapping;
        private Dictionary<int, int> hgMapping;
        private HashSet<int> gEnvelope;
        private HashSet<int> hEnvelope;
        private HashSet<int> gOutsiders;
        private HashSet<int> hOutsiders;
        private int totalNumberOfEdgesInSubgraph;
        private Action<T, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>> newSolutionFound;
        private bool analyzeDisconnected;
        private bool findExactMatch;
        private int recursionDepth;
        private bool exactMatchInG = true;
        private int gInitialChoice;
        private int hInitialChoice;

        public void HighLevelSetup(
            int gMatchingVertex,
            int hMatchingVertex,
            UndirectedGraph g,
            UndirectedGraph h,
            Func<int, int, T> graphScoringFunction,
            Action<T, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>> newSolutionFound,
            bool analyzeDisconnected = false,
            bool findExactMatch = false,
            int recursionDepth = int.MaxValue,
            Action<int, T, Dictionary<int, int>, Dictionary<int, int>> depthReached = null
            )
        {
            this.g = g;
            this.h = h;

            this.gInitialChoice = gMatchingVertex;
            this.hInitialChoice = hMatchingVertex;
            this.recursionDepth = recursionDepth;
            this.findExactMatch = findExactMatch;
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
        }

        public void Recurse(ref T bestScore)
        {
            recursionDepth -= 1;
            if (recursionDepth == 0)
                depthReached?.Invoke(recursionDepth, graphScoringFunction(ghMapping.Keys.Count, totalNumberOfEdgesInSubgraph), ghMapping, hgMapping);
            else if (gEnvelope.Count == 0 || hEnvelope.Count == 0)
            {

                // no more connections could be found
                // check for optimality

                var vertices = ghMapping.Keys.Count;
                // count the number of edges in subgraph
                var resultingValuation = graphScoringFunction(vertices, totalNumberOfEdgesInSubgraph);
                depthReached?.Invoke(recursionDepth, resultingValuation, ghMapping, hgMapping);
                if (resultingValuation.CompareTo(bestScore) > 0)
                {
                    newSolutionFound(resultingValuation, () => new Dictionary<int, int>(ghMapping), () => new Dictionary<int, int>(hgMapping));
                }
            }
            else if (graphScoringFunction(Math.Min(g.Vertices.Count, h.Vertices.Count), Math.Min(g.EdgeCount, h.EdgeCount)).CompareTo(bestScore) > 0)
            {
                var gMatchingVertex = -1;
                var gMatchingOptimality = int.MaxValue;
                foreach (var gVertexCondidate in gEnvelope)
                {
                    var degree = 0;
                    foreach (var gSub in ghMapping.Keys)
                    {
                        if (gConnectionExistance[gVertexCondidate, gSub])
                        {
                            degree += 1;
                        }
                    }
                    if (degree < gMatchingOptimality)
                    {
                        gMatchingOptimality = degree;
                        gMatchingVertex = gVertexCondidate;
                    }
                }

                #region prepare to recurse
                gEnvelope.Remove(gMatchingVertex);
                var edgeCountInSubgraphBackup = totalNumberOfEdgesInSubgraph;
                var gVerticesToRemoveFromEnvelope = new List<int>();

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
                        if (analyzeDisconnected)
                            DisconnectComponent(ref bestScore);

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
                if (!(findExactMatch && exactMatchInG))
                {
                    var gRestoreOperation = g.RemoveVertex(gMatchingVertex);

                    Recurse(ref bestScore);

                    g.RestoreVertex(gMatchingVertex, gRestoreOperation);
                }
                gEnvelope.Add(gMatchingVertex);
                recursionDepth += 1;
            }
        }

        private void DisconnectComponent(ref T bestScore)
        {
            // if exact match is required then recurse only when no vertex in g would be omitted
            if (gOutsiders.Count > 0 && hOutsiders.Count > 0 && (!findExactMatch || (gEnvelope.Count == 0 && exactMatchInG) || (hEnvelope.Count == 0 && !exactMatchInG)))
            {
                var currentVertices = ghMapping.Keys.Count;
                var currentEdges = totalNumberOfEdgesInSubgraph;
                var gOutsiderGraph = g.DeepCloneIntersecting(gOutsiders);
                var hOutsiderGraph = h.DeepCloneIntersecting(hOutsiders);
                var subgraphsSwapped = false;
                if (hOutsiderGraph.EdgeCount < gOutsiderGraph.EdgeCount)
                {
                    subgraphsSwapped = true;
                    var tmp = gOutsiderGraph;
                    gOutsiderGraph = hOutsiderGraph;
                    hOutsiderGraph = tmp;
                }


                while (gOutsiderGraph.Vertices.Count > 0 && graphScoringFunction(Math.Min(gOutsiderGraph.Vertices.Count, hOutsiderGraph.Vertices.Count) + currentVertices, Math.Min(gOutsiderGraph.EdgeCount, hOutsiderGraph.EdgeCount) + currentEdges).CompareTo(bestScore) > 0)
                {
                    var gMatchingVertex = -1;
                    var gMatchingScore = int.MaxValue;

                    foreach (var gCandidate in gOutsiderGraph.Vertices)
                    {
                        if (gOutsiderGraph.Degree(gCandidate) < gMatchingScore)
                        {
                            gMatchingScore = gOutsiderGraph.Degree(gCandidate);
                            gMatchingVertex = gCandidate;
                        }
                    }

                    foreach (var hMatchingCandidate in hOutsiderGraph.Vertices.ToArray())
                    {
                        var subSolver = new CoreAlgorithm<T>()
                        {
                            g = gOutsiderGraph,
                            h = hOutsiderGraph,
                            depthReached = depthReached,
                            // tocontemplate: how to value disconnected components?
                            graphScoringFunction = (int vertices, int edges) => graphScoringFunction(vertices + currentVertices, edges + currentEdges),
                            newSolutionFound = (newScore, ghMap, hgMap) =>
                            {
                                newSolutionFound(
                                    newScore,
                                    () =>
                                    {
                                        var ghExtended = subgraphsSwapped ? hgMap() : ghMap();
                                        foreach (var myMapping in ghMapping)
                                            ghExtended.Add(myMapping.Key, myMapping.Value);
                                        return ghExtended;
                                    },
                                    () =>
                                    {
                                        var hgExtended = subgraphsSwapped ? ghMap() : hgMap();
                                        foreach (var myMapping in hgMapping)
                                            hgExtended.Add(myMapping.Key, myMapping.Value);
                                        return hgExtended;
                                    }
                                    );
                            },
                            ghMapping = new Dictionary<int, int>(),
                            hgMapping = new Dictionary<int, int>(),
                            gEnvelope = new HashSet<int>() { gMatchingVertex },
                            hEnvelope = new HashSet<int>() { hMatchingCandidate },
                            gOutsiders = new HashSet<int>(gOutsiderGraph.Vertices.Where(vertex => vertex != gMatchingVertex)),
                            hOutsiders = new HashSet<int>(hOutsiderGraph.Vertices.Where(vertex => vertex != hMatchingCandidate)),
                            totalNumberOfEdgesInSubgraph = 0,
                            gConnectionExistance = subgraphsSwapped ? hConnectionExistance : gConnectionExistance,
                            hConnectionExistance = subgraphsSwapped ? gConnectionExistance : hConnectionExistance,
                            analyzeDisconnected = true,
                            recursionDepth = recursionDepth, // todo: make sure it is recursionDepth not recursionDepth-1
                            findExactMatch = findExactMatch,
                            exactMatchInG = !subgraphsSwapped,
                            gInitialChoice = gMatchingVertex,
                            hInitialChoice = hMatchingCandidate
                        };
                        subSolver.Recurse(ref bestScore);
                    }

                    if (findExactMatch && !subgraphsSwapped)
                        break;

                    gOutsiderGraph.RemoveVertex(gMatchingVertex);
                }
            }
        }
    }
}
