﻿using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class SerialSubgraphIsomorphismApproximator
    {
        // let D be max{|G|,|H|}
        // upper bound polynomial is on the order of O(D^4)
        public static void ApproximateOptimalSubgraph(
            UndirectedGraph gArgument,
            UndirectedGraph hArgument,
            Func<int, int, double> graphScoringFunction,
            int seed,
            out double bestScore,
            out int subgraphEdges,
            out Dictionary<int, int> ghOptimalMapping,
            out Dictionary<int, int> hgOptimalMapping,
            bool analyzeDisconnected = false,
            bool findExactMatch = false
            )
        {
            if (!analyzeDisconnected && findExactMatch)
                throw new Exception("Cannot analyze only connected components if seeking exact matches. Please change the parameter 'analyzeDisconnected' to true.");
            if (findExactMatch)
                throw new Exception("Feature not yet supported.");

            #region Initial setup
            var gMax = gArgument.Vertices.Max();

            var gConnectionExistance = new bool[gMax + 1, gMax + 1];
            var hConnectionExistance = new bool[hArgument.Vertices.Count, hArgument.Vertices.Count];

            foreach (var kvp in gArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    gConnectionExistance[kvp.Key, vertexTo] = true;

            foreach (var kvp in hArgument.Neighbours)
                foreach (var vertexTo in kvp.Value)
                    hConnectionExistance[kvp.Key, vertexTo] = true;
            #endregion

            CoreInternalState initialSetupPreMatch(int gMatchingVertex, int hMatchingVertex, int additionalOrder = 0)
            {
                // todo: cache more immutable!
                //setupCore.HighLevelSetup(gMatchingVertex, hMatchingVertex, gArgument, hArgument, graphScoringFunction, null, analyzeDisconnected, findExactMatch);
                var stateToImport = new CoreInternalState()
                {
                    g = gArgument,
                    h = hArgument,
                    gInitialChoice = gMatchingVertex,
                    hInitialChoice = hMatchingVertex,
                    findExactMatch = findExactMatch,
                    analyzeDisconnected = analyzeDisconnected,
                    graphScoringFunction = graphScoringFunction,
                    ghMapping = new Dictionary<int, int>(),
                    hgMapping = new Dictionary<int, int>(),
                    gEnvelope = new HashSet<int>() { gMatchingVertex },
                    hEnvelope = new HashSet<int>() { hMatchingVertex },
                    gOutsiders = new HashSet<int>(gArgument.Vertices),
                    hOutsiders = new HashSet<int>(hArgument.Vertices),
                    totalNumberOfEdgesInSubgraph = 0,

                    gConnectionExistance = gConnectionExistance,
                    hConnectionExistance = hConnectionExistance,
                };

                stateToImport.gOutsiders.Remove(gMatchingVertex);
                stateToImport.hOutsiders.Remove(hMatchingVertex);

                return stateToImport;
            };

            // make the best local choice
            var bestLocalSetup = new CoreInternalState();
            var random = new Random(seed);
            var archivedBestConnectionDetails = new Tuple<double, int>(double.MinValue, 0);
            // while there is an increase in result continue to approximate

            var step = 0;
            while (true)
            {
                var listOfLocalBestNextSetup = new List<CoreInternalState>();
                var listOfLocalBestConnectionDetails = new List<Tuple<double, int>>();
                var latestScore = double.MinValue;

                #region PREDICTION
                void makePrediction(int gCandidate, int hCandidate, int additionalOrder = 0)
                {
                    var localSetup = bestLocalSetup.Clone();
                    var predictor = new CoreAlgorithm();
                    predictor.ImportShallowInternalState(localSetup);

                    if (predictor.TryMatchFromEnvelopeMutateInternalState(gCandidate, hCandidate))
                    {
                        var score = graphScoringFunction(localSetup.ghMapping.Keys.Count, localSetup.totalNumberOfEdgesInSubgraph);
                        if (latestScore <= score)
                        {
                            if (latestScore < score)
                            {
                                latestScore = score;
                                listOfLocalBestNextSetup.Clear();
                                listOfLocalBestConnectionDetails.Clear();
                            }

                            listOfLocalBestNextSetup.Add(predictor.ExportShallowInternalState().Clone());
                            listOfLocalBestConnectionDetails.Add(new Tuple<double, int>(score, localSetup.totalNumberOfEdgesInSubgraph));
                        }
                    }
                }
                #endregion

                if (step == 0)
                {
                    foreach (var gCandidate in gArgument.Vertices.ToArray())
                        foreach (var hCandidate in hArgument.Vertices.ToArray())
                        {
                            bestLocalSetup = initialSetupPreMatch(gCandidate, hCandidate);
                            makePrediction(gCandidate, hCandidate);
                        }
                }
                else
                {
                    foreach (var gCandidate in bestLocalSetup.gEnvelope)
                        foreach (var hCandidate in bestLocalSetup.hEnvelope)
                            makePrediction(gCandidate, hCandidate);
                }
                
                if (listOfLocalBestConnectionDetails.Count == 0)
                {
                    break;
                }
                else
                {
                    var randomIndex = random.Next() % listOfLocalBestConnectionDetails.Count;
                    bestLocalSetup = listOfLocalBestNextSetup[randomIndex];
                    archivedBestConnectionDetails = listOfLocalBestConnectionDetails[randomIndex];
                }

                step += 1;
            }


            if (findExactMatch && bestLocalSetup.ghMapping.Count < gArgument.Vertices.Count)
            {
                // did not find an exact match
                bestScore = double.MinValue;
                subgraphEdges = 0;
                ghOptimalMapping = new Dictionary<int, int>();
                hgOptimalMapping = new Dictionary<int, int>();
            }
            else
            {
                bestScore = archivedBestConnectionDetails.Item1;
                subgraphEdges = archivedBestConnectionDetails.Item2;
                ghOptimalMapping = bestLocalSetup.ghMapping;
                hgOptimalMapping = bestLocalSetup.hgMapping;
            }
        }
    }
}
