using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class SerialSubgraphIsomorphismGrouppedApproximability
    {
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

            var valuations = new Func<int, int, int>[]
            {
                (d1, d2) => d1 * d2,
                (d1, d2) => Math.Min(d1, d2),
                (d1, d2) => Math.Max(d1, d2),
                (d1, d2) => (int)Math.Pow(d1, d2)+(int)Math.Pow(d1, d2),
                (d1, d2) => (int)Math.Pow(d1, d2)*(int)Math.Pow(d1, d2),
                (d1, d2) => d1 + d2,
            };
            var valuationDescriptions = new string[]
            {
                "product",
                "min",
                "max",
                "sum of powers",
                "product of powers",
                "sum",
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

            for (int valuationIndex = 0; valuationIndex < valuations.Length; valuationIndex++)
            {
                if (bestValuations.Contains(valuationIndex))
                    Console.ForegroundColor = ConsoleColor.Green;
                else
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(valuationDescriptions[valuationIndex]);

                Console.ResetColor();
            }
        }
    }
}
