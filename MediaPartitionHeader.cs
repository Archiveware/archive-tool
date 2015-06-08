using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class MediaPartitionHeader
    {
        public static byte[] Signature = ASCIIEncoding.ASCII.GetBytes("<<<<<<<<<<~LTA~v3~P7N~>>>>>>>>>>");
        public Guid SetIdentifier;
        public uint SliceSequence;
        public uint SliceDataOffset;
        public uint Sequence;
        public long DataOffset;
        public uint DataLength;
        public long CodingOffset;
        public uint CodingLength;
        public byte CodingChunks;
        public byte CodingWordSize;
        public UInt32[] ChunkCrc32 = new UInt32[200];
        public UInt32 Crc;

        public bool IsValid;

        /// <summary>String indicating which set and slice the partition data associated with this header belongs to. 
        /// On undamaged media, each partition header occurs at least twice, and this identity is used to only process the associated data once.</summary>
        public string PartitionIdentity
        {
            get
            {
                return String.Format("{0}:{1}", SetIdentifier, SliceSequence);
            }
        }

        public static int HeaderLength = 890;

        public static MediaPartitionHeader TryRead(Stream fs, long offset)
        {
            byte[] headerData = new byte[HeaderLength];

            try
            {
                var header = new MediaPartitionHeader();
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(headerData, 0, HeaderLength);

                byte[] guidBytes = new byte[16];
                Array.ConstrainedCopy(headerData, 32, guidBytes, 0, 16);
                header.SetIdentifier = new Guid(guidBytes);

                header.SliceSequence = BitConverter.ToUInt32(headerData, 48);
                header.SliceDataOffset = BitConverter.ToUInt32(headerData, 52);
                header.Sequence = BitConverter.ToUInt32(headerData, 56);
                header.DataOffset = BitConverter.ToInt64(headerData, 60);
                header.DataLength = BitConverter.ToUInt32(headerData, 68);
                header.CodingOffset = BitConverter.ToInt64(headerData, 72);
                header.CodingLength = BitConverter.ToUInt32(headerData, 80);
                header.CodingChunks = headerData[84];
                header.CodingWordSize = headerData[85];

                for (int i = 0; i < header.ChunkCrc32.Length; i++)
                {
                    header.ChunkCrc32[i] = BitConverter.ToUInt32(headerData, 86 + (i * 4));
                }
                header.Crc = BitConverter.ToUInt32(headerData, HeaderLength - 4);

                header.IsValid = headerData.Take(32).SequenceEqual(Signature) && (header.Crc == Crc32C.Crc32CAlgorithm.Compute(headerData, 0, HeaderLength - 4));

                return header;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Unexpected error reading partition header: {0}", ex.Message);
                Console.WriteLine();
                return null;
            }
        }
    }
}
