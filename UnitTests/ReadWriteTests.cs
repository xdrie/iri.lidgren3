using System;
using Lidgren.Network;
using System.Reflection;
using System.Text;

namespace UnitTests
{
    public static class ReadWriteTests
    {
        public static string ToBinaryString(ulong value, int bits, bool includeSpaces)
        {
            int numSpaces = Math.Max(0, (bits / 8) - 1);
            if (includeSpaces == false)
                numSpaces = 0;

            StringBuilder bdr = new StringBuilder(bits + numSpaces);
            for (int i = 0; i < bits + numSpaces; i++)
                bdr.Append(' ');

            for (int i = 0; i < bits; i++)
            {
                ulong shifted = value >> i;
                bool isSet = (shifted & 1) != 0;

                int pos = bits - 1 - i;
                if (includeSpaces)
                    pos += Math.Max(0, pos / 8);

                bdr[pos] = isSet ? '1' : '0';
            }
            return bdr.ToString();
        }

        public static void Run(NetPeer peer)
        {
            NetOutgoingMessage msg = peer.CreateMessage();

            msg.Write(false);
            msg.Write(-3, 6);
            msg.Write(42);
            
            msg.Write("duke of earl");
            msg.Write((byte)43);
            msg.Write((ushort)44);
            msg.Write(ulong.MaxValue, 64);
            msg.Write(true);

            msg.WritePadBits();

            int bcnt = 0;

            msg.Write(567845.0f);
            msg.WriteVar(2115998022);
            msg.Write(46.0);
            msg.Write((ushort)14, 9);
            bcnt += msg.WriteVar(-47);
            msg.WriteVar(470000);
            msg.WriteVar((uint)48);
            bcnt += msg.WriteVar((long)-49);

            if (bcnt != 2)
                throw new LidgrenException("WriteVariable* wrote too many bytes!");
           
            byte[] data = msg.Data;
            NetIncomingMessage inc = Program.CreateIncomingMessage(data, msg.BitLength);

            var bdr = new StringBuilder();

            bdr.Append(inc.ReadBoolean());
            bdr.Append(inc.ReadInt32(6));
            bdr.Append(inc.ReadInt32());

            if (!inc.ReadString(out string strResult))
                throw new LidgrenException("Read/write failure");
            bdr.Append(strResult);
            
            bdr.Append(inc.ReadByte());

            if (inc.PeekUInt16() != 44)
                throw new LidgrenException("Read/write failure");

            bdr.Append(inc.ReadUInt16());

            var pp = inc.PeekUInt64(64);
            if (pp != ulong.MaxValue)
                throw new LidgrenException("Read/write failure");

            bdr.Append(inc.ReadUInt64());
            bdr.Append(inc.ReadBoolean());
        
            inc.SkipPadBits();

            bdr.Append(inc.ReadSingle());
            bdr.Append(inc.ReadVarInt32());
            bdr.Append(inc.ReadDouble());
            bdr.Append(inc.ReadUInt32(9));
            bdr.Append(inc.ReadVarInt32());
            bdr.Append(inc.ReadVarInt32());
            bdr.Append(inc.ReadVarUInt32());
            bdr.Append(inc.ReadVarInt64());

            var bdrr = bdr.ToString();
            if (bdrr.Equals("False-342duke of earl434418446744073709551615True56784521159980224614-4747000048-49"))
                Console.WriteLine("Read/write tests OK");
            else
                throw new LidgrenException("Read/write tests FAILED!");

            msg = peer.CreateMessage();

            NetOutgoingMessage tmp = peer.CreateMessage();
            tmp.Write(42, 14);

            msg.Write(tmp);
            msg.Write(tmp);

            if (msg.BitLength != tmp.BitLength * 2)
                throw new LidgrenException("NetOutgoingMessage.Write(NetOutgoingMessage) failed!");

            tmp = peer.CreateMessage();

            Test test = new Test();
            test.Number = 42;
            test.Name = "Hallon";
            test.Age = 8.2f;

            tmp.WriteAllFields(test, BindingFlags.Public | BindingFlags.Instance);

            data = tmp.Data;

            inc = Program.CreateIncomingMessage(data, tmp.BitLength);

            Test readTest = new Test();
            inc.ReadAllFields(readTest, BindingFlags.Public | BindingFlags.Instance);

            LidgrenException.Assert(readTest.Number == 42);
            LidgrenException.Assert(readTest.Name == "Hallon");
            LidgrenException.Assert(readTest.Age == 8.2f);
            
            // test aligned WriteBytes/ReadBytes
            msg = peer.CreateMessage();
            byte[] originalData = new byte[] { 5, 6, 7, 8, 9 };
            msg.Write(originalData);

            inc = Program.CreateIncomingMessage(msg.Data, msg.BitLength);
            Span<byte> readData = stackalloc byte[originalData.Length]; 
            inc.Read(readData);

            if(!readData.SequenceEqual(originalData))
                    throw new Exception("Read fail");
        }
    }

    public class TestBase
    {
        public int Number;
    }

    public class Test : TestBase
    {
        public float Age;
        public string Name;
    }
}
