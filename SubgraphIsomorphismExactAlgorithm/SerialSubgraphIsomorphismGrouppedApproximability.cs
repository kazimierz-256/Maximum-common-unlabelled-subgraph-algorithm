using GraphDataStructure;
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

            var composers = new Func<int, int, int, int, int>[]
            {
                (int d1, int d2, int s1, int s2) => d1,
                (int d1, int d2, int s1, int s2) => d2,
                (int d1, int d2, int s1, int s2) => d1 + d2,
                (int d1, int d2, int s1, int s2) => d1 * d2,
                (int d1, int d2, int s1, int s2) => (int)Math.Pow(d1, d2),
                (int d1, int d2, int s1, int s2) => (int)Math.Pow(d2, d1),
                (int d1, int d2, int s1, int s2) => Math.Min(d1, d2),
                (int d1, int d2, int s1, int s2) => Math.Max(d2, d2),
                //(a, b, f1, f2) => f1(a, b),
                //(a, b, f1, f2) => f2(a, b),
                //(a, b, f1, f2) => f1(a, b) + f2(a, b),
                //(a, b, f1, f2) => f1(a, b) * f2(a, b),
                //(a, b, f1, f2) => Math.Min(f1(a, b), f2(a, b)),
                //(a, b, f1, f2) => Math.Max(f1(a, b), f2(a, b)),
                //(a, b, f1, f2) => (int)Math.Pow(f1(a, b), f2(a, b)),
                //(a, b, f1, f2) => (int)Math.Pow(f2(a, b), f1(a, b)),
            };
            
            var possibleCompinations = new List<Func<int, int, int, int, int>>();
            {
                var cloned = new Func<int, int, int, int, int>[possibleCompinations.Count];
                possibleCompinations.CopyTo(cloned);
                possibleCompinations.Clear();

                foreach (var composer1 in composers)
                    foreach (var composer2 in composers)
                    {
                        possibleCompinations.Add((d1, d2, s1, s2) => composer1(composer2(d1, d2, 0, 0), composer2(d1, d2, 0, 0), 0, 0));
                        possibleCompinations.Add((d1, d2, s1, s2) => composer1(composer2(s1, s2, 0, 0), composer2(s1, s2, 0, 0), 0, 0));
                        possibleCompinations.Add((d1, d2, s1, s2) => composer1(composer2(d1, s1, 0, 0), composer2(d2, s2, 0, 0), 0, 0));
                    }
            }

            var maxScore = double.NegativeInfinity;
            double localScore;
            int localEdges;
            Dictionary<int, int> ghLocalMapping;
            Dictionary<int, int> hgLocalMapping;

            var bestValuations = new HashSet<int>();

            for (int valuationIndex = 0; valuationIndex < possibleCompinations.Count; valuationIndex++)
            {
                SerialSubgraphIsomorphismApproximator.ApproximateOptimalSubgraph(
                    orderOfPolynomial,
                    gArgument,
                    hArgument,
                    graphScoringFunction,
                    possibleCompinations[valuationIndex],
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
