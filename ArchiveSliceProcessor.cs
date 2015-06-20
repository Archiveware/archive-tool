using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class ArchiveSliceProcessor
    {
        public static void Process(string inFile, string outPath, bool repair, bool extract, bool verbose)
        {
            Console.WriteLine("Processing{0}{1} archive slice {2}", repair ? " / Repairing" : "", extract ? " / Extracting" : "", inFile);

            try
            {
                int dataChunkCount, codingChunkCount, codingWordSize;
                var missingChunks = MissingSliceChunks(inFile, out dataChunkCount, out codingChunkCount, out codingWordSize);

                if (verbose)
                    Console.WriteLine("  Data chunks: {0} Coding chunks: {1} Word size: {2} Missing: {3}", dataChunkCount, codingChunkCount, codingWordSize,
                        missingChunks.Count > 0 ? string.Join(",", missingChunks) : "none");

                if (dataChunkCount == 0 || (codingWordSize != 8 && codingWordSize != 16 && codingWordSize != 32))
                    throw new ApplicationException("Missing or invalid coding information: file is not a valid archive slice");

                if (missingChunks.Count > codingChunkCount)
                    throw new ApplicationException("Too many missing parts to be able to repair this archive slice");

                bool needsRepair = false;
                foreach (var missingChunk in missingChunks)
                    if (missingChunk <= dataChunkCount)
                        needsRepair = true;

                if(needsRepair)
                    if (!repair)
                        throw new ApplicationException("One or more missing data chunks; repair option must be enabled in order to continue");
                    else
                        if (!ReplaceMissingSliceChunks(inFile, dataChunkCount, codingChunkCount, codingWordSize, missingChunks, verbose))
                            throw new ApplicationException("Unexpected repair process failure");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error: {0}", ex.Message);
            }
        }

        internal static List<int> MissingSliceChunks(string inFile, out int dataChunkCount, out int codingChunkCount, out int codingWordSize)
        {
            var missingChunks = new List<int>();

            using (var fs = new FileStream(inFile, FileMode.Open))
            {
                fs.Seek(ArchiveSliceHeader.DataPartitionCountOffset, SeekOrigin.Begin);
                dataChunkCount = fs.ReadByte();
                codingChunkCount = fs.ReadByte();
                codingWordSize = fs.ReadByte();

                if (dataChunkCount > 0 && codingChunkCount > 0)
                {
                    int chunkSize = (int)(fs.Length / (dataChunkCount + codingChunkCount));
                    int index = 1;
                    for (long offset = 0; offset < fs.Length; offset += chunkSize)
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        byte[] buffer = new byte[42];
                        fs.Read(buffer, 0, buffer.Length);
                        if (buffer.All(b => b == 0))
                            missingChunks.Add(index);
                        index++;
                    }
                }
            }

            return missingChunks;
        }

        internal static bool ReplaceMissingSliceChunks(string inFile, int dataChunkCount, int codingChunkCount, int codingWordSize, List<int> erasures, bool verbose)
        {
            using (var fs = new FileStream(inFile, FileMode.Open, FileAccess.ReadWrite))
            {
                int chunkSize = (int)(fs.Length / (dataChunkCount + codingChunkCount));
                byte[] data = new byte[chunkSize * dataChunkCount];
                byte[] coding = new byte[chunkSize * codingChunkCount];

                if (verbose)
                    Console.Write("  Initiating repair, chunk size: {0}... ", chunkSize);

                fs.Read(data, 0, data.Length);
                fs.Read(coding, 0, coding.Length);

                var dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var codingHandle = GCHandle.Alloc(coding, GCHandleType.Pinned);

                erasures.Add(-1); //End of erasure list
                int result = NativeCode.Decode(dataChunkCount, codingChunkCount, codingWordSize, dataHandle.AddrOfPinnedObject(), codingHandle.AddrOfPinnedObject(), chunkSize, erasures.ToArray());

                codingHandle.Free();
                dataHandle.Free();

                if (result == 0)
                {
                    if (verbose)
                        Console.WriteLine("OK: writing repaired data");
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(data, 0, data.Length);
                    fs.Write(coding, 0, coding.Length);
                    return true;
                }
                else
                {
                    if(verbose)
                        Console.WriteLine("FAILED: {0}", result);
                    return false;
                }
            }
        }
    }
}
