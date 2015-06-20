using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class ArchiveSliceHeader
    {
        public static byte[] Signature = ASCIIEncoding.ASCII.GetBytes("<<<<<<<<<<~LTA~v3~SLC~>>>>>>>>>>");
        public Guid SetIdentifier;
        public uint Sequence;
        public uint DataLength;
        public uint PaddingLength;
        public byte DataPartitions;
        public byte CodingPartitions;
        public byte CodingWordSize;
        public UInt32 ContentCrc;
        public UInt32 Crc;

        public bool IsValid;

        public static int HeaderLength = 71;
        public static int DataPartitionCountOffset = 60;

        public static ArchiveSliceHeader TryRead(Stream fs, long offset)
        {
            byte[] headerData = new byte[HeaderLength];

            try
            {
                var header = new ArchiveSliceHeader();
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(headerData, 0, HeaderLength);

                byte[] guidBytes = new byte[16];
                Array.ConstrainedCopy(headerData, 32, guidBytes, 0, 16);
                header.SetIdentifier = new Guid(guidBytes);

                header.Sequence = BitConverter.ToUInt32(headerData, 48);
                header.DataLength = BitConverter.ToUInt32(headerData, 52);
                header.PaddingLength = BitConverter.ToUInt32(headerData, 56);
                header.DataPartitions = headerData[60];
                header.CodingPartitions = headerData[61];
                header.CodingWordSize = headerData[62];
                header.ContentCrc = BitConverter.ToUInt32(headerData, 63);
                header.Crc = BitConverter.ToUInt32(headerData, 67);

                header.IsValid = headerData.Take(32).SequenceEqual(Signature) && (header.Crc == Crc32C.Crc32CAlgorithm.Compute(headerData, 0, HeaderLength - 4));

                return header;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Unexpected error reading slice header: {0}", ex.Message);
                Console.WriteLine();
                return null;
            }
        }
    }
}
