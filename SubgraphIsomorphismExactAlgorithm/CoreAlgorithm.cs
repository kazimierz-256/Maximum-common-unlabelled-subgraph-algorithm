#define induced

using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public struct CoreInternalState
    {
        // A class used to manipulate/clone algorithm's internal state

        public Func<int, int, double> subgraphScoringFunction;
        public Graph g;
        public Graph h;
        public bool[,] gConnectionExistence;
        public bool[,] hConnectionExistence;
        public Dictionary<int, int> ghMapping;
        public Dictionary<int, int> hgMapping;
        public HashSet<int> gEnvelope;
        public HashSet<int> hEnvelope;
        public HashSet<int> gOutsiders;
        public HashSet<int> hOutsiders;
        public int totalNumberOfEdgesInSubgraph;
        public Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFound;
        public bool analyzeDisconnected;
        public bool findGraphGinH;
        public int leftoverSteps;
        public int deepnessTakeawaySteps;
        public int originalLeftoverSteps;

        public CoreInternalState Clone(bool gClone = false, bool hClone = false)
        => new CoreInternalState()
        {
            analyzeDisconnected = analyzeDisconnected,
            findGraphGinH = findGraphGinH,
            g = gClone ? g.DeepClone() : g,
            h = hClone ? h.DeepClone() : h,
            gConnectionExistence = gConnectionExistence.Clone() as bool[,],
            hConnectionExistence = hConnectionExistence.Clone() as bool[,],
            gEnvelope = new HashSet<int>(gEnvelope),
            hEnvelope = new HashSet<int>(hEnvelope),
            ghMapping = new Dictionary<int, int>(ghMapping),
            hgMapping = new Dictionary<int, int>(hgMapping),
            gOutsiders = new HashSet<int>(gOutsiders),
            hOutsiders = new HashSet<int>(hOutsiders),
            subgraphScoringFunction = subgraphScoringFunction,
            newSolutionFound = newSolutionFound,
            totalNumberOfEdgesInSubgraph = totalNumberOfEdgesInSubgraph,
            leftoverSteps = leftoverSteps,
            deepnessTakeawaySteps = deepnessTakeawaySteps,
            originalLeftoverSteps = originalLeftoverSteps
        };
    }
    public class CoreAlgorithm
    {
        private Func<int, int, double> subgraphScoringFunction;
        private Graph g;
        private Graph h;
        private bool[,] gConnectionExistence;
        private bool[,] hConnectionExistence;
        private Dictionary<int, int> ghMapping;
        private Dictionary<int, int> hgMapping;
        private HashSet<int> gEnvelope;
        private HashSet<int> hEnvelope;
        public int[] gExportEnvelope { get => gEnvelope.ToArray(); }
        public int[] hExportEnvelope { get => hEnvelope.ToArray(); }
        private HashSet<int> gOutsiders;
        private HashSet<int> hOutsiders;
        private int totalNumberOfEdgesInSubgraph;
        private Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFoundNotificationAction;
        private bool analyzeDisconnected;
        private bool findGraphGinH;
        private int leftoverSteps;
        private int deepness = 0;
        private int deepnessTakeawaySteps;
        private int originalLeftoverSteps;

        public CoreInternalState ExportShallowInternalState() => new CoreInternalState()
        {
            analyzeDisconnected = analyzeDisconnected,
            findGraphGinH = findGraphGinH,
            g = g,
            h = h,
            gConnectionExistence = gConnectionExistence,
            hConnectionExistence = hConnectionExistence,
            gEnvelope = gEnvelope,
            hEnvelope = hEnvelope,
            ghMapping = ghMapping,
            hgMapping = hgMapping,
            gOutsiders = gOutsiders,
            hOutsiders = hOutsiders,
            subgraphScoringFunction = subgraphScoringFunction,
            newSolutionFound = newSolutionFoundNotificationAction,
            totalNumberOfEdgesInSubgraph = totalNumberOfEdgesInSubgraph,
            leftoverSteps = leftoverSteps,
            deepnessTakeawaySteps = deepnessTakeawaySteps,
            originalLeftoverSteps = originalLeftoverSteps,
        };

        public void ImportShallowInternalState(CoreInternalState state)
        {
            analyzeDisconnected = state.analyzeDisconnected;
            findGraphGinH = state.findGraphGinH;
            g = state.g;
            h = state.h;
            gConnectionExistence = state.gConnectionExistence;
            hConnectionExistence = state.hConnectionExistence;
            gEnvelope = state.gEnvelope;
            hEnvelope = state.hEnvelope;
            ghMapping = state.ghMapping;
            hgMapping = state.hgMapping;
            gOutsiders = state.gOutsiders;
            hOutsiders = state.hOutsiders;
            subgraphScoringFunction = state.subgraphScoringFunction;
            newSolutionFoundNotificationAction = state.newSolutionFound;
            totalNumberOfEdgesInSubgraph = state.totalNumberOfEdgesInSubgraph;
            leftoverSteps = state.leftoverSteps;
            deepnessTakeawaySteps = state.deepnessTakeawaySteps;
            originalLeftoverSteps = state.originalLeftoverSteps;
        }


        public void InternalStateSetup(
            int gInitialMatchingVertex,
            int hInitialMatchingVertex,
            Graph g,
            Graph h,
            Func<int, int, double> subgraphScoringFunction,
            Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFoundNotificationAction,
            bool analyzeDisconnected = false,
            bool findGraphGinH = false,
            int leftoverSteps = -1,
            int deepnessTakeawaySteps = 0
            )
        {
            this.g = g;
            this.h = h;
            this.subgraphScoringFunction = subgraphScoringFunction;
            this.newSolutionFoundNotificationAction = newSolutionFoundNotificationAction;
            this.analyzeDisconnected = analyzeDisconnected;
            this.findGraphGinH = findGraphGinH;
            this.leftoverSteps = leftoverSteps;
            originalLeftoverSteps = leftoverSteps;
            this.deepnessTakeawaySteps = deepnessTakeawaySteps;

            ghMapping = new Dictionary<int, int>();
            hgMapping = new Dictionary<int, int>();
            // for simplicity insert initial isomorphic vertices into the envelope
            gEnvelope = new HashSet<int>() { gInitialMatchingVertex };
            hEnvelope = new HashSet<int>() { hInitialMatchingVertex };
            gOutsiders = new HashSet<int>(g.Vertices);
            hOutsiders = new HashSet<int>(h.Vertices);
            gOutsiders.Remove(gInitialMatchingVertex);
            hOutsiders.Remove(hInitialMatchingVertex);
            totalNumberOfEdgesInSubgraph = 0;

            // determine the edge-existence matrix
            var gMax = g.Vertices.Max();
            gConnectionExistence = new bool[gMax + 1, gMax + 1];
            foreach (var kvp in g.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    gConnectionExistence[kvp.Key, vertexTo] = true;

            var hMax = h.Vertices.Max();
            hConnectionExistence = new bool[hMax + 1, hMax + 1];
            foreach (var kvp in h.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    hConnectionExistence[kvp.Key, vertexTo] = true;
        }

        // returns boolean value whether two vertices are locally isomorphic
        // if they are the method modifies internal state
        public bool TryMatchFromEnvelopeMutateInternalState(int gMatchingCandidate, int hMatchingCandidate)
        {
            if (gEnvelope.Contains(gMatchingCandidate) && hEnvelope.Contains(hMatchingCandidate))
            {
                var candidatesTrulyIsomorphic = true;
                var potentialNumberOfNewEdges = 0;

                foreach (var ghSingleMapping in ghMapping)
                {
                    var gVertexInSubgraph = ghSingleMapping.Key;
                    var hVertexInSubgraph = ghSingleMapping.Value;
                    var gConnection = gConnectionExistence[gMatchingCandidate, gVertexInSubgraph];
                    var hConnection = hConnectionExistence[hMatchingCandidate, hVertexInSubgraph];
#if induced
                    if (gConnection != hConnection)
#else
                    if (gConnection && gConnection != hConnection)
#endif
                    {
                        candidatesTrulyIsomorphic = false;
                        break;
                    }
                    else if (gConnection)
                    {
                        potentialNumberOfNewEdges += 1;
                    }
                }

                if (candidatesTrulyIsomorphic)
                {
                    totalNumberOfEdgesInSubgraph += potentialNumberOfNewEdges;
                    // by definition add the transition functions (which means adding them to the subgraph)
                    ghMapping.Add(gMatchingCandidate, hMatchingCandidate);
                    hgMapping.Add(hMatchingCandidate, gMatchingCandidate);

                    // if the matching vertex was in the envelope set then remove it
                    gEnvelope.Remove(gMatchingCandidate);
                    hEnvelope.Remove(hMatchingCandidate);

                    // spread the id to all neighbours in the envelope set and discover new neighbours
                    foreach (var gOutsider in gOutsiders.ToArray())
                    {
                        // if the vertex ia a neighbour of the matching vertex
                        if (gConnectionExistence[gMatchingCandidate, gOutsider])
                        {
                            // the outsider vertex is new to the envelope
                            gEnvelope.Add(gOutsider);
                            gOutsiders.Remove(gOutsider);
                        }
                    }
                    // similarly do the same with H graph
                    foreach (var hNeighbour in hOutsiders.ToArray())
                    {
                        if (hConnectionExistence[hNeighbour, hMatchingCandidate])
                        {
                            hEnvelope.Add(hNeighbour);
                            hOutsiders.Remove(hNeighbour);
                        }
                    }

                    // successful matching
                    return true;
                }
                else
                {
                    // candidates are not locally isomorphic
                    return false;
                }
            }
            // at least one of the candidates is not in the envelope set
            return false;
        }

        // main recursive discovery procedure
        // the parameter allows multiple threads to read the value directly in parallel (writing is more complicated)
        public void Recurse(ref double bestScore)
        {
            if (leftoverSteps == 0 || gEnvelope.Count == 0 || hEnvelope.Count == 0)
            {
                // no more connections could be found
                // is the found subgraph optimal?

                var vertices = ghMapping.Keys.Count;
                // count the number of edges in subgraph
                var resultingValuation = subgraphScoringFunction(vertices, totalNumberOfEdgesInSubgraph);
                if (resultingValuation > bestScore)
                {
                    // notify about the found solution (a local maximum) and provide a lazy evaluation method that creates the necessary mapping
                    newSolutionFoundNotificationAction?.Invoke(
                        resultingValuation,
                        () => new Dictionary<int, int>(ghMapping),
                        () => new Dictionary<int, int>(hgMapping),
                        totalNumberOfEdgesInSubgraph
                        );
                }
            }
            else if (subgraphScoringFunction(g.Vertices.Count, g.EdgeCount) > bestScore)
            {
                // if there is hope for a larger score then recurse further

                // the following is for the approximation algorithm part
                // reset leftoverSteps if necessary
                if (deepness <= deepnessTakeawaySteps)
                    leftoverSteps = originalLeftoverSteps;
                else if (leftoverSteps > 0)
                    leftoverSteps -= 1;

                #region Choosing the next candidate
                var gMatchingCandidate = -1;
                var totalNumberOfCandidates = int.MaxValue;
                var isomorphicH = new int[hEnvelope.Count];
                var newEdges = 0;
                var minScore2 = int.MaxValue;
                var degree = -1;
                var isomorphicCandidates = new int[hEnvelope.Count];
                var edges = 0;
                var gConnection = false;
                var tmp = isomorphicH;
                var localNumberOfCandidates = 0;
                var score2 = 0;
                var locallyIsomorphic = true;

                foreach (var gCan in gEnvelope)
                {
                    localNumberOfCandidates = 0;
                    score2 = 0;
                    edges = 0;
                    foreach (var hCan in hEnvelope)
                    {
                        locallyIsomorphic = true;
                        var localEdges = 0;
                        foreach (var gMap in ghMapping)
                        {
                            gConnection = gConnectionExistence[gCan, gMap.Key];
#if induced
                            if (gConnection != hConnectionExistence[hCan, gMap.Value])
#else
                            if (gConnection && !hConnectionExistence[hCan, gMap.Value])
#endif
                            {
                                locallyIsomorphic = false;
                                break;
                            }
                            if (gConnection)
                                localEdges += 1;
                        }

                        if (locallyIsomorphic)
                        {
                            edges = localEdges;
                            isomorphicCandidates[localNumberOfCandidates] = hCan;
                            localNumberOfCandidates += 1;
                            score2 += h.VertexDegree(hCan);
                            if (score2 > minScore2)
                                break;
                        }
                    }

                    if (score2 < minScore2)
                    {
                        totalNumberOfCandidates = localNumberOfCandidates;
                        minScore2 = score2;
                        degree = -1;
                        gMatchingCandidate = gCan;
                        newEdges = edges;

                        tmp = isomorphicCandidates;
                        isomorphicCandidates = isomorphicH;
                        isomorphicH = tmp;
                        if (totalNumberOfCandidates == 0)
                            break;
                    }
                    else if (score2 == minScore2)
                    {
                        var thisDegree = g.VertexDegree(gCan);
                        if (degree == -1)
                            degree = g.VertexDegree(gMatchingCandidate);

                        if (thisDegree < degree || (thisDegree == degree && ghMapping.Count(map => gConnectionExistence[map.Key, gCan]) < ghMapping.Count(map => gConnectionExistence[map.Key, gMatchingCandidate])))
                        {
                            totalNumberOfCandidates = localNumberOfCandidates;
                            degree = thisDegree;
                            gMatchingCandidate = gCan;
                            newEdges = edges;

                            tmp = isomorphicCandidates;
                            isomorphicCandidates = isomorphicH;
                            isomorphicH = tmp;
                        }
                    }
                }
                #endregion

                gEnvelope.Remove(gMatchingCandidate);
                if (totalNumberOfCandidates > 0)
                {
                    #region G setup
                    var gVerticesToRemoveFromEnvelope = new int[gOutsiders.Count];
                    var gVerticesToRemoveFromEnvelopeLimit = 0;

                    foreach (var gOutsider in gOutsiders)
                    {
                        // if the vertex ia a neighbour of the matching vertex
                        if (gConnectionExistence[gMatchingCandidate, gOutsider])
                        {
                            // the outsider vertex is new to the envelope
                            gEnvelope.Add(gOutsider);
                            gVerticesToRemoveFromEnvelope[gVerticesToRemoveFromEnvelopeLimit] = gOutsider;
                            gVerticesToRemoveFromEnvelopeLimit += 1;
                        }
                    }

                    // for minor performance improvement removal of the outsiders that are neighbours of gMatchingCandidate looks quite different from the way it is implemented in the TryMatchFromEnvelopeMutateInternalState procedure
                    for (int i = 0; i < gVerticesToRemoveFromEnvelopeLimit; i += 1)
                    {
                        gOutsiders.Remove(gVerticesToRemoveFromEnvelope[i]);
                    }
                    #endregion

                    var hVerticesToRemoveFromEnvelope = new int[hOutsiders.Count];
                    var hVerticesToRemoveFromEnvelopeLimit = 0;


                    // a necessary in-place copy to an array since hEnvelope is modified during recursion
                    for (int hCandidate = 0; hCandidate < totalNumberOfCandidates; hCandidate += 1)
                    {
                        var hMatchingCandidate = isomorphicH[hCandidate];
                        // verify mutual agreement connections of neighbours

                        #region H setup
                        totalNumberOfEdgesInSubgraph += newEdges;

                        ghMapping.Add(gMatchingCandidate, hMatchingCandidate);
                        hgMapping.Add(hMatchingCandidate, gMatchingCandidate);

                        hEnvelope.Remove(hMatchingCandidate);

                        hVerticesToRemoveFromEnvelopeLimit = 0;
                        foreach (var hNeighbour in hOutsiders)
                            if (hConnectionExistence[hNeighbour, hMatchingCandidate])
                            {
                                hEnvelope.Add(hNeighbour);
                                hVerticesToRemoveFromEnvelope[hVerticesToRemoveFromEnvelopeLimit] = hNeighbour;
                                hVerticesToRemoveFromEnvelopeLimit += 1;
                            }

                        for (int i = 0; i < hVerticesToRemoveFromEnvelopeLimit; i += 1)
                            hOutsiders.Remove(hVerticesToRemoveFromEnvelope[i]);

                        deepness += 1;
                        #endregion


                        Recurse(ref bestScore);
                        if (analyzeDisconnected)
                            DisconnectComponentAndRecurse(ref bestScore);


                        #region H cleanup
                        deepness -= 1;
                        for (int i = 0; i < hVerticesToRemoveFromEnvelopeLimit; i += 1)
                        {
                            hEnvelope.Remove(hVerticesToRemoveFromEnvelope[i]);
                            hOutsiders.Add(hVerticesToRemoveFromEnvelope[i]);
                        }

                        hEnvelope.Add(hMatchingCandidate);

                        ghMapping.Remove(gMatchingCandidate);
                        hgMapping.Remove(hMatchingCandidate);

                        totalNumberOfEdgesInSubgraph -= newEdges;
                        #endregion

                    }
                    #region G cleanup
                    for (int i = 0; i < gVerticesToRemoveFromEnvelopeLimit; i += 1)
                    {
                        gEnvelope.Remove(gVerticesToRemoveFromEnvelope[i]);
                        gOutsiders.Add(gVerticesToRemoveFromEnvelope[i]);
                    }
                    #endregion
                }
                // remove the candidate from the graph and recurse
                // then restore the removed vertex along with all the neighbours
                // if an exact match is required then - obviously - do not remove any verices from the G graph
                if (!findGraphGinH && subgraphScoringFunction(g.Vertices.Count - 1, g.EdgeCount - g.VertexDegree(gMatchingCandidate)) > bestScore)
                {
                    var gRestoreOperation = g.RemoveVertex(gMatchingCandidate);
                    deepness += 1;

                    Recurse(ref bestScore);

                    deepness -= 1;
                    g.AddVertex(gMatchingCandidate, gRestoreOperation);
                }
                gEnvelope.Add(gMatchingCandidate);
                // the procedure has left the recursion step having the internal state unchanged
            }
        }

        private void DisconnectComponentAndRecurse(ref double bestScore)
        {
            // if exact match is required then recurse only if the envelope set is empty
            var currentlyBuiltVertices = ghMapping.Keys.Count;
            var currentlyBuiltEdges = totalNumberOfEdgesInSubgraph;
            if (
                gOutsiders.Count > 0
                && hOutsiders.Count > 0
                && (!findGraphGinH || gEnvelope.Count == 0)
                && (subgraphScoringFunction(hOutsiders.Count + currentlyBuiltVertices, hOutsiders.Count * (hOutsiders.Count - 1) / 2 + currentlyBuiltEdges) > bestScore)
                && (subgraphScoringFunction(gOutsiders.Count + currentlyBuiltVertices, gOutsiders.Count * (gOutsiders.Count - 1) / 2 + currentlyBuiltEdges) > bestScore)
                )
            {
                var gOutsiderGraph = g.DeepCloneHavingVerticesIntersectedWith(gOutsiders);
                var hOutsiderGraph = h.DeepCloneHavingVerticesIntersectedWith(hOutsiders);
                var subgraphsSwapped = false;
                if (!findGraphGinH && hOutsiderGraph.EdgeCount < gOutsiderGraph.EdgeCount)
                {
                    subgraphsSwapped = true;
                    var tmp = gOutsiderGraph;
                    gOutsiderGraph = hOutsiderGraph;
                    hOutsiderGraph = tmp;
                }

                if (subgraphScoringFunction(hOutsiderGraph.Vertices.Count + currentlyBuiltVertices, hOutsiderGraph.EdgeCount + currentlyBuiltEdges) > bestScore)
                {
                    // if there is hope to improve the score then recurse
                    var removedVertices = new HashSet<int>();
                    while (
                        gOutsiderGraph.Vertices.Count > 0
                        && subgraphScoringFunction(gOutsiderGraph.Vertices.Count + currentlyBuiltVertices, gOutsiderGraph.EdgeCount + currentlyBuiltEdges) > bestScore
                        )
                    {
                        // choose the candidate with largest degree within the graph of outsiders
                        // if there is an ambiguity then choose the vertex with the largest degree in the original graph
                        var gMatchingCandidate = gOutsiderGraph.Vertices.ArgMax(
                            v => gOutsiderGraph.VertexDegree(v),
                            v => removedVertices.Count(r => gConnectionExistence[r, v])
                            );

                        foreach (var hMatchingCandidate in hOutsiderGraph.Vertices)
                        {
                            var subSolver = new CoreAlgorithm()
                            {
                                g = gOutsiderGraph,
                                h = hOutsiderGraph,
                                // there might exist an ambiguity in evaluating the scoring function for disconnected components
                                // the simplest valuation has been chosen - the sum of all vertices of all disconnected components
                                subgraphScoringFunction = (int vertices, int edges) => subgraphScoringFunction(vertices + currentlyBuiltVertices, edges + currentlyBuiltEdges),
                                newSolutionFoundNotificationAction = (newScore, ghMap, hgMap, edges) => newSolutionFoundNotificationAction?.Invoke(
                                    newScore,
                                    () =>
                                    {
                                        var ghExtended = subgraphsSwapped ? hgMap() : ghMap();
                                        foreach (var gCurrentMap in ghMapping)
                                            ghExtended.Add(gCurrentMap.Key, gCurrentMap.Value);
                                        return ghExtended;
                                    },
                                    () =>
                                    {
                                        var hgExtended = subgraphsSwapped ? ghMap() : hgMap();
                                        foreach (var hCurrentMap in hgMapping)
                                            hgExtended.Add(hCurrentMap.Key, hCurrentMap.Value);
                                        return hgExtended;
                                    },
                                    edges + totalNumberOfEdgesInSubgraph
                                    )
                                ,
                                ghMapping = new Dictionary<int, int>(),
                                hgMapping = new Dictionary<int, int>(),
                                gEnvelope = new HashSet<int>() { gMatchingCandidate },
                                hEnvelope = new HashSet<int>() { hMatchingCandidate },
                                gOutsiders = new HashSet<int>(gOutsiderGraph.Vertices),
                                hOutsiders = new HashSet<int>(hOutsiderGraph.Vertices),
                                totalNumberOfEdgesInSubgraph = 0,
                                gConnectionExistence = subgraphsSwapped ? hConnectionExistence : gConnectionExistence,
                                hConnectionExistence = subgraphsSwapped ? gConnectionExistence : hConnectionExistence,
                                analyzeDisconnected = true,
                                findGraphGinH = findGraphGinH,
                                leftoverSteps = leftoverSteps,
                                deepness = deepness
                            };
                            subSolver.gOutsiders.Remove(gMatchingCandidate);
                            subSolver.hOutsiders.Remove(hMatchingCandidate);
                            subSolver.Recurse(ref bestScore);
                        }

                        if (findGraphGinH)
                            break;

                        gOutsiderGraph.RemoveVertex(gMatchingCandidate);
                        removedVertices.Add(gMatchingCandidate);
                    }
                }
            }
        }
    }
}
