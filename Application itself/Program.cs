﻿using System;
using System.Linq;
using MathParser;
using SubgraphIsomorphismExactAlgorithm;
using GraphDataStructure;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    class Program
    {
        static void Main(string[] args)
        {
            var firstTime = true;
            bool stringToBool(string input)
            {
                var normalizedInput = input.ToLower();
                switch (normalizedInput)
                {
                    case "yes":
                    case "y":
                    case "true":
                    case "t":
                    case "tak":
                        return true;
                    default:
                        return false;
                }
            }

            Func<int, int, double> stringToValuation(string input)
            {
                switch (input.ToLower())
                {
                    case "v":
                        return (v, e) => v;
                    case "e":
                        return (v, e) => e;
                    case "v+e":
                        return (v, e) => v + e;
                    case "e+v":
                        return (v, e) => v + e;
                    default:
                        break;
                }
                return Parse.ParseInput(input);
            }

            void inputInstructions()
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("1. `address of graph g without quotation marks \"` e.g. `g.csv` `C:\\...\\g.csv`");
                Console.WriteLine("2. `address of graph h without quotation marks \"`");
                Console.WriteLine("3. `output file address without quotation marks \"`");
                Console.WriteLine("4. `monotonic subraph valuation in 'vertices' and 'edges' without white characters` e.g. `v` `v+e` `e` (the only simple valuations that support single character interpretation) `vertices*edges` `vertices*log(1+edges)`");
                Console.WriteLine("5. `compute exactly or not (if not please provide the index of approximating algorithm)?` e.g. `yes` `true` `t` `1` `2`");
                Console.WriteLine("6*. `analyze disconnected?` (defaults to false)");
                Console.WriteLine("7*. `find exact matching of G in H?` (defaults to false, analyze disconnected must be also true if this is set to true)");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                Console.ResetColor();
            }

            while (true)
            {
                var input = args;
                if (args.Length < 2 || !firstTime)
                {
                    // parse input
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Please enter necessary arguments separated by a semicolon ; in a single line according to the following order:");
                    Console.ResetColor();
                    inputInstructions();
                    Console.WriteLine();
                    input = Console.ReadLine().Split(';', StringSplitOptions.RemoveEmptyEntries);
                }

                try
                {
                    GraphFileIO.Read(input[0], out var g);
                    GraphFileIO.Read(input[1], out var h);
                    var outputAddress = input[2];
                    var valuation = stringToValuation(input[3]);
                    var computeExactly = false;

                    if (!int.TryParse(input[4], out var approximatingIndex))
                    {
                        computeExactly = stringToBool(input[4]);
                    }

                    bool analyzeDisconnected = false;
                    if (input.Length > 5)
                        analyzeDisconnected = stringToBool(input[5]);

                    bool findExactMatch = false;
                    if (input.Length > 6)
                        analyzeDisconnected = stringToBool(input[6]);

                    // order of polynomial

                    double bestScore;
                    int subgraphEdges;
                    Dictionary<int, int> ghOptimalMapping;
                    Dictionary<int, int> hgOptimalMapping;

                    if (computeExactly)
                    {
                        ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                            g,
                            h,
                            valuation,
                            out bestScore,
                            out subgraphEdges,
                            out ghOptimalMapping,
                            out hgOptimalMapping,
                            analyzeDisconnected,
                            findExactMatch
                            );
                    }
                    else
                    {
                        if (approximatingIndex == 1)
                        {

                            SubgraphIsomorphismGrouppedApproximability.ApproximateOptimalSubgraph(
                                g,
                                h,
                                valuation,
                                out bestScore,
                                out subgraphEdges,
                                out ghOptimalMapping,
                                out hgOptimalMapping,
                                analyzeDisconnected,
                                findExactMatch,
                                1000
                                );
                        }
                        else if (approximatingIndex == 2)
                        {
                            ParallelSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
                                g,
                                h,
                                valuation,
                                out bestScore,
                                out subgraphEdges,
                                out ghOptimalMapping,
                                out hgOptimalMapping,
                                analyzeDisconnected,
                                findExactMatch,
                                (g.EdgeCount + h.EdgeCount + g.Vertices.Count + h.Vertices.Count) * 150
                                );
                        }
                        else
                        {
                            throw new Exception("Incorrect index of approximating algorithm. Please use `1` or `2`.");
                        }
                    }

                    // print matches if filepath is valid
                    PrintTransition(outputAddress, ghOptimalMapping);

                    var lightColour = computeExactly ? ConsoleColor.Green : ConsoleColor.Cyan;
                    var darkColour = computeExactly ? ConsoleColor.DarkGreen : ConsoleColor.DarkCyan;

                    Console.WriteLine("Indices in the first row and first column stand for indices of vertices in the relevant graph.");
                    Console.WriteLine("Green indicees belong to the isomorphic subgraph and are ordered so that the mapping between the vertices between two subgraphs becomes trivial to figure out.");

                    Console.WriteLine($"Achieved score: {bestScore}");

                    var order = ghOptimalMapping.Keys.ToArray();
                    Console.WriteLine("Graph G:");
                    g.PrintSubgraph(order, ghOptimalMapping, darkColour, lightColour);
                    Console.WriteLine();

                    Console.WriteLine("Graph H:");
                    h.PrintSubgraph(order.Select(key => ghOptimalMapping[key]).ToArray(), hgOptimalMapping, darkColour, lightColour);
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occured. Make sure the arguments are entered correctly (without backticks):");
                    inputInstructions();
                    Console.WriteLine("Affirmative answers supported: true, yes, y, t, tak. Any other answer is treated as negative (or as a special command in some cases).");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Details of the error: {e.ToString()}");
                    Console.ResetColor();
                }
                firstTime = false;
            }
        }

        private static void PrintTransition(string outputAddress, Dictionary<int, int> ghOptimalMapping)
        {
            var standardisedFrom = ghOptimalMapping.Keys.ToArray();
            var standardisedTo = standardisedFrom.Select(from => ghOptimalMapping[from]);
            var builder = new StringBuilder();

            builder.AppendJoin(',', standardisedFrom);
            builder.AppendLine();
            builder.AppendJoin(',', standardisedTo);

            using (var file = new StreamWriter(outputAddress))
            {
                file.Write(builder);
            }
        }
    }
}
