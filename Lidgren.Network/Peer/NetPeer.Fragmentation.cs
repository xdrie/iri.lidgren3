using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

namespace Lidgren.Network
{
    internal readonly struct ReceivedFragmentGroup
    {
        //public float LastReceived;
        public byte[] Data { get; }
        public NetBitVector ReceivedChunks { get; }

        public ReceivedFragmentGroup(byte[] data, NetBitVector receivedChunks)
        {
            Data = data;
            ReceivedChunks = receivedChunks;
        }
    }

    public partial class NetPeer
    {
        private int _lastUsedFragmentGroup;

        private Dictionary<NetConnection, Dictionary<int, ReceivedFragmentGroup>> _receivedFragmentGroups =
            new Dictionary<NetConnection, Dictionary<int, ReceivedFragmentGroup>>();

        // on user thread
        private NetSendResult SendFragmentedMessage(
            NetOutgoingMessage message,
            IReadOnlyCollection<NetConnection> recipients,
            NetDeliveryMethod method,
            int sequenceChannel)
        {
            // Note: this group id is PER SENDING/NetPeer; ie. same id is sent to all recipients;
            // this should be ok however; as long as recipients differentiate between same id but different sender
            int group = Interlocked.Increment(ref _lastUsedFragmentGroup);
            if (group >= NetConstants.MaxFragmentationGroups)
            {
                // @TODO: not thread safe; but in practice probably not an issue
                _lastUsedFragmentGroup = 1;
                group = 1;
            }
            message._fragmentGroup = group;

            // do not send msg; but set fragmentgroup in case user tries to recycle it immediately

            // create fragmentation specifics
            int totalBytes = message.ByteLength;

            // determine minimum mtu for all recipients
            int mtu = GetMTU(recipients);
            int bytesPerChunk = NetFragmentationHelper.GetBestChunkSize(group, totalBytes, mtu);

            int numChunks = totalBytes / bytesPerChunk;
            if (numChunks * bytesPerChunk < totalBytes)
                numChunks++;

            var retval = NetSendResult.Sent;

            int bitsPerChunk = bytesPerChunk * 8;
            int bitsLeft = message.BitLength;
            for (int i = 0; i < numChunks; i++)
            {
                NetOutgoingMessage chunk = CreateMessage(0);

                chunk.BitLength = bitsLeft > bitsPerChunk ? bitsPerChunk : bitsLeft;
                chunk.Data = message.Data;
                chunk._fragmentGroup = group;
                chunk._fragmentGroupTotalBits = totalBytes * 8;
                chunk._fragmentChunkByteSize = bytesPerChunk;
                chunk._fragmentChunkNumber = i;

                LidgrenException.Assert(chunk.BitLength != 0);
                LidgrenException.Assert(chunk.GetEncodedSize() < mtu);

                Interlocked.Add(ref chunk._recyclingCount, recipients.Count);

                foreach (var recipient in recipients.AsListEnumerator())
                {
                    var res = recipient.EnqueueMessage(chunk, method, sequenceChannel);
                    if ((int)res > (int)retval)
                        retval = res; // return "worst" result
                }

                bitsLeft -= bitsPerChunk;
            }

            return retval;
        }

        private void HandleReleasedFragment(NetIncomingMessage message)
        {
            if (message.SenderConnection == null)
                throw new ArgumentException("The message has no associated connection.", nameof(message));

            AssertIsOnLibraryThread();

            // read fragmentation header and combine fragments
            int headerOffset = NetFragmentationHelper.ReadHeader(
                message.Data, 0,
                out int group,
                out int totalBits,
                out int chunkByteSize,
                out int chunkNumber);

            LidgrenException.Assert(message.ByteLength > headerOffset);
            LidgrenException.Assert(group > 0);
            LidgrenException.Assert(totalBits > 0);
            LidgrenException.Assert(chunkByteSize > 0);

            int totalBytes = NetUtility.ByteCountForBits(totalBits);
            int totalChunkCount = totalBytes / chunkByteSize;
            if (totalChunkCount * chunkByteSize < totalBytes)
                totalChunkCount++;

            LidgrenException.Assert(chunkNumber < totalChunkCount);

            if (chunkNumber >= totalChunkCount)
            {
                LogWarning("Index out of bounds for chunk " + chunkNumber + " (total chunks " + totalChunkCount + ")");
                return;
            }

            if (!_receivedFragmentGroups.TryGetValue(message.SenderConnection, out var groups))
            {
                groups = new Dictionary<int, ReceivedFragmentGroup>();
                _receivedFragmentGroups.Add(message.SenderConnection, groups);
            }

            if (!groups.TryGetValue(group, out ReceivedFragmentGroup info))
            {
                info = new ReceivedFragmentGroup(new byte[totalBytes], new NetBitVector(totalChunkCount));
                groups.Add(group, info);
            }

            info.ReceivedChunks[chunkNumber] = true;
            //info.LastReceived = (float)NetTime.Now;

            // copy to data
            int offset = chunkNumber * chunkByteSize;
            Buffer.BlockCopy(message.Data, headerOffset, info.Data, offset, message.ByteLength - headerOffset);
            
            int chunkCount = info.ReceivedChunks.PopCount;
            //LogVerbose("Found fragment #" + chunkNumber + " in group " + group + " offset " + 
            //    offset + " of total bits " + totalBits + " (total chunks done " + cnt + ")");

            LogVerbose(
                "Received fragment " + chunkNumber + " of " + totalChunkCount + " (" + chunkCount + " chunks received)");

            if (info.ReceivedChunks.PopCount == totalChunkCount)
            {
                // Done! Transform this incoming message
                message.Data = info.Data;
                message.BitLength = totalBits;
                message.IsFragment = false;

                LogVerbose(
                    "Fragment group #" + group + " fully received in " +
                    totalChunkCount + " chunks (" + totalBits + " bits)");

                groups.Remove(group);

                ReleaseMessage(message);
            }
            else
            {
                // data has been copied; recycle this incoming message
                Recycle(message);
            }
            return;
        }
    }
}
