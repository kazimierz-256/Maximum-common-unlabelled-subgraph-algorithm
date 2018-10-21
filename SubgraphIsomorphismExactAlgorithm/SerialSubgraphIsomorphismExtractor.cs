﻿using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismExtractor<T> where T : IComparable
    {
        public static void ExtractOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, T> graphScoringFunction,
            T initialScore,
            out T bestScore,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            UndirectedGraph g, h;
            var solver = new CoreAlgorithm<T>();
            var swappedGraphs = false;

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

            var localBestScore = initialScore;
            var ghLocalOptimalMapping = new Dictionary<int, int>();
            var hgLocalOptimalMapping = new Dictionary<int, int>();

            while (graphScoringFunction(g.Vertices.Count, g.EdgeCount).CompareTo(localBestScore) > 0)
            {
                var gMatchingVertex = -1;
                var gMatchingScore = int.MaxValue;

                foreach (var gCandidate in g.Vertices)
                {
                    if (g.Degree(gCandidate) < gMatchingScore)
                    {
                        gMatchingScore = g.Degree(gCandidate);
                        gMatchingVertex = gCandidate;
                    }
                }

                foreach (var hMatchingVertex in h.Vertices)
                {
                    solver.SetupAndRecurse(gMatchingVertex, hMatchingVertex, g, h, graphScoringFunction, (newScore, ghMap, hgMap) =>
                    {
                        if (newScore.CompareTo(localBestScore) > 0)
                        {
                            localBestScore = newScore;
                            ghLocalOptimalMapping = ghMap();
                            hgLocalOptimalMapping = hgMap();
                        }
                    },
                    ref localBestScore, analyzeDisconnected, findExactMatch);

                }

                // ignore previous g-vertices
                g.RemoveVertex(gMatchingVertex);
            }


            // return the solution
            bestScore = localBestScore;
            if (swappedGraphs)
            {
                ghOptimalMapping = hgLocalOptimalMapping;
                hgOptimalMapping = ghLocalOptimalMapping;
            }
            else
            {
                ghOptimalMapping = ghLocalOptimalMapping;
                hgOptimalMapping = hgLocalOptimalMapping;
            }
        }
    }
}
