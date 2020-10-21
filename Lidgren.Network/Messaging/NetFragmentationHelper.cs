using System;

namespace Lidgren.Network
{
    internal static class NetFragmentationHelper
    {
        internal static void WriteHeader(
            Span<byte> destination,
            ref int offset,
            int group,
            int totalBits,
            int chunkByteSize,
            int chunkNumber)
        {
            uint num1 = (uint)group;
            while (num1 >= 0x80)
            {
                destination[offset++] = (byte)(num1 | 0x80);
                num1 >>= 7;
            }
            destination[offset++] = (byte)num1;

            // write variable length fragment total bits
            uint num2 = (uint)totalBits;
            while (num2 >= 0x80)
            {
                destination[offset++] = (byte)(num2 | 0x80);
                num2 >>= 7;
            }
            destination[offset++] = (byte)num2;

            // write variable length fragment chunk size
            uint num3 = (uint)chunkByteSize;
            while (num3 >= 0x80)
            {
                destination[offset++] = (byte)(num3 | 0x80);
                num3 >>= 7;
            }
            destination[offset++] = (byte)num3;

            // write variable length fragment chunk number
            uint num4 = (uint)chunkNumber;
            while (num4 >= 0x80)
            {
                destination[offset++] = (byte)(num4 | 0x80);
                num4 >>= 7;
            }
            destination[offset++] = (byte)num4;
        }

        internal static bool ReadHeader(
            ReadOnlySpan<byte> buffer, ref int offset,
            out int group, out int totalBits, out int chunkByteSize, out int chunkNumber)
        {
            int part;
            int shift;

            shift = 0;
            group = 0;
            do
            {
                if (shift == 5 * 7)
                {
                    totalBits = default;
                    chunkByteSize = default;
                    chunkNumber = default;
                    return false;
                }

                part = buffer[offset++];
                group |= (part & 0x7F) << shift;
                shift += 7;
            } while ((part & 0x80) == 0);

            shift = 0;
            totalBits = 0;
            do
            {
                if (shift == 5 * 7)
                {
                    chunkByteSize = default;
                    chunkNumber = default;
                    return false;
                }

                part = buffer[offset++];
                totalBits |= (part & 0x7F) << shift;
                shift += 7;
            } while ((part & 0x80) == 0);

            shift = 0; 
            chunkByteSize = 0;
            do
            {
                if (shift == 5 * 7)
                {
                    chunkNumber = default;
                    return false;
                }

                part = buffer[offset++];
                chunkByteSize |= (part & 0x7F) << shift;
                shift += 7;
            } while ((part & 0x80) == 0);

            shift = 0; 
            chunkNumber = 0;
            do
            {
                if (shift == 5 * 7)
                    return false;

                part = buffer[offset++];
                chunkNumber |= (part & 0x7F) << shift;
                shift += 7;
            } while ((part & 0x80) == 0);

            return true;
        }

        internal static int GetFragmentationHeaderSize(int groupId, int totalBits, int chunkByteSize, int numChunks)
        {
            return (
                NetBitWriter.BitsForValue((uint)groupId) +
                NetBitWriter.BitsForValue((uint)totalBits) +
                NetBitWriter.BitsForValue((uint)chunkByteSize) +
                NetBitWriter.BitsForValue((uint)numChunks)) / 7 + 4;
        }

        internal static int GetBestChunkSize(int group, int totalBytes, int mtu)
        {
            // TODO: optimize

            int totalBits = totalBytes * 8;
            int tryChunkSize = mtu - NetConstants.HeaderByteSize - 4; // naive approximation
            int est = GetFragmentationHeaderSize(group, totalBits, tryChunkSize, totalBytes / tryChunkSize);
            tryChunkSize = mtu - NetConstants.HeaderByteSize - est; // slightly less naive approximation

            int headerSize;
            do
            {
                tryChunkSize--; // keep reducing chunk size until it fits within MTU including header

                int numChunks = totalBytes / tryChunkSize;
                if (numChunks * tryChunkSize < totalBytes)
                    numChunks++;

                headerSize = GetFragmentationHeaderSize(group, totalBits, tryChunkSize, numChunks); // 4+ bytes

            } while (tryChunkSize + headerSize + NetConstants.HeaderByteSize + 1 >= mtu);

            return tryChunkSize;
        }
    }
}
