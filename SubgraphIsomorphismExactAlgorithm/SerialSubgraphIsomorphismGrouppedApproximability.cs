﻿using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class SerialSubgraphIsomorphismGrouppedApproximability
    {
        public static void ApproximateOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, double> graphScoringFunction,
            out double bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            ) => ApproximateOptimalSubgraph(
                            3,
                            gArgument,
                            hArgument,
                            graphScoringFunction,
                            out bestScore,
                            out subgraphEdges,
                            out ghOptimalMapping,
                            out hgOptimalMapping,
                            analyzeDisconnected,
                            findExactMatch
                            );

        public static void ApproximateOptimalSubgraph(
        int orderOfPolynomial,
        UndirectedGraph gArgument,
        UndirectedGraph hArgument,
        Func<int, int, double> graphScoringFunction,
        out double bestScore,
        out int subgraphEdges,
        out Dictionary<int, int> ghOptimalMapping,
        out Dictionary<int, int> hgOptimalMapping,
        bool analyzeDisconnected = false,
        bool findExactMatch = false
        )
        {
            bestScore = double.MinValue;
            subgraphEdges = 0;
            ghOptimalMapping = new Dictionary<int, int>();
            hgOptimalMapping = new Dictionary<int, int>();

            var maxScore = double.NegativeInfinity;
            double localScore;
            int localEdges;
            Dictionary<int, int> ghLocalMapping;
            Dictionary<int, int> hgLocalMapping;

            var bestValuations = new HashSet<int>();
            const int randomTrials = 20;
            var random = new Random(0);
            for (int valuationIndex = 0; valuationIndex < randomTrials; valuationIndex++)
            {
                SerialSubgraphIsomorphismApproximator.ApproximateOptimalSubgraph(
                    orderOfPolynomial,
                    gArgument,
                    hArgument,
                    graphScoringFunction,
                    random.Next(),
                    out localScore,
                    out localEdges,
                    out ghLocalMapping,
                    out hgLocalMapping,
                    analyzeDisconnected,
                    findExactMatch
                    );

                if (localScore > maxScore)
                {
                    bestValuations.Clear();
                    bestValuations.Add(valuationIndex);
                }
                else if (localScore == maxScore)
                {
                    bestValuations.Add(valuationIndex);
                }
                if (localScore > maxScore)
                {
                    bestScore = maxScore = localScore;
                    subgraphEdges = localEdges;
                    ghOptimalMapping = new Dictionary<int, int>(ghLocalMapping);
                    hgOptimalMapping = new Dictionary<int, int>(hgLocalMapping);
                }
            }

            //Console.WriteLine();
            //for (int valuationIndex = 0; valuationIndex < possibleCompinations.Count; valuationIndex++)
            //{
            //    if (bestValuations.Contains(valuationIndex))
            //        Console.ForegroundColor = ConsoleColor.DarkCyan;
            //    else
            //        Console.ForegroundColor = ConsoleColor.DarkGray;

            //    Console.WriteLine(valuationIndex.ToString().PadLeft(10));

            //    Console.ResetColor();
            //}
            //Console.WriteLine($"score: {bestScore}");
            //Console.WriteLine();
        }
    }
}