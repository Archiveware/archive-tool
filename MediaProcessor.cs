using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace ArchiveTool
{
    class MediaProcessor
    {
        public static void Scan(string inFile, string outPath, bool repair, bool extract, bool verbose)
        {

            try
            {
                if (extract && !Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);

                using (var fs = new FileStream(inFile, FileMode.Open))
                {
                    Console.WriteLine("Scanning {0} for media partition headers", inFile);
                    var headers = HeaderScan(fs, verbose);

                    Console.WriteLine();
                    Console.WriteLine("Validating data associated with {0} headers", headers.Count);
                    var validPartitions = new Dictionary<String, MediaPartitionHeader>();
                    foreach (var header in headers)
                    {
                        if (!validPartitions.ContainsKey(header.PartitionIdentity))
                        {
                            if (MediaPartition.Process(fs, header, repair, extract, outPath, verbose))
                                validPartitions.Add(header.PartitionIdentity, header);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error: {0}", ex.Message);
                Environment.Exit(2);
            }
        }

        /// <summary>Scan the specified media file stream for partition headers by looking for their 32-byte signature.
        /// Trivial algorithm used is quite fast for undamaged media, still acceptable for media with missing sections.</summary>
        private static List<MediaPartitionHeader> HeaderScan(Stream fs, bool verbose)
        {
            var headers = new List<MediaPartitionHeader>();
            byte[] buffer = new byte[65536];

            for (long offset = 0; offset < fs.Length - 32; offset += (buffer.Length - 32))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(buffer, 0, buffer.Length);

                for (int j = 0; j < buffer.Length - 32; j++)
                {
                    if (SignatureFound(buffer, j))
                    {
                        var header = MediaPartitionHeader.TryRead(fs, offset + j);
                        if (header != null)
                        {
                            if (verbose)
                                Console.WriteLine("  @{0,-16} set: {1}    seq: {2,-8} slice: {3,-8} valid: {4}", offset + j, header.SetIdentifier, header.Sequence, header.SliceSequence, header.IsValid);
                            else
                                Console.Write(header.IsValid ? "." : "?");

                            headers.Add(header);

                            if (header.IsValid)
                                offset += (offset > header.DataOffset ? header.CodingLength : header.DataLength) + MediaPartitionHeader.HeaderLength - buffer.Length + 32;
                        }
                    }
                }
            }
            return headers;
        }

        /// <summary>Helper function for <see cref="List"/>: returns True if the first 32 bytes at the specified offset match the media partition header signature.</summary>
        private static bool SignatureFound(byte[] buffer, int offset)
        {
            for (int i = 0; i < 32; i++)
            {
                if (buffer[offset + i] != MediaPartitionHeader.Signature[i])
                    return false;
            }
            return true;
        }

    }
}
