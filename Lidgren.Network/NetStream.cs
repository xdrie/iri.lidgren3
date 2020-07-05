using System;
using System.IO;

namespace Lidgren.Network
{
    public class NetStream //: Stream
    {
        public NetPeer Peer { get; }

        public NetStream(NetPeer peer)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
        }
    }
}
