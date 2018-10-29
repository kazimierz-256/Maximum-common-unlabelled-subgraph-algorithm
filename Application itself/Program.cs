using System;
using System.Linq;
using MathParser;
using SubgraphIsomorphismExactAlgorithm;
using GraphDataStructure;
using System.IO;

namespace Application_itself
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
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("`address of graph g` `address of graph h` `output file address` `monotonic subraph valuation in v and e without white characters` `compute exactly or not?` `analyze disconnected?` `find exact matching of G in H?` `launch in parallel? if exact or order of polynomial if approximate`");
                Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
                Console.ResetColor();
            }

            while (true)
            {
                var input = args;
                if (args.Length < 2 || !firstTime)
                {
                    // parse input
                    Console.WriteLine("Please enter necessary arguments according to the following instructions:");
                    inputInstructions();
                    input = Console.ReadLine().Split(" ");
                }

                try
                {
                    GraphFileIO.Read(input[0], out var g);
                    GraphFileIO.Read(input[1], out var h);
                    var outputAddress = input[2];
                    var valuation = stringToValuation(input[3]);
                    var initialValue = double.NegativeInfinity;
                    var computeExactly = stringToBool(input[4]);

                    bool analyzeDisconnected = false;
                    if (input.Length > 5)
                        analyzeDisconnected = stringToBool(input[5]);

                    bool findExactMatch = false;
                    if (input.Length > 6)
                        analyzeDisconnected = stringToBool(input[6]);

                    bool launchInParallel = true;
                    int orderOfPolynomial = 5;

                    if (input.Length > 7)
                    {
                        if (computeExactly)
                        {
                            launchInParallel = stringToBool(input[7]);
                        }
                        else
                        {
                            int.Parse(input[7]);
                        }
                    }
                    // order of polynomial

                    double bestScore;
                    int subgraphEdges;
                    System.Collections.Generic.Dictionary<int, int> ghOptimalMapping;
                    System.Collections.Generic.Dictionary<int, int> hgOptimalMapping;

                    if (computeExactly)
                    {
                        if (launchInParallel)
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
                            SerialSubgraphIsomorphismExtractor.ExtractOptimalSubgraph(
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
                    }
                    else
                    {
                        SerialSubgraphIsomorphismApproximator.ApproximateOptimalSubgraph(
                            g,
                            h,
                            valuation,
                            0,
                            out bestScore,
                            out subgraphEdges,
                            out ghOptimalMapping,
                            out hgOptimalMapping,
                            analyzeDisconnected,
                            findExactMatch
                            );
                    }

                    // print matches if filepath is valid?

                    var light = computeExactly ? ConsoleColor.Green : ConsoleColor.Cyan;
                    var dark = computeExactly ? ConsoleColor.DarkGreen : ConsoleColor.DarkCyan;

                    Console.WriteLine("Graph G:");
                    g.PrintSubgraph(ghOptimalMapping.Keys.ToArray(), ghOptimalMapping, dark, light);
                    Console.WriteLine();

                    Console.WriteLine("Graph H:");
                    h.PrintSubgraph(ghOptimalMapping.Keys.Select(key => ghOptimalMapping[key]).ToArray(), hgOptimalMapping, dark, light);
                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occured. Make sure the arguments are entered correctly (without backticks):");
                    inputInstructions();
                    Console.WriteLine("Affirmative answers supported: true, yes, y, t, tak. Any other answer is treated as negative.");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Details of the error: {e.ToString()}");
                    Console.ResetColor();
                }
                firstTime = false;
            }
        }
    }
}
