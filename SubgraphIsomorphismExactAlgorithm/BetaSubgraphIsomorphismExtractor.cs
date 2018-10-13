using System;
using System.Collections.Generic;
using System.Text;
using GraphDataStructure;

namespace SubgraphIsomorphismExactAlgorithm
{
    public class BetaSubgraphIsomorphismExtractor<T> : ISubgraphIsomorphismExtractor<T>
        where T : IComparable
    {
        public void Extract(UndirectedGraph argG, UndirectedGraph argH, Func<int, int, T> graphScore, T initialScore, out T score, out Dictionary<int, int> gBestSolution, out Dictionary<int, int> hBestSolution)
        {
            // todo: reimplement
            score = initialScore;
            gBestSolution = new Dictionary<int, int>();
            hBestSolution = new Dictionary<int, int>();

            var initialPackets = new List<PacketG>();
            foreach (var gConnection in argG.Connections)
            {
                var gFromVertex = gConnection.Key;

                var gEnvelope = 0UL;
                foreach (var gToVertex in gConnection.Value)
                    gEnvelope |= 1UL << gToVertex;

                foreach (var gToVertex in gConnection.Value)
                {
                    foreach (var hConnection in argH.Connections)
                    {
                        var hFromVertex = hConnection.Key;

                        var hEnvelope = 0UL;
                        foreach (var hToVertex in hConnection.Value)
                            hEnvelope |= 1UL << hToVertex;

                        foreach (var hToVertex in hConnection.Value)
                        {
                            // unique pair-pair connection found...
                            var hPacket = new PacketH((1UL << hFromVertex) | (1UL << hToVertex), hEnvelope)
                            {
                                verticesEnumerable = new HashSet<int>() { hFromVertex, hToVertex }
                            };
                            var gPacket = new PacketG((1UL << gFromVertex) | (1UL << gToVertex), gEnvelope)
                            {
                                verticesEnumerable = new HashSet<int>() { gFromVertex, gToVertex },
                                isomorphicTo = hPacket,
                                gToH = new Dictionary<int, int>() { { gFromVertex, hFromVertex }, { gToVertex, hFromVertex } },
                                hToG = new Dictionary<int, int>() { { hFromVertex, gFromVertex }, { hToVertex, hToVertex } },
                            };
                            initialPackets.Add(gPacket);
                        }
                    }
                }
            }
            // successfully created connections
            var futurePackets = new List<List<PacketG>>
            {
                new List<PacketG>()
            };

            for (int i = 0; i < initialPackets.Count - 1; i++)
            {
                for (int j = i + 1; j < initialPackets.Count; j++)
                {
                    if (CheckValidity(argG, argH, initialPackets[i], initialPackets[j], out var newPacket))
                    {
                        futurePackets[0].Add(newPacket);
                    }
                }
            }
        }


        private bool CheckValidity(UndirectedGraph g, UndirectedGraph h, PacketG gPacket1, PacketG gPacket2, out PacketG gMerged)
        {
            gMerged = null;

            // possibly check for the number of highlighted bits when intersecting with the envelope? are they equal for g and h?
            if (gPacket1.AreValidCandidatesToMerge(gPacket2) && gPacket1.isomorphicTo.AreValidCandidatesToMerge(gPacket2.isomorphicTo))
            {

                foreach (var gVertex1 in gPacket1.verticesEnumerable)
                {
                    foreach (var gVertex2 in gPacket2.verticesEnumerable)
                    {
                        if (g.ExistsConnectionBetween(gVertex1, gVertex2) == h.ExistsConnectionBetween(gPacket1.gToH[gVertex1], gPacket2.gToH[gVertex2]))
                        {
                            // connection is ok
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                var hVertices = gPacket1.isomorphicTo.vertices | gPacket2.isomorphicTo.vertices;
                var hEnvelope = (gPacket1.isomorphicTo.envelope & (~gPacket2.isomorphicTo.vertices)) | (gPacket2.isomorphicTo.envelope & (~gPacket1.isomorphicTo.vertices));
                var hNewMerged = new PacketH(hVertices, hEnvelope)
                {
                    verticesEnumerable = new HashSet<int>(gPacket1.isomorphicTo.verticesEnumerable)
                };
                foreach (var gToHnew in gPacket2.gToH)
                {
                    hNewMerged.verticesEnumerable.Add(gToHnew.Key);
                }

                var gVertices = gPacket1.vertices | gPacket2.vertices;
                var gEnvelope = (gPacket1.envelope & (~gPacket2.vertices)) | (gPacket2.envelope & (~gPacket1.vertices));
                var gNewMerged = new PacketG(gVertices, gEnvelope)
                {
                    gToH = new Dictionary<int, int>(gPacket1.gToH),
                    isomorphicTo = hNewMerged,
                    hToG = new Dictionary<int, int>(gPacket1.hToG),
                    verticesEnumerable = new HashSet<int>(gPacket1.verticesEnumerable)
                };
                foreach (var gToHnew in gPacket2.gToH)
                {
                    gNewMerged.gToH.Add(gToHnew.Key, gToHnew.Value);
                    gNewMerged.verticesEnumerable.Add(gToHnew.Key);
                }
                foreach (var hToGnew in gPacket2.hToG)
                {
                    gNewMerged.hToG.Add(hToGnew.Key, hToGnew.Value);
                }

                gMerged = gNewMerged;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
