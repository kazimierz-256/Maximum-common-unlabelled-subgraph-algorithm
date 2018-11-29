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

        public int[][] isomorphicH;
        public int[][] isomorphicHIndices;
        public int[][] isomorphicCandidates;
        public int[][] isomorphicCandidatesIndices;

        public int[][] neighbours;
        public int[] gNeighbourCount;
        public int[] hNeighbourCount;
        public int[][] neighbourIndices;

        private int gVertexCount;
        private int hVertexCount;
        private int gEdgeCount;

        private int Remove(int vertex)
        {
            int i = 0, neighbour, neighbourCountMinus1, lastVertex, vertexIndex;
            for (int degree = gNeighbourCount[vertex]; i < degree; i += 1)
            {
                neighbour = neighbours[vertex][i];
                neighbourCountMinus1 = gNeighbourCount[neighbour] - 1;
                lastVertex = neighbours[neighbour][neighbourCountMinus1];
                vertexIndex = neighbourIndices[neighbour][vertex];
                // exchange vertices
                neighbours[neighbour][neighbourCountMinus1] = vertex;
                neighbours[neighbour][vertexIndex] = lastVertex;
                //update indices
                neighbourIndices[neighbour][lastVertex] = vertexIndex;
                neighbourIndices[neighbour][vertex] = neighbourCountMinus1;
                // finalize
                gNeighbourCount[neighbour] = neighbourCountMinus1;
            }

            return i;
        }

        private void Restore(int vertex)
        {
            // yeah, it's that simple
            for (int i = 0, degree = gNeighbourCount[vertex]; i < degree; i += 1)
                gNeighbourCount[neighbours[vertex][i]] += 1;
        }

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
            double approximationRatio = 1d,
            bool optimizeForAutomorphism = false,
            int[][] neighbours = null,
            int[] gNeighbourCount = null,
            int[][] neighbourIndices = null,
            int gEdgeCount = -1,
            HashSet<int> gAllowedSubsetVertices = null,
            HashSet<int> hAllowedSubsetVertices = null
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
            this.approximationRatio = approximationRatio;

            gVertexCount = gAllowedSubsetVertices == null ? g.Vertices.Count : gAllowedSubsetVertices.Count;
            hVertexCount = hAllowedSubsetVertices == null ? h.Vertices.Count : hAllowedSubsetVertices.Count;

            gMapping = new int[Math.Min(gVertexCount, hVertexCount)];
            hMapping = new int[gMapping.Length];
            mappingCount = 0;
            // for simplicity insert initial isomorphic vertices into the envelope
            gEnvelope = new int[gVertexCount];
            gEnvelope[0] = gInitialMatchingVertex;
            gEnvelopeLimit = 1;

            hEnvelope = new int[hVertexCount];
            hEnvelope[0] = hInitialMatchingVertex;
            hEnvelopeLimit = 1;

            gOutsiders = new int[gVertexCount - 1];
            gOutsidersLimit = 0;
            foreach (var gVertex in g.Vertices)
            {
                if (gVertex == gInitialMatchingVertex || (gAllowedSubsetVertices != null && !gAllowedSubsetVertices.Contains(gVertex)))
                    continue;

                gOutsiders[gOutsidersLimit] = gVertex;
                gOutsidersLimit += 1;
            }

            hOutsiders = new int[hVertexCount - 1];
            hOutsidersLimit = 0;
            foreach (var hVertex in h.Vertices)
            {
                if (hVertex == hInitialMatchingVertex || (hAllowedSubsetVertices != null && !hAllowedSubsetVertices.Contains(hVertex)))
                    continue;

                hOutsiders[hOutsidersLimit] = hVertex;
                hOutsidersLimit += 1;
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

            /// TOCONSIDER: should this be limited at any time?
            gEnvelopeHashes = new int[gMax + 1];
            hEnvelopeHashes = new int[hMax + 1];

            isomorphicH = new int[hVertexCount][];
            isomorphicHIndices = new int[hVertexCount][];
            isomorphicCandidates = new int[hVertexCount][];
            isomorphicCandidatesIndices = new int[hVertexCount][];
            for (int i = 0; i < hVertexCount; i += 1)
            {
                isomorphicH[i] = new int[hVertexCount - i];
                isomorphicHIndices[i] = new int[hVertexCount - i];
                isomorphicCandidates[i] = new int[hVertexCount - i];
                isomorphicCandidatesIndices[i] = new int[hVertexCount - i];
            }


            hNeighbourCount = new int[hMax + 1];
            if (hAllowedSubsetVertices == null)
            {
                foreach (var hVertex in h.Vertices)
                    hNeighbourCount[hVertex] = h.VertexDegree(hVertex);
            }
            else
            {
                foreach (var hVertex in h.Vertices)
                {
                    if (hAllowedSubsetVertices.Contains(hVertex))
                    {
                        var degree = 0;
                        foreach (var hVertex2 in h.Vertices)
                        {
                            if (hAllowedSubsetVertices.Contains(hVertex))
                                degree += 1;
                        }
                        hNeighbourCount[hVertex] = degree;
                    }
                }
            }

            if (neighbours == null)
            {
                this.gEdgeCount = 0;
                this.neighbours = new int[gMax + 1][];
                this.neighbourIndices = new int[gMax + 1][];
                this.gNeighbourCount = new int[gMax + 1];

                foreach (var gVertex in g.Vertices)
                {
                    if (gAllowedSubsetVertices != null && !gAllowedSubsetVertices.Contains(gVertex))
                        continue;
                    var degree = g.VertexDegree(gVertex);
                    if (!optimizeForAutomorphism)
                    {
                        this.neighbours[gVertex] = new int[degree];
                        this.neighbourIndices[gVertex] = new int[gMax + 1];
                        int i = 0;
                        foreach (var neighbour in g.VertexNeighbours(gVertex))
                        {
                            if (gAllowedSubsetVertices != null && !gAllowedSubsetVertices.Contains(neighbour))
                                continue;
                            this.neighbours[gVertex][i] = neighbour;
                            this.neighbourIndices[gVertex][neighbour] = i;
                            i += 1;
                        }
                        degree = i;
                    }
                    this.gNeighbourCount[gVertex] = degree;
                    this.gEdgeCount += degree;
                }
                this.gEdgeCount /= 2;
            }
            else
            {
                this.neighbours = neighbours;
                this.neighbourIndices = neighbourIndices;
                this.gNeighbourCount = gNeighbourCount;
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
            else if (subgraphScoringFunction(gVertexCount, gEdgeCount) * approximationRatio > bestScore)
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
                    for (int he = 0; he < hEnvelopeLimit; he += 1)
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
                            score += hNeighbourCount[hCan];
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
                        var thisDegree = gNeighbourCount[gCan];
                        if (degree == -1)
                            degree = gNeighbourCount[gMatchingCandidate];

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
                    for (int hCandidate = 0; hCandidate < totalNumberOfCandidates && subgraphScoringFunction(gVertexCount, gEdgeCount) * approximationRatio > bestScore; hCandidate += 1)
                    {
                        var hMatchingCandidate = isomorphicH[mappingCount][hCandidate];
                        // verify mutual agreement connections of neighbours

                        #region H setup

                        hEnvelope[isomorphicHIndices[mappingCount][hCandidate]] = hEnvelope[hEnvelopeLimit - 1];
                        hEnvelopeLimit -= 1;

                        var hEnvelopeOriginalLimit = hEnvelopeLimit;
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
                        hEnvelopeLimit = hEnvelopeOriginalLimit;
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
                if (!findGraphGinH && gVertexCount > 1 && subgraphScoringFunction(gVertexCount - 1, gEdgeCount - gNeighbourCount[gMatchingCandidate]) * approximationRatio > bestScore)
                {
                    var takeaway = Remove(gMatchingCandidate);
                    gVertexCount -= 1;
                    gEdgeCount -= takeaway;
                    deepness += 1;

                    Recurse(ref bestScore);

                    deepness -= 1;
                    gVertexCount += 1;
                    gEdgeCount += takeaway;
                    Restore(gMatchingCandidate);
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
                        var gDegree = gNeighbourCount[gCan];
                        for (int he = 0; he < hEnvelopeLimit; he += 1)
                        {
                            var hCan = hEnvelope[he];
                            if (hNeighbourCount[hCan] != gDegree || (gEnvelopeHashes != null && gHash != hEnvelopeHashes[hCan]))
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
                                score += hNeighbourCount[hCan];
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
                            var thisDegree = gNeighbourCount[gCan];
                            if (degree == -1)
                                degree = gNeighbourCount[gMatchingCandidate];

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
                        for (int hCandidate = 0; hCandidate < totalNumberOfCandidates && !found; hCandidate += 1)
                        {
                            var hMatchingCandidate = isomorphicH[mappingCount][hCandidate];
                            // verify mutual agreement connections of neighbours

                            #region H setup

                            hEnvelope[isomorphicHIndices[mappingCount][hCandidate]] = hEnvelope[hEnvelopeLimit - 1];
                            hEnvelopeLimit -= 1;

                            var hEnvelopeOriginalLimit = hEnvelopeLimit;
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


                            RecurseAutomorphism(ref found);


                            #region H cleanup
                            deepness -= 1;

                            mappingCount -= 1;
                            hOutsidersLimit = hOutsidersOriginalLimit;
                            // restore hEnvelope to original state
                            hEnvelopeLimit = hEnvelopeOriginalLimit;
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
                    gEnvelope[gEnvelopeLimit] = gEnvelope[gMatchingCandidateIndex];
                    gEnvelope[gMatchingCandidateIndex] = gMatchingCandidate;
                    gEnvelopeLimit += 1;
                    // the procedure has left the recursion step having the internal state unchanged
                }
            }
        }

        private void DisconnectComponentAndRecurse(ref double bestScore)
        {
            // if exact match is required then recurse only if the envelope set is empty
            if (
                gOutsidersLimit > 0
                && hOutsidersLimit > 0
                && (!findGraphGinH || gEnvelopeLimit == 0)
                && (subgraphScoringFunction(hOutsidersLimit + mappingCount, hOutsidersLimit * (hOutsidersLimit - 1) / 2 + totalNumberOfEdgesInSubgraph) * approximationRatio > bestScore)
                && (subgraphScoringFunction(gOutsidersLimit + mappingCount, gOutsidersLimit * (gOutsidersLimit - 1) / 2 + totalNumberOfEdgesInSubgraph) * approximationRatio > bestScore)
                )
            {
                var subgraphsSwapped = false;
                if (!findGraphGinH && hOutsidersLimit < gOutsidersLimit)
                {
                    subgraphsSwapped = true;
                }

                // if there is hope to improve the score then recurse
                var firstOutsiders = new HashSet<int>(gOutsiders.Take(gOutsidersLimit));
                var secondOutsiders = new HashSet<int>(hOutsiders.Take(hOutsidersLimit));
                var firstEdges = 0;
                var secondEdges = 0;
                foreach (var gVertex1 in firstOutsiders)
                {
                    foreach (var gVertex2 in firstOutsiders)
                    {
                        if (gVertex1 == gVertex2)
                            break;

                        if (gConnectionExistence[gVertex1, gVertex2])
                            firstEdges += 1;
                    }
                }
                foreach (var hVertex1 in secondOutsiders)
                {
                    foreach (var hVertex2 in secondOutsiders)
                    {
                        if (hVertex1 == hVertex2)
                            break;

                        if (hConnectionExistence[hVertex1, hVertex2])
                            secondEdges += 1;
                    }
                }

                if (subgraphsSwapped)
                {
                    var tmp = firstOutsiders;
                    firstOutsiders = secondOutsiders;
                    secondOutsiders = tmp;
                }

                if (subgraphScoringFunction(secondOutsiders.Count + mappingCount, secondEdges + totalNumberOfEdgesInSubgraph) * approximationRatio > bestScore)
                {
                    while (
                        firstOutsiders.Count > 0
                        && subgraphScoringFunction(firstOutsiders.Count + mappingCount, firstEdges + totalNumberOfEdgesInSubgraph) * approximationRatio > bestScore
                        )
                    {
                        // choose the candidate with largest degree within the graph of outsiders
                        // if there is an ambiguity then choose the vertex with the largest degree in the original graph
                        var firstMatchingCandidate = firstOutsiders.ArgMax(v => subgraphsSwapped ? hNeighbourCount[v] : gNeighbourCount[v]);

                        foreach (var secondMatchingCandidate in secondOutsiders)
                        {
                            new CoreAlgorithm().InternalStateSetup(
                                 firstMatchingCandidate,
                                 secondMatchingCandidate,
                                 subgraphsSwapped ? h : g,
                                 subgraphsSwapped ? g : h,
                                 (int vertices, int edges) => subgraphScoringFunction(vertices + mappingCount, edges + totalNumberOfEdgesInSubgraph),
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
                                 leftoverSteps: leftoverSteps,
                                 gAllowedSubsetVertices: firstOutsiders,
                                 hAllowedSubsetVertices: secondOutsiders
                            ).Recurse(ref bestScore);
                        }

                        if (findGraphGinH)
                            break;

                        foreach (var vertex1 in firstOutsiders)
                        {
                            foreach (var vertex2 in firstOutsiders)
                            {
                                if (vertex1 == vertex2)
                                    break;

                                if (subgraphsSwapped ? hConnectionExistence[vertex1, vertex2] : gConnectionExistence[vertex1, vertex2])
                                    firstEdges -= 1;
                            }
                        }

                        firstOutsiders.Remove(firstMatchingCandidate);
                    }
                }

            }
        }
    }
}
