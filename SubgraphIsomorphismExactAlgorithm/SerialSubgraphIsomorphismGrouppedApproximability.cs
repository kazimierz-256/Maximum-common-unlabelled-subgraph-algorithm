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

            Func<int, int, int> min = (int a, int b) => Math.Max(a, b);
            Func<int, int, int> max = (int a, int b) => Math.Min(a, b);
            Func<int, int, int> power = (int a, int b) => (int)Math.Pow(a, b);

            var valuations = new Func<int, int, int, int, int>[]
            {
                (d1, d2, s1, s2) => d1 * d2,
                (d1, d2, s1, s2) => d1 + d2,
                (d1, d2, s1, s2) => min(d1, d2),
                (d1, d2, s1, s2) => max(d1, d2),
                (d1, d2, s1, s2) => power(d1,d2) + power(d2,d1),
                (d1, d2, s1, s2) => power(d1,d2) * power(d2,d1),
                (d1, d2, s1, s2) => min(power(d1,d2), power(d2,d1)),
                (d1, d2, s1, s2) => max(power(d1,d2), power(d2,d1)),
                (d1, d2, s1, s2) => (d1+s1) * (d2+s2),
                (d1, d2, s1, s2) => (d1+s1) + (d2+s2),
                (d1, d2, s1, s2) => (d1*s1) + (d2*s2),
                (d1, d2, s1, s2) => power(d1,d2) + power(d2,d1),
                (d1, d2, s1, s2) => power(d1,d2) * power(d2,d1),
                (d1, d2, s1, s2) => min(d1+s1, d2+s2),
                (d1, d2, s1, s2) => max(d1+s1, d2+s2),
                (d1, d2, s1, s2) => power(d1+s1, d2+s2)+power(d2+s2, d1+s1),
                (d1, d2, s1, s2) => power(d1 + s1, d2 + s2) * power(d2 + s2, d1 + s1),
                (d1, d2, s1, s2) => min(power(d1 + s1, d2 + s2), power(d2 + s2, d1 + s1)),
                (d1, d2, s1, s2) => max(power(d1 + s1, d2 + s2), power(d2 + s2, d1 + s1)),
            };
            var valuationDescriptions = new string[]
            {
                "product of degrees",
                "sum of degrees",
                "min of degrees",
                "max of degrees",
                "sum of powers of degrees",
                "product of powers of degrees",
                "min of powers of degrees",
                "max of powers of degrees",
                "product of subgraph connections and degrees",
                "sum of subgraph connections and degrees",
                "sum of product of subgraph connections and degrees",
                "strange sum of powers of subgraph connections and degrees",
                "strange product of powers of subgraph connections and degrees",
                "min of subgraph connections and degrees",
                "max of subgraph connections and degrees",
                "sum of powers of subgraph connections and degrees",
                "product of powers of subgraph connections and degrees",
                "min of powers of subgraph connections and degrees",
                "max of powers of subgraph connections and degrees",
            };

            var maxScore = double.NegativeInfinity;
            double localScore;
            int localEdges;
            Dictionary<int, int> ghLocalMapping;
            Dictionary<int, int> hgLocalMapping;

            var bestValuations = new HashSet<int>();

            for (int valuationIndex = 0; valuationIndex < valuations.Length; valuationIndex++)
            {
                SerialSubgraphIsomorphismApproximator.ApproximateOptimalSubgraph(
                    orderOfPolynomial,
                    gArgument,
                    hArgument,
                    graphScoringFunction,
                    valuations[valuationIndex],
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

            Console.WriteLine();
            for (int valuationIndex = 0; valuationIndex < valuations.Length; valuationIndex++)
            {
                if (bestValuations.Contains(valuationIndex))
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                else
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                Console.WriteLine(valuationDescriptions[valuationIndex]);

                Console.ResetColor();
            }
            Console.WriteLine();
        }
    }
}
