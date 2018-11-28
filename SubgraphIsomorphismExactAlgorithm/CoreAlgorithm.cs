#define induced

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
        public int[] gEnvelope;
        public int gEnvelopeLimit;
        public int[] hEnvelope;
        public int hEnvelopeLimit;
        public int[] gOutsiders;
        public int gOutsidersLimit;
        public int[] hOutsiders;
        public int hOutsidersLimit;
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

        private int[][] isomorphicH;
        private int[][] isomorphicHIndices;
        private int[][] isomorphicCandidates;
        private int[][] isomorphicCandidatesIndices;

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
            gEnvelope = new int[g.Vertices.Count];
            gEnvelope[0] = gInitialMatchingVertex;
            gEnvelopeLimit = 1;

            hEnvelope = new int[h.Vertices.Count];
            hEnvelope[0] = hInitialMatchingVertex;
            hEnvelopeLimit = 1;

            gOutsiders = new int[g.Vertices.Count - 1];
            gOutsidersLimit = 0;
            foreach (var vertex in g.Vertices)
            {
                if (vertex != gInitialMatchingVertex)
                {
                    gOutsiders[gOutsidersLimit] = vertex;
                    gOutsidersLimit += 1;
                }
            }

            hOutsiders = new int[h.Vertices.Count - 1];
            hOutsidersLimit = 0;
            foreach (var vertex in h.Vertices)
            {
                if (vertex != hInitialMatchingVertex)
                {
                    hOutsiders[hOutsidersLimit] = vertex;
                    hOutsidersLimit += 1;
                }
            }

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

            isomorphicH = new int[h.Vertices.Count + 1][];
            isomorphicHIndices = new int[isomorphicH.GetLength(0)][];
            isomorphicCandidates = new int[isomorphicH.GetLength(0)][];
            isomorphicCandidatesIndices = new int[isomorphicH.GetLength(0)][];
            for (int i = 0; i < isomorphicH.GetLength(0); i++)
            {
                isomorphicH[i] = new int[h.Vertices.Count + 1];
                isomorphicHIndices[i] = new int[isomorphicH[i].GetLength(0)];
                isomorphicCandidates[i] = new int[isomorphicH[i].GetLength(0)];
                isomorphicCandidatesIndices[i] = new int[isomorphicH[i].GetLength(0)];
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
        public bool TryMatchFromEnvelopeMutateInternalState(int gMatchingCandidateIndex, int hMatchingCandidateIndex)
        {
            if (gMatchingCandidateIndex < gEnvelopeLimit && hMatchingCandidateIndex < hEnvelopeLimit)
            {
                var gMatchingCandidate = gEnvelope[gMatchingCandidateIndex];
                var hMatchingCandidate = hEnvelope[hMatchingCandidateIndex];
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
                    gEnvelope[gMatchingCandidateIndex] = gEnvelope[gEnvelopeLimit - 1];
                    gEnvelopeLimit -= 1;
                    hEnvelope[hMatchingCandidateIndex] = hEnvelope[hEnvelopeLimit - 1];
                    hEnvelopeLimit -= 1;

                    // spread the id to all neighbours in the envelope set and discover new neighbours
                    for (int go = 0; go < gOutsidersLimit;)
                    {
                        var gOutsider = gOutsiders[go];
                        // if the vertex ia a neighbour of the matching vertex
                        if (gConnectionExistence[gMatchingCandidate, gOutsider])
                        {
                            // the outsider vertex is new to the envelope
                            gEnvelope[gEnvelopeLimit] = gOutsider;
                            gEnvelopeLimit += 1;
                            gOutsiders[go] = gOutsiders[gOutsidersLimit - 1];
                            gOutsidersLimit -= 1;
                        }
                        else
                        {
                            go += 1;
                        }
                    }
                    // similarly do the same with H graph
                    for (int ho = 0; ho < hOutsidersLimit;)
                    {
                        var hOutsider = hOutsiders[ho];
                        // if the vertex ia a neighbour of the matching vertex
                        if (hConnectionExistence[hMatchingCandidate, hOutsider])
                        {
                            // the outsider vertex is new to the envelope
                            hEnvelope[hEnvelopeLimit] = hOutsider;
                            hEnvelopeLimit += 1;
                            hOutsiders[ho] = hOutsiders[hOutsidersLimit - 1];
                            hOutsidersLimit -= 1;
                        }
                        else
                        {
                            ho += 1;
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
            if (leftoverSteps == 0 || gEnvelopeLimit == 0 || hEnvelopeLimit == 0)
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
                var gMatchingCandidateIndex = -1;
                var totalNumberOfCandidates = int.MaxValue;
                var newEdges = 0;
                var minScore = int.MaxValue;
                var degree = -1;
                var tmp = isomorphicH[mappingCount];
                var localNumberOfCandidates = 0;
                var score = 0;
                var locallyIsomorphic = true;
                var i = 0;
                for (int ge = 0; ge < gEnvelopeLimit; ge += 1)
                {
                    var gCan = gEnvelope[ge];
                    localNumberOfCandidates = 0;
                    score = 0;
                    var gHash = gEnvelopeHashes == null ? 0 : gEnvelopeHashes[gCan];
                    for (int he = 0; he < hEnvelopeLimit; he++)
                    {
                        var hCan = hEnvelope[he];
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
                            isomorphicCandidates[mappingCount][localNumberOfCandidates] = hCan;
                            isomorphicCandidatesIndices[mappingCount][localNumberOfCandidates] = he;
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
                        gMatchingCandidateIndex = ge;

                        tmp = isomorphicCandidates[mappingCount];
                        isomorphicCandidates[mappingCount] = isomorphicH[mappingCount];
                        isomorphicH[mappingCount] = tmp;
                        tmp = isomorphicCandidatesIndices[mappingCount];
                        isomorphicCandidatesIndices[mappingCount] = isomorphicHIndices[mappingCount];
                        isomorphicHIndices[mappingCount] = tmp;
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
                            gMatchingCandidateIndex = ge;

                            tmp = isomorphicCandidates[mappingCount];
                            isomorphicCandidates[mappingCount] = isomorphicH[mappingCount];
                            isomorphicH[mappingCount] = tmp;
                            tmp = isomorphicCandidatesIndices[mappingCount];
                            isomorphicCandidatesIndices[mappingCount] = isomorphicHIndices[mappingCount];
                            isomorphicHIndices[mappingCount] = tmp;
                        }
                    }
                }
                for (i = 0; i < mappingCount; i += 1)
                {
                    if (gConnectionExistence[gMatchingCandidate, gMapping[i]])
                        newEdges += 1;
                }
                #endregion

                gEnvelope[gMatchingCandidateIndex] = gEnvelope[gEnvelopeLimit - 1];
                gEnvelopeLimit -= 1;
                if (totalNumberOfCandidates > 0)
                {
                    #region G setup

                    var gEnvelopeOriginalSize = gEnvelopeLimit;
                    var gOutsidersOriginalLimit = gOutsidersLimit;
                    for (int go = 0; go < gOutsidersLimit;)
                    {
                        var gOutsider = gOutsiders[go];
                        // if the vertex ia a neighbour of the matching vertex
                        if (gConnectionExistence[gMatchingCandidate, gOutsider])
                        {
                            // the outsider vertex is new to the envelope
                            if (gEnvelopeHashes != null)
                                gEnvelopeHashes[gOutsider] = mappingCount + 1;

                            gEnvelope[gEnvelopeLimit] = gOutsider;
                            gEnvelopeLimit += 1;
                            gOutsiders[go] = gOutsiders[gOutsidersLimit - 1];
                            gOutsiders[gOutsidersLimit - 1] = gOutsider;
                            gOutsidersLimit -= 1;
                        }
                        else
                        {
                            go += 1;
                        }
                    }

                    totalNumberOfEdgesInSubgraph += newEdges;
                    #endregion
                    var hOutsidersOriginalLimit = hOutsidersLimit;
                    // a necessary in-place copy to an array since hEnvelope is modified during recursion
                    for (int hCandidate = 0; hCandidate < totalNumberOfCandidates && subgraphScoringFunction(g.Vertices.Count, g.EdgeCount) * approximationRatio > bestScore; hCandidate += 1)
                    {
                        var hMatchingCandidate = isomorphicH[mappingCount][hCandidate];
                        // verify mutual agreement connections of neighbours

                        #region H setup

                        hEnvelope[isomorphicHIndices[mappingCount][hCandidate]] = hEnvelope[hEnvelopeLimit - 1];
                        hEnvelopeLimit -= 1;

                        var hOriginalSize = hEnvelopeLimit;
                        for (int ho = 0; ho < hOutsidersLimit;)
                        {
                            var hNeighbour = hOutsiders[ho];
                            if (hConnectionExistence[hNeighbour, hMatchingCandidate])
                            {
                                if (hEnvelopeHashes != null)
                                    hEnvelopeHashes[hNeighbour] = mappingCount + 1;
                                hEnvelope[hEnvelopeLimit] = hNeighbour;
                                hEnvelopeLimit += 1;
                                hOutsiders[ho] = hOutsiders[hOutsidersLimit - 1];
                                hOutsiders[hOutsidersLimit - 1] = hNeighbour;
                                hOutsidersLimit -= 1;
                            }
                            else
                            {
                                ho += 1;
                            }
                        }

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

                        mappingCount -= 1;
                        hOutsidersLimit = hOutsidersOriginalLimit;
                        // restore hEnvelope to original state
                        hEnvelopeLimit = hOriginalSize;
                        hEnvelope[hEnvelopeLimit] = hEnvelope[isomorphicHIndices[mappingCount][hCandidate]];
                        hEnvelope[isomorphicHIndices[mappingCount][hCandidate]] = hMatchingCandidate;
                        hEnvelopeLimit += 1;



                        #endregion

                    }
                    #region G cleanup
                    totalNumberOfEdgesInSubgraph -= newEdges;
                    // restore gEnvelope to original state
                    gEnvelopeLimit = gEnvelopeOriginalSize;
                    gOutsidersLimit = gOutsidersOriginalLimit;
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
                gEnvelope[gEnvelopeLimit] = gEnvelope[gMatchingCandidateIndex];
                gEnvelope[gMatchingCandidateIndex] = gMatchingCandidate;
                gEnvelopeLimit += 1;
                // the procedure has left the recursion step having the internal state unchanged
            }
        }

        public void RecurseAutomorphism(ref bool found)
        {
            if (gEnvelopeLimit == hEnvelopeLimit && !found)
            {
                if (gEnvelopeLimit == 0)
                {
                    found = true;
                }
                else
                {
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
                        var gMatchingCandidateIndex = -1;
                        var totalNumberOfCandidates = int.MaxValue;
                        var isomorphicH = new int[hEnvelopeLimit];
                        var isomorphicHIndices = new int[hEnvelopeLimit];
                        var newEdges = 0;
                        var minScore = int.MaxValue;
                        var degree = -1;
                        var isomorphicCandidates = new int[hEnvelopeLimit];
                        var isomorphicCandidatesIndices = new int[hEnvelopeLimit];
                        var tmp = isomorphicH;
                        var localNumberOfCandidates = 0;
                        var score = 0;
                        var locallyIsomorphic = true;
                        var i = 0;
                        for (int ge = 0; ge < gEnvelopeLimit; ge += 1)
                        {
                            var gCan = gEnvelope[ge];
                            localNumberOfCandidates = 0;
                            score = 0;
                            var gHash = gEnvelopeHashes == null ? 0 : gEnvelopeHashes[gCan];
                            var gDegree = g.VertexDegree(gCan);
                            for (int he = 0; he < hEnvelopeLimit; he++)
                            {
                                var hCan = hEnvelope[he];
                                if (h.VertexDegree(hCan) != gDegree)
                                    continue;
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
                                    isomorphicCandidatesIndices[localNumberOfCandidates] = he;
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
                                gMatchingCandidateIndex = ge;

                                tmp = isomorphicCandidates;
                                isomorphicCandidates = isomorphicH;
                                isomorphicH = tmp;
                                tmp = isomorphicCandidatesIndices;
                                isomorphicCandidatesIndices = isomorphicHIndices;
                                isomorphicHIndices = tmp;
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
                                    gMatchingCandidateIndex = ge;

                                    tmp = isomorphicCandidates;
                                    isomorphicCandidates = isomorphicH;
                                    isomorphicH = tmp;
                                    tmp = isomorphicCandidatesIndices;
                                    isomorphicCandidatesIndices = isomorphicHIndices;
                                    isomorphicHIndices = tmp;
                                }
                            }
                        }
                        for (i = 0; i < mappingCount; i += 1)
                        {
                            if (gConnectionExistence[gMatchingCandidate, gMapping[i]])
                                newEdges += 1;
                        }
                        #endregion

                        gEnvelope[gMatchingCandidateIndex] = gEnvelope[gEnvelopeLimit - 1];
                        gEnvelopeLimit -= 1;
                        if (totalNumberOfCandidates > 0)
                        {
                            #region G setup
                            var gVerticesToRemoveFromEnvelope = new int[gOutsidersLimit];
                            var gVerticesToRemoveFromEnvelopeLimit = 0;

                            var gEnvelopeOriginalSize = gEnvelopeLimit;
                            for (int go = 0; go < gOutsidersLimit;)
                            {
                                var gOutsider = gOutsiders[go];
                                // if the vertex ia a neighbour of the matching vertex
                                if (gConnectionExistence[gMatchingCandidate, gOutsider])
                                {
                                    // the outsider vertex is new to the envelope
                                    if (gEnvelopeHashes != null)
                                        gEnvelopeHashes[gOutsider] = mappingCount + 1;

                                    gEnvelope[gEnvelopeLimit] = gOutsider;
                                    gEnvelopeLimit += 1;
                                    gVerticesToRemoveFromEnvelope[gVerticesToRemoveFromEnvelopeLimit] = gOutsider;
                                    gVerticesToRemoveFromEnvelopeLimit += 1;
                                    gOutsiders[go] = gOutsiders[gOutsidersLimit - 1];
                                    gOutsidersLimit -= 1;
                                }
                                else
                                {
                                    go += 1;
                                }
                            }

                            totalNumberOfEdgesInSubgraph += newEdges;
                            #endregion

                            var hVerticesToRemoveFromEnvelope = new int[hOutsidersLimit];
                            var hVerticesToRemoveFromEnvelopeLimit = 0;

                            // a necessary in-place copy to an array since hEnvelope is modified during recursion
                            for (int hCandidate = 0; hCandidate < totalNumberOfCandidates && !found; hCandidate += 1)
                            {
                                var hMatchingCandidate = isomorphicH[hCandidate];
                                // verify mutual agreement connections of neighbours

                                #region H setup

                                hEnvelope[isomorphicHIndices[hCandidate]] = hEnvelope[hEnvelopeLimit - 1];
                                hEnvelopeLimit -= 1;

                                var hOriginalSize = hEnvelopeLimit;
                                hVerticesToRemoveFromEnvelopeLimit = 0;
                                for (int ho = 0; ho < hOutsidersLimit;)
                                {
                                    var hNeighbour = hOutsiders[ho];
                                    if (hConnectionExistence[hNeighbour, hMatchingCandidate])
                                    {
                                        if (hEnvelopeHashes != null)
                                            hEnvelopeHashes[hNeighbour] = mappingCount + 1;
                                        hEnvelope[hEnvelopeLimit] = hNeighbour;
                                        hEnvelopeLimit += 1;
                                        hVerticesToRemoveFromEnvelope[hVerticesToRemoveFromEnvelopeLimit] = hNeighbour;
                                        hVerticesToRemoveFromEnvelopeLimit += 1;
                                        hOutsiders[ho] = hOutsiders[hOutsidersLimit - 1];
                                        hOutsidersLimit -= 1;
                                    }
                                    else
                                    {
                                        ho += 1;
                                    }
                                }

                                gMapping[mappingCount] = gMatchingCandidate;
                                hMapping[mappingCount] = hMatchingCandidate;
                                mappingCount += 1;

                                deepness += 1;
                                #endregion


                                RecurseAutomorphism(ref found);


                                #region H cleanup
                                deepness -= 1;
                                for (i = 0; i < hVerticesToRemoveFromEnvelopeLimit; i += 1)
                                {
                                    hOutsiders[hOutsidersLimit] = hVerticesToRemoveFromEnvelope[i];
                                    hOutsidersLimit += 1;
                                }

                                // restore hEnvelope to original state
                                hEnvelopeLimit = hOriginalSize;
                                hEnvelope[hEnvelopeLimit] = hEnvelope[isomorphicHIndices[hCandidate]];
                                hEnvelope[isomorphicHIndices[hCandidate]] = hMatchingCandidate;
                                hEnvelopeLimit += 1;


                                mappingCount -= 1;

                                #endregion

                            }
                            #region G cleanup
                            totalNumberOfEdgesInSubgraph -= newEdges;
                            // restore gEnvelope to original state
                            gEnvelopeLimit = gEnvelopeOriginalSize;
                            for (i = 0; i < gVerticesToRemoveFromEnvelopeLimit; i += 1)
                            {
                                gOutsiders[gOutsidersLimit] = gVerticesToRemoveFromEnvelope[i];
                                gOutsidersLimit += 1;
                            }
                            #endregion
                        }
                        gEnvelope[gEnvelopeLimit] = gEnvelope[gMatchingCandidateIndex];
                        gEnvelope[gMatchingCandidateIndex] = gMatchingCandidate;
                        gEnvelopeLimit += 1;
                        // the procedure has left the recursion step having the internal state unchanged
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
                gOutsidersLimit > 0
                && hOutsidersLimit > 0
                && (!findGraphGinH || gEnvelopeLimit == 0)
                && (subgraphScoringFunction(hOutsidersLimit + currentlyBuiltVertices, hOutsidersLimit * (hOutsidersLimit - 1) / 2 + currentlyBuiltEdges) * approximationRatio > bestScore)
                && (subgraphScoringFunction(gOutsidersLimit + currentlyBuiltVertices, gOutsidersLimit * (gOutsidersLimit - 1) / 2 + currentlyBuiltEdges) * approximationRatio > bestScore)
                )
            {
                var gOutsiderGraph = g.DeepCloneHavingVerticesIntersectedWith(new HashSet<int>(gOutsiders.Take(gOutsidersLimit)));
                var hOutsiderGraph = h.DeepCloneHavingVerticesIntersectedWith(new HashSet<int>(hOutsiders.Take(hOutsidersLimit)));
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
                    while (
                        gOutsiderGraph.Vertices.Count > 0
                        && subgraphScoringFunction(gOutsiderGraph.Vertices.Count + currentlyBuiltVertices, gOutsiderGraph.EdgeCount + currentlyBuiltEdges) * approximationRatio > bestScore
                        )
                    {
                        // choose the candidate with largest degree within the graph of outsiders
                        // if there is an ambiguity then choose the vertex with the largest degree in the original graph
                        var gMatchingCandidate = gOutsiderGraph.Vertices.ArgMax(v => gOutsiderGraph.VertexDegree(v));

                        foreach (var hMatchingCandidate in hOutsiderGraph.Vertices)
                        {
                            new CoreAlgorithm().InternalStateSetup(
                                 gMatchingCandidate,
                                 hMatchingCandidate,
                                 gOutsiderGraph,
                                 hOutsiderGraph,
                                 (int vertices, int edges) => subgraphScoringFunction(vertices + currentlyBuiltVertices, edges + currentlyBuiltEdges),
                                 (newScore, ghMap, hgMap, edges) => newSolutionFoundNotificationAction?.Invoke(
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
                                     ),
                                 analyzeDisconnected: analyzeDisconnected,
                                 findGraphGinH: findGraphGinH,
                                 approximationRatio: approximationRatio,
                                 gConnectionExistence: subgraphsSwapped ? hConnectionExistence : gConnectionExistence,
                                 hConnectionExistence: subgraphsSwapped ? gConnectionExistence : hConnectionExistence,
                                 deepnessTakeawaySteps: Math.Max(0, deepnessTakeawaySteps - deepness),
                                 leftoverSteps: leftoverSteps
                            ).Recurse(ref bestScore);
                        }

                        if (findGraphGinH)
                            break;

                        gOutsiderGraph.RemoveVertex(gMatchingCandidate);
                    }
                }
            }
        }
    }
}
