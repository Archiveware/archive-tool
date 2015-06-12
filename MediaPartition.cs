﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                if (verbose)
                    Console.WriteLine("Slice #{0} is irrecoverably damaged: {1} bad chunks, exceeding maximum {2}", header.SliceSequence, badDataChunks.Count + badCodingChunks.Count, header.CodingChunks);
                else
                    Console.Write("X");
                return false;
            }

            if (badDataChunks.Any() && repair)
            {
                if (verbose)
                    Console.Write("Repairing {0} data chunks... ", badDataChunks.Count());

                var erasures = badDataChunks.ToList();
                foreach (var badCodingChunkIndex in badCodingChunks)
                    erasures.Add(100 + badCodingChunkIndex);
                erasures.Add(-1); //End-of-list marker

                var dataHandle = GCHandle.Alloc(data.Buffer, GCHandleType.Pinned);
                var codingHandle = GCHandle.Alloc(coding.Buffer, GCHandleType.Pinned);

                int result = NativeCode.Decode(100, header.CodingChunks, header.CodingWordSize, dataHandle.AddrOfPinnedObject(), codingHandle.AddrOfPinnedObject(),
                                                (int)(header.DataLength / 100), erasures.ToArray());

                if (verbose)
                    Console.WriteLine(result == 0 ? "OK" : string.Format("FAILED ({0})", result));
                else
                    Console.Write(result == 0 ? "r" : "x");

                codingHandle.Free();
                dataHandle.Free();
            }

            if (extract)
            {
                using (var fs2 = new FileStream(Path.Combine(outPath, string.Format("{0}-{1:0000000000}.slice", header.SetIdentifier, header.SliceSequence)), FileMode.OpenOrCreate))
                {
                    fs2.Seek(header.SliceDataOffset, SeekOrigin.Begin);
                    fs2.Write(data.Buffer, 0, (int)header.DataLength);
                }
            }

            Console.Write(".");

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

            int bufferOffset = 0;
            int readLength = length;
            //Negative offset may be encountered if the media stream is truncated. Attempt to read as many bytes as possible anyway
            if (offset < 0 && -offset < length)
            {
                bufferOffset = -(int)offset;
                readLength += (int)offset;
                offset = 0;
            }

            //If we can do a valid read, go ahead: otherwise, we'll skip this step (and return a zero-filled buffer)
            if (offset >= 0 && offset < fs.Length && readLength > 0)
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(result.Buffer, bufferOffset, readLength);

                int chunkLength = length / chunkCount;
                int chunkOffset = 0;
                for (int i = 0; i < chunkCount; i++)
                {
                    result.Crc[i] = Crc32C.Crc32CAlgorithm.Compute(result.Buffer, chunkOffset, chunkLength);
                    chunkOffset += chunkLength;
                }
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