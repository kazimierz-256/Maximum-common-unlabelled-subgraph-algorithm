using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public struct CoreInternalState
    {
        public Func<int, int, double> graphScoringFunction;
        public UndirectedGraph g;
        public UndirectedGraph h;
        public bool[,] gConnectionExistance;
        public bool[,] hConnectionExistance;
        public Dictionary<int, int> ghMapping;
        public Dictionary<int, int> hgMapping;
        public HashSet<int> gEnvelope;
        public HashSet<int> hEnvelope;
        public HashSet<int> gOutsiders;
        public HashSet<int> hOutsiders;
        public int totalNumberOfEdgesInSubgraph;
        public Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFound;
        public bool analyzeDisconnected;
        public bool findExactMatch;
        public int gInitialChoice;
        public int hInitialChoice;
        public bool checkForEquality;
        public bool checkStartingFromBest;

        public CoreInternalState Clone(bool gClone = false, bool hClone = false)
        => new CoreInternalState()
        {
            analyzeDisconnected = analyzeDisconnected,
            findExactMatch = findExactMatch,
            g = gClone ? g.DeepClone() : g,
            h = hClone ? h.DeepClone() : h,
            gConnectionExistance = gConnectionExistance.Clone() as bool[,],
            hConnectionExistance = hConnectionExistance.Clone() as bool[,],
            gEnvelope = new HashSet<int>(gEnvelope),
            hEnvelope = new HashSet<int>(hEnvelope),
            ghMapping = new Dictionary<int, int>(ghMapping),
            hgMapping = new Dictionary<int, int>(hgMapping),
            gInitialChoice = gInitialChoice,
            hInitialChoice = hInitialChoice,
            gOutsiders = new HashSet<int>(gOutsiders),
            hOutsiders = new HashSet<int>(hOutsiders),
            graphScoringFunction = graphScoringFunction,
            newSolutionFound = newSolutionFound,
            totalNumberOfEdgesInSubgraph = totalNumberOfEdgesInSubgraph,
            checkForEquality = checkForEquality,
            checkStartingFromBest = checkStartingFromBest
        };
    }
    public class CoreAlgorithm
    {
        private Func<int, int, double> graphScoringFunction = null;
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
        private Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFound;
        private bool analyzeDisconnected;
        private bool findExactMatch;
        private int gInitialChoice;
        private int hInitialChoice;
        private bool checkForEquality;
        private bool checkStartingFromBest;

        public CoreInternalState ExportShallowInternalState() => new CoreInternalState()
        {
            analyzeDisconnected = analyzeDisconnected,
            findExactMatch = findExactMatch,
            g = g,
            h = h,
            gConnectionExistance = gConnectionExistance,
            hConnectionExistance = hConnectionExistance,
            gEnvelope = gEnvelope,
            hEnvelope = hEnvelope,
            ghMapping = ghMapping,
            hgMapping = hgMapping,
            gInitialChoice = gInitialChoice,
            hInitialChoice = hInitialChoice,
            gOutsiders = gOutsiders,
            hOutsiders = hOutsiders,
            graphScoringFunction = graphScoringFunction,
            newSolutionFound = newSolutionFound,
            totalNumberOfEdgesInSubgraph = totalNumberOfEdgesInSubgraph,
            checkForEquality = checkForEquality,
            checkStartingFromBest = checkStartingFromBest
        };

        public void ImportShallowInternalState(CoreInternalState state)
        {
            analyzeDisconnected = state.analyzeDisconnected;
            findExactMatch = state.findExactMatch;
            g = state.g;
            h = state.h;
            gConnectionExistance = state.gConnectionExistance;
            hConnectionExistance = state.hConnectionExistance;
            gEnvelope = state.gEnvelope;
            hEnvelope = state.hEnvelope;
            ghMapping = state.ghMapping;
            hgMapping = state.hgMapping;
            gInitialChoice = state.gInitialChoice;
            hInitialChoice = state.hInitialChoice;
            gOutsiders = state.gOutsiders;
            hOutsiders = state.hOutsiders;
            graphScoringFunction = state.graphScoringFunction;
            newSolutionFound = state.newSolutionFound;
            totalNumberOfEdgesInSubgraph = state.totalNumberOfEdgesInSubgraph;
            checkForEquality = state.checkForEquality;
            checkStartingFromBest = state.checkStartingFromBest;
        }


        public void HighLevelSetup(
            int gMatchingVertex,
            int hMatchingVertex,
            UndirectedGraph g,
            UndirectedGraph h,
            Func<int, int, double> graphScoringFunction,
            Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFound,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            this.g = g;
            this.h = h;

            this.gInitialChoice = gMatchingVertex;
            this.hInitialChoice = hMatchingVertex;
            this.findExactMatch = findExactMatch;
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
            var hMax = h.Vertices.Max();
            hConnectionExistance = new bool[hMax + 1, hMax + 1];
            foreach (var kvp in h.Neighbours)
            {
                foreach (var vertexTo in kvp.Value)
                {
                    hConnectionExistance[kvp.Key, vertexTo] = true;
                }
            }
        }

        // returns boolean value based on isomorphicity of candidates
        public bool TryMatchFromEnvelopeMutateInternalState(int gMatchingVertex, int hMatchingCandidate)
        {
            if (gEnvelope.Contains(gMatchingVertex) && hEnvelope.Contains(hMatchingCandidate))
            {

                var verticesTrulyIsomorphic = true;
                var potentialNumberOfNewEdges = 0;

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
                    gEnvelope.Remove(gMatchingVertex);

                    foreach (var gNeighbour in gOutsiders.ToArray())
                    {
                        // if the neighbour is in the subgraph
                        if (gConnectionExistance[gMatchingVertex, gNeighbour])
                        {
                            // if it is new to the envelope
                            gEnvelope.Add(gNeighbour);
                            gOutsiders.Remove(gNeighbour);
                        }
                    }
                    totalNumberOfEdgesInSubgraph += potentialNumberOfNewEdges;
                    // by definition add the transition functions (which means adding to the subgraph)
                    ghMapping.Add(gMatchingVertex, hMatchingCandidate);
                    hgMapping.Add(hMatchingCandidate, gMatchingVertex);

                    // if the matching vertex was on the envelope then remove it
                    hEnvelope.Remove(hMatchingCandidate);

                    // spread the id to all neighbours on the envelope & discover new neighbours
                    foreach (var hNeighbour in hOutsiders.ToArray())
                    {
                        if (hConnectionExistance[hNeighbour, hMatchingCandidate])
                        {
                            hEnvelope.Add(hNeighbour);
                            hOutsiders.Remove(hNeighbour);
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        public void Recurse(ref double bestScore)
        {
            if (gEnvelope.Count == 0 || hEnvelope.Count == 0)
            {

                // no more connections could be found
                // check for optimality

                var vertices = ghMapping.Keys.Count;
                // count the number of edges in subgraph
                var resultingValuation = graphScoringFunction(vertices, totalNumberOfEdgesInSubgraph);
                if (resultingValuation.CompareTo(bestScore) > (checkForEquality ? -1 : 0))
                {
                    newSolutionFound?.Invoke(
                        resultingValuation,
                        () => new Dictionary<int, int>(ghMapping),
                        () => new Dictionary<int, int>(hgMapping),
                        totalNumberOfEdgesInSubgraph
                        );
                }
            }
            else if (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(bestScore) > (checkForEquality ? -1 : 0))
            {
                var gMatchingVertex = gEnvelope.First();

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
                if (!findExactMatch)
                {
                    var gRestoreOperation = g.RemoveVertex(gMatchingVertex);

                    Recurse(ref bestScore);

                    g.RestoreVertex(gMatchingVertex, gRestoreOperation);
                }
                gEnvelope.Add(gMatchingVertex);
            }
        }

        private void DisconnectComponent(ref double bestScore)
        {
            // if exact match is required then recurse only when no vertex in g would be omitted
            if (gOutsiders.Count > 0 && hOutsiders.Count > 0 && (!findExactMatch || gEnvelope.Count == 0))
            {
                var currentVertices = ghMapping.Keys.Count;
                var currentEdges = totalNumberOfEdgesInSubgraph;
                var gOutsiderGraph = g.DeepCloneIntersecting(gOutsiders);
                var hOutsiderGraph = h.DeepCloneIntersecting(hOutsiders);
                var subgraphsSwapped = false;
                if (!findExactMatch && hOutsiderGraph.EdgeCount < gOutsiderGraph.EdgeCount)
                {
                    subgraphsSwapped = true;
                    var tmp = gOutsiderGraph;
                    gOutsiderGraph = hOutsiderGraph;
                    hOutsiderGraph = tmp;
                }


                while (gOutsiderGraph.Vertices.Count > 0 && graphScoringFunction(Math.Min(gOutsiderGraph.Vertices.Count, hOutsiderGraph.Vertices.Count) + currentVertices, Math.Min(gOutsiderGraph.EdgeCount, hOutsiderGraph.EdgeCount) + currentEdges).CompareTo(bestScore) > (checkForEquality ? -1 : 0))
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
                        var subSolver = new CoreAlgorithm()
                        {
                            g = gOutsiderGraph,
                            h = hOutsiderGraph,
                            // tocontemplate: how to value disconnected components?
                            graphScoringFunction = (int vertices, int edges) => graphScoringFunction(vertices + currentVertices, edges + currentEdges),
                            newSolutionFound = (newScore, ghMap, hgMap, edges) => newSolutionFound?.Invoke(
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
                                },
                                edges + totalNumberOfEdgesInSubgraph
                                )
                            ,
                            ghMapping = new Dictionary<int, int>(),
                            hgMapping = new Dictionary<int, int>(),
                            gEnvelope = new HashSet<int>() { gMatchingVertex },
                            hEnvelope = new HashSet<int>() { hMatchingCandidate },
                            gOutsiders = new HashSet<int>(gOutsiderGraph.Vertices),
                            hOutsiders = new HashSet<int>(hOutsiderGraph.Vertices),
                            totalNumberOfEdgesInSubgraph = 0,
                            gConnectionExistance = subgraphsSwapped ? hConnectionExistance : gConnectionExistance,
                            hConnectionExistance = subgraphsSwapped ? gConnectionExistance : hConnectionExistance,
                            analyzeDisconnected = true,
                            findExactMatch = findExactMatch,
                            gInitialChoice = gMatchingVertex,
                            hInitialChoice = hMatchingCandidate,
                            checkForEquality = checkForEquality,
                            checkStartingFromBest = checkStartingFromBest
                        };
                        subSolver.gOutsiders.Remove(gMatchingVertex);
                        subSolver.hOutsiders.Remove(hMatchingCandidate);
                        subSolver.Recurse(ref bestScore);
                    }

                    if (findExactMatch)
                        break;

                    gOutsiderGraph.RemoveVertex(gMatchingVertex);
                }
            }
        }
    }
}
