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
        public byte SliceDataChunks;
        public byte SliceCodingChunks;
        public byte SliceCodingWordSize;
        public uint Sequence;
        private long HeaderOffset;
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

        public static int HeaderLength = 901;

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
                header.SliceDataChunks = headerData[56];
                header.SliceCodingChunks = headerData[57];
                header.SliceCodingWordSize = headerData[58];
                header.Sequence = BitConverter.ToUInt32(headerData, 59);
                header.HeaderOffset = BitConverter.ToInt64(headerData, 63);
                header.DataOffset = BitConverter.ToInt64(headerData, 71);
                header.DataLength = BitConverter.ToUInt32(headerData, 79);
                header.CodingOffset = BitConverter.ToInt64(headerData, 83);
                header.CodingLength = BitConverter.ToUInt32(headerData, 91);
                header.CodingChunks = headerData[95];
                header.CodingWordSize = headerData[96];

                for (int i = 0; i < header.ChunkCrc32.Length; i++)
                {
                    header.ChunkCrc32[i] = BitConverter.ToUInt32(headerData, 97 + (i * 4));
                }
                header.Crc = BitConverter.ToUInt32(headerData, HeaderLength - 4);

                header.IsValid = headerData.Take(32).SequenceEqual(Signature) && (header.Crc == Crc32C.Crc32CAlgorithm.Compute(headerData, 0, HeaderLength - 4));

                //If header is not at original location, adjust data offsets accordingly (media may be truncated...)
                header.DataOffset += (offset - header.HeaderOffset); 
                header.CodingOffset += (offset - header.HeaderOffset);
                
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
