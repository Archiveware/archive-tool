using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class MediaPartition
    {
        public static bool Process(Stream fs, MediaPartitionHeader header, bool repair, bool extract, string outPath, bool verbose)
        {
            var data = Read(fs, header.DataOffset, (int)header.DataLength, 100);
            var coding = Read(fs, header.CodingOffset, (int)header.CodingLength, header.CodingChunks);

            var badDataChunks = CrcMismatches(header.ChunkCrc32.Take(100).ToArray(), data.Crc);
            var badCodingChunks = CrcMismatches(header.ChunkCrc32.Skip(100).Take(header.CodingChunks).ToArray(), coding.Crc);

            if (badDataChunks.Count + badCodingChunks.Count > header.CodingChunks)
            {
                Console.WriteLine("{0}Slice #{1} is irrecoverably damaged: {2} bad chunks, exceeding maximum {3}{0}", verbose ? "" : "\r\n", header.SliceSequence, badDataChunks.Count + badCodingChunks.Count, header.CodingChunks);
                return false;
            }

            if (extract)
            {
                using (var fs2 = new FileStream(Path.Combine(outPath, string.Format("{0}-{1:0000000000}.slice", header.SetIdentifier, header.SliceSequence)), FileMode.OpenOrCreate))
                {
                    fs2.Seek(header.SliceDataOffset, SeekOrigin.Begin);
                    fs2.Write(data.Buffer, 0, (int)header.DataLength);
                }
            }

            return true;
        }

        class ReadResultWrapper
        {
            public UInt32[] Crc;
            public byte[] Buffer;

            public ReadResultWrapper(int crcCount, int bufferLength)
            {
                Crc = new UInt32[crcCount];
                Buffer = new byte[bufferLength];
            }
        }

        private static ReadResultWrapper Read(Stream fs, long offset, int length, int chunkCount)
        {
            var result = new ReadResultWrapper(chunkCount, length);

            fs.Seek(offset, SeekOrigin.Begin);
            fs.Read(result.Buffer, 0, length);

            int chunkLength = length / chunkCount;
            int chunkOffset = 0;
            for (int i = 0; i < chunkCount; i++)
            {
                result.Crc[i] = Crc32C.Crc32CAlgorithm.Compute(result.Buffer, chunkOffset, chunkLength);
                chunkOffset += chunkLength;
            }

            return result;
        }

        private static List<int> CrcMismatches(UInt32[] headerValues, UInt32[] calculatedValues)
        {
            var mismatches = new List<int>();

            for (int i = 0; i < headerValues.Length; i++)
            {
                if (headerValues[i] != calculatedValues[i])
                    mismatches.Add(i);
            }

            return mismatches;
        }
    }
}
