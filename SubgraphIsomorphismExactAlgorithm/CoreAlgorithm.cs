﻿#define induced

using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class CoreAlgorithm
    {
        public Func<int, int, double> subgraphScoringFunction;
        public Graph g;
        public Graph h;
        public bool[,] gConnectionExistence;
        public bool[,] hConnectionExistence;
        public int[] gMapping;
        public int[] hMapping;
        public int mappingCount;
        public HashSet<int> gEnvelope;
        public HashSet<int> hEnvelope;
        public HashSet<int> gOutsiders;
        public HashSet<int> hOutsiders;
        public int totalNumberOfEdgesInSubgraph;
        public Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFoundNotificationAction;
        public bool analyzeDisconnected;
        public bool findGraphGinH;
        public int leftoverSteps;
        public int deepness = 0;
        public int deepnessTakeawaySteps;
        public int originalLeftoverSteps;
        public double approximationRatio;
        private Random random = new Random(0);
        public int[] gEnvelopeHashes;
        public int[] hEnvelopeHashes;
        public HashSet<int> hVerticesAutomorphic = new HashSet<int>();

        public CoreAlgorithm InternalStateSetup(
            int gInitialMatchingVertex,
            int hInitialMatchingVertex,
            Graph g,
            Graph h,
            Func<int, int, double> subgraphScoringFunction,
            Action<double, Func<Dictionary<int, int>>, Func<Dictionary<int, int>>, int> newSolutionFoundNotificationAction,
            bool analyzeDisconnected = false,
            bool findGraphGinH = false,
            int leftoverSteps = -1,
            int deepnessTakeawaySteps = 0,
            bool[,] gConnectionExistence = null,
            bool[,] hConnectionExistence = null,
            HashSet<int> automorphismVerticesOverride = null,
            double approximationRatio = 1d)
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
            this.approximationRatio = approximationRatio;

            gMapping = new int[Math.Min(g.Vertices.Count, h.Vertices.Count)];
            hMapping = new int[gMapping.Length];
            mappingCount = 0;
            // for simplicity insert initial isomorphic vertices into the envelope
            gEnvelope = new HashSet<int>() { gInitialMatchingVertex };
            hEnvelope = new HashSet<int>() { hInitialMatchingVertex };
            if (automorphismVerticesOverride == null)
            {
                gOutsiders = new HashSet<int>(g.Vertices);
                hOutsiders = new HashSet<int>(h.Vertices);
            }
            else
            {
                gOutsiders = new HashSet<int>(automorphismVerticesOverride);
                hOutsiders = new HashSet<int>(automorphismVerticesOverride);
            }
            gOutsiders.Remove(gInitialMatchingVertex);
            hOutsiders.Remove(hInitialMatchingVertex);

            totalNumberOfEdgesInSubgraph = 0;

            // determine the edge-existence matrix
            int gMax = -2, hMax = -2;
            if (gConnectionExistence == null)
            {
                gMax = g.Vertices.Max();

                this.gConnectionExistence = new bool[gMax + 1, gMax + 1];
                foreach (var kvp in g.Neighbours)
                    foreach (var vertexTo in kvp.Value)
                        this.gConnectionExistence[kvp.Key, vertexTo] = true;
            }
            else
            {
                this.gConnectionExistence = gConnectionExistence;
                gMax = this.gConnectionExistence.GetLength(0);
            }

            if (hConnectionExistence == null)
            {
                hMax = h.Vertices.Max();


                this.hConnectionExistence = new bool[hMax + 1, hMax + 1];
                foreach (var kvp in h.Neighbours)
                    foreach (var vertexTo in kvp.Value)
                        this.hConnectionExistence[kvp.Key, vertexTo] = true;
            }
            else
            {
                this.hConnectionExistence = hConnectionExistence;
                hMax = this.hConnectionExistence.GetLength(0);
            }


            if (gConnectionExistence == null && hConnectionExistence == null)
            {
                gEnvelopeHashes = new int[gMax + 1];
                hEnvelopeHashes = new int[hMax + 1];
            }

            return this;
        }

        // returns boolean value whether two vertices are locally isomorphic
        // if they are the method modifies internal state
        public bool TryMatchFromEnvelopeMutateInternalState(int gMatchingCandidate, int hMatchingCandidate)
        {
            if (gEnvelope.Contains(gMatchingCandidate) && hEnvelope.Contains(hMatchingCandidate))
            {
                var candidatesTrulyIsomorphic = true;
                var potentialNumberOfNewEdges = 0;

                for (int i = 0; i < mappingCount; i += 1)
                {
                    var gVertexInSubgraph = gMapping[i];
                    var hVertexInSubgraph = hMapping[i];
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
                    gMapping[mappingCount] = gMatchingCandidate;
                    hMapping[mappingCount] = hMatchingCandidate;
                    mappingCount += 1;

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

        public Dictionary<int, int> gGetDictionaryOutOfMapping()
        {
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < mappingCount; i += 1)
                dictionary.Add(gMapping[i], hMapping[i]);
            return dictionary;
        }
        public Dictionary<int, int> hGetDictionaryOutOfMapping()
        {
            var dictionary = new Dictionary<int, int>();
            for (int i = 0; i < mappingCount; i += 1)
                dictionary.Add(hMapping[i], gMapping[i]);
            return dictionary;
        }

        // main recursive discovery procedure
        // the parameter allows multiple threads to read the value directly in parallel (writing is more complicated)
        public void Recurse(ref double bestScore)
        {
            if (leftoverSteps == 0 || gEnvelope.Count == 0 || hEnvelope.Count == 0)
            {
                // no more connections could be found
                // is the found subgraph optimal?

                // count the number of edges in subgraph
                var resultingValuation = subgraphScoringFunction(mappingCount, totalNumberOfEdgesInSubgraph);
                if (resultingValuation > bestScore)
                {
                    // notify about the found solution (a local maximum) and provide a lazy evaluation method that creates the necessary mapping
                    newSolutionFoundNotificationAction?.Invoke(
                        resultingValuation,
                        gGetDictionaryOutOfMapping,
                        hGetDictionaryOutOfMapping,
                        totalNumberOfEdgesInSubgraph
                        );
                }
            }
            else if (subgraphScoringFunction(g.Vertices.Count, g.EdgeCount) * approximationRatio > bestScore)
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
                var minScore = int.MaxValue;
                var degree = -1;
                var isomorphicCandidates = new int[hEnvelope.Count];
                var tmp = isomorphicH;
                var localNumberOfCandidates = 0;
                var score = 0;
                var locallyIsomorphic = true;
                var i = 0;
                foreach (var gCan in gEnvelope)
                {
                    localNumberOfCandidates = 0;
                    score = 0;
                    var gHash = gEnvelopeHashes == null ? 0 : gEnvelopeHashes[gCan];
                    foreach (var hCan in hEnvelope)
                    {
                        if (gEnvelopeHashes != null && gHash != hEnvelopeHashes[hCan])
                            continue;

                        locallyIsomorphic = true;
                        var localEdges = gHash > 0 ? 1 : 0;
                        for (i = gHash; i < mappingCount; i += 1)
                        {
#if induced
                            if (gConnectionExistence[gCan, gMapping[i]] != hConnectionExistence[hCan, hMapping[i]])
#else
                            if (gConnectionExistence[gCan, gMapping[i]] && !hConnectionExistence[hCan, hMapping[i]])
#endif
                            {
                                locallyIsomorphic = false;
                                break;
                            }
                        }

                        if (locallyIsomorphic)
                        {
                            isomorphicCandidates[localNumberOfCandidates] = hCan;
                            localNumberOfCandidates += 1;
                            score += h.VertexDegree(hCan);
                            if (score > minScore)
                                break;
                        }
                    }

                    if (score < minScore)
                    {
                        totalNumberOfCandidates = localNumberOfCandidates;
                        minScore = score;
                        degree = -1;
                        gMatchingCandidate = gCan;

                        tmp = isomorphicCandidates;
                        isomorphicCandidates = isomorphicH;
                        isomorphicH = tmp;
                        if (totalNumberOfCandidates == 0)
                            break;
                    }
                    else if (score == minScore)
                    {
                        var thisDegree = g.VertexDegree(gCan);
                        if (degree == -1)
                            degree = g.VertexDegree(gMatchingCandidate);

                        if (thisDegree < degree)
                        {
                            totalNumberOfCandidates = localNumberOfCandidates;
                            degree = thisDegree;
                            gMatchingCandidate = gCan;

                            tmp = isomorphicCandidates;
                            isomorphicCandidates = isomorphicH;
                            isomorphicH = tmp;
                        }
                    }
                }
                for (i = 0; i < mappingCount; i += 1)
                {
                    if (gConnectionExistence[gMatchingCandidate, gMapping[i]])
                        newEdges += 1;
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
                            if (gEnvelopeHashes != null)
                                gEnvelopeHashes[gOutsider] = mappingCount + 1;
                            gEnvelope.Add(gOutsider);
                            gVerticesToRemoveFromEnvelope[gVerticesToRemoveFromEnvelopeLimit] = gOutsider;
                            gVerticesToRemoveFromEnvelopeLimit += 1;
                        }
                    }

                    // for minor performance improvement removal of the outsiders that are neighbours of gMatchingCandidate looks quite different from the way it is implemented in the TryMatchFromEnvelopeMutateInternalState procedure
                    for (i = 0; i < gVerticesToRemoveFromEnvelopeLimit; i += 1)
                    {
                        gOutsiders.Remove(gVerticesToRemoveFromEnvelope[i]);
                    }
                    totalNumberOfEdgesInSubgraph += newEdges;
                    #endregion

                    var hVerticesToRemoveFromEnvelope = new int[hOutsiders.Count];
                    var hVerticesToRemoveFromEnvelopeLimit = 0;

                    // a necessary in-place copy to an array since hEnvelope is modified during recursion
                    for (int hCandidate = 0; hCandidate < totalNumberOfCandidates && subgraphScoringFunction(g.Vertices.Count, g.EdgeCount) * approximationRatio > bestScore; hCandidate += 1)
                    {
                        var hMatchingCandidate = isomorphicH[hCandidate];
                        // verify mutual agreement connections of neighbours

                        #region H setup

                        hEnvelope.Remove(hMatchingCandidate);

                        hVerticesToRemoveFromEnvelopeLimit = 0;
                        foreach (var hNeighbour in hOutsiders)
                            if (hConnectionExistence[hNeighbour, hMatchingCandidate])
                            {
                                if (hEnvelopeHashes != null)
                                    hEnvelopeHashes[hNeighbour] = mappingCount + 1;
                                hEnvelope.Add(hNeighbour);
                                hVerticesToRemoveFromEnvelope[hVerticesToRemoveFromEnvelopeLimit] = hNeighbour;
                                hVerticesToRemoveFromEnvelopeLimit += 1;
                            }

                        for (i = 0; i < hVerticesToRemoveFromEnvelopeLimit; i += 1)
                            hOutsiders.Remove(hVerticesToRemoveFromEnvelope[i]);

                        gMapping[mappingCount] = gMatchingCandidate;
                        hMapping[mappingCount] = hMatchingCandidate;
                        mappingCount += 1;

                        deepness += 1;
                        #endregion


                        Recurse(ref bestScore);
                        if (analyzeDisconnected)
                            DisconnectComponentAndRecurse(ref bestScore);


                        #region H cleanup
                        deepness -= 1;
                        for (i = 0; i < hVerticesToRemoveFromEnvelopeLimit; i += 1)
                        {
                            hEnvelope.Remove(hVerticesToRemoveFromEnvelope[i]);
                            hOutsiders.Add(hVerticesToRemoveFromEnvelope[i]);
                        }

                        hEnvelope.Add(hMatchingCandidate);

                        mappingCount -= 1;

                        #endregion

                    }
                    #region G cleanup
                    totalNumberOfEdgesInSubgraph -= newEdges;
                    for (i = 0; i < gVerticesToRemoveFromEnvelopeLimit; i += 1)
                    {
                        gEnvelope.Remove(gVerticesToRemoveFromEnvelope[i]);
                        gOutsiders.Add(gVerticesToRemoveFromEnvelope[i]);
                    }
                    #endregion
                }
                // remove the candidate from the graph and recurse
                // then restore the removed vertex along with all the neighbours
                // if an exact match is required then - obviously - do not remove any verices from the G graph
                if (!findGraphGinH && subgraphScoringFunction(g.Vertices.Count - 1, g.EdgeCount - g.VertexDegree(gMatchingCandidate)) * approximationRatio > bestScore)
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

        public void RecurseAutomorphism(ref bool found)
        {
            if (gEnvelope.Count == hEnvelope.Count && !found)
            {
                if (gEnvelope.Count == 0)
                {
                    found = true;
                }
                else
                {
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
                        var gDegree = g.VertexDegree(gCan);
                        foreach (var hCan in hEnvelope)
                        {
                            if (h.VertexDegree(hCan) != gDegree)
                                continue;
                            locallyIsomorphic = true;
                            var localEdges = 0;
                            for (int i = 0; i < mappingCount; i += 1)
                            {
                                gConnection = gConnectionExistence[gCan, gMapping[i]];
                                if (gConnection != hConnectionExistence[hCan, hMapping[i]])
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

                            if (thisDegree < degree || (thisDegree == degree && edges < newEdges))
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

                    if (totalNumberOfCandidates > 0)
                    {
                        gEnvelope.Remove(gMatchingCandidate);
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

                            gMapping[mappingCount] = gMatchingCandidate;
                            hMapping[mappingCount] = hMatchingCandidate;
                            mappingCount += 1;

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

                            #endregion

                            RecurseAutomorphism(ref found);

                            #region H cleanup
                            for (int i = 0; i < hVerticesToRemoveFromEnvelopeLimit; i += 1)
                            {
                                hEnvelope.Remove(hVerticesToRemoveFromEnvelope[i]);
                                hOutsiders.Add(hVerticesToRemoveFromEnvelope[i]);
                            }

                            hEnvelope.Add(hMatchingCandidate);

                            mappingCount -= 1;

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
                        gEnvelope.Add(gMatchingCandidate);
                    }

                }
            }
        }

        private void DisconnectComponentAndRecurse(ref double bestScore)
        {
            // if exact match is required then recurse only if the envelope set is empty
            var currentlyBuiltVertices = mappingCount;
            var currentlyBuiltEdges = totalNumberOfEdgesInSubgraph;
            if (
                gOutsiders.Count > 0
                && hOutsiders.Count > 0
                && (!findGraphGinH || gEnvelope.Count == 0)
                && (subgraphScoringFunction(hOutsiders.Count + currentlyBuiltVertices, hOutsiders.Count * (hOutsiders.Count - 1) / 2 + currentlyBuiltEdges) * approximationRatio > bestScore)
                && (subgraphScoringFunction(gOutsiders.Count + currentlyBuiltVertices, gOutsiders.Count * (gOutsiders.Count - 1) / 2 + currentlyBuiltEdges) * approximationRatio > bestScore)
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

                if (subgraphScoringFunction(hOutsiderGraph.Vertices.Count + currentlyBuiltVertices, hOutsiderGraph.EdgeCount + currentlyBuiltEdges) * approximationRatio > bestScore)
                {
                    // if there is hope to improve the score then recurse
                    var removedVertices = new HashSet<int>();
                    while (
                        gOutsiderGraph.Vertices.Count > 0
                        && subgraphScoringFunction(gOutsiderGraph.Vertices.Count + currentlyBuiltVertices, gOutsiderGraph.EdgeCount + currentlyBuiltEdges) * approximationRatio > bestScore
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
                                        for (int i = 0; i < mappingCount; i += 1)
                                            ghExtended.Add(gMapping[i], hMapping[i]);
                                        return ghExtended;
                                    },
                                    () =>
                                    {
                                        var hgExtended = subgraphsSwapped ? ghMap() : hgMap();
                                        for (int i = 0; i < mappingCount; i += 1)
                                            hgExtended.Add(hMapping[i], gMapping[i]);
                                        return hgExtended;
                                    },
                                    edges + totalNumberOfEdgesInSubgraph
                                    )
                                ,
                                gMapping = new int[Math.Min(gOutsiderGraph.Vertices.Count, hOutsiderGraph.Vertices.Count)],
                                hMapping = new int[Math.Min(gOutsiderGraph.Vertices.Count, hOutsiderGraph.Vertices.Count)],
                                mappingCount = 0,
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
                                deepness = deepness,
                                approximationRatio = approximationRatio,
                                gEnvelopeHashes = new int[gOutsiderGraph.Vertices.Max() + 1],
                                hEnvelopeHashes = new int[hOutsiderGraph.Vertices.Max() + 1]
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
