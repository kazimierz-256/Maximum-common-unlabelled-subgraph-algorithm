using System;
using System.Collections.Generic;
using System.Text;

namespace SubgraphIsomorphismExactAlgorithm
{
    public abstract class Packet
    {
        public HashSet<int> verticesEnumerable;
        public readonly ulong vertices;
        public readonly ulong envelope;
        public Packet(ulong vertices, ulong envelope)
        {
            this.vertices = vertices;
            this.envelope = envelope;
        }

        public bool IsConnectedTo(Packet packet) => (envelope & packet.vertices) != 0;
        public bool HaveDifferentVertices(Packet packet) => (vertices & packet.vertices) == 0;
        public bool AreValidCandidatesToMerge(Packet packet) => (envelope & packet.vertices) != 0 && (vertices & packet.vertices) == 0;
    }
    public class PacketH : Packet
    {
        public PacketH(ulong vertices, ulong envelope) : base(vertices, envelope)
        {
        }
    }
    public class PacketG : Packet
    {
        public PacketH isomorphicTo;
        public Dictionary<int, int> hToG;
        public Dictionary<int, int> gToH;
        public PacketG(ulong vertices, ulong envelope) : base(vertices, envelope)
        {
        }
    }
}
