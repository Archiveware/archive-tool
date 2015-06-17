using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class ArchiveSetProcessor
    {
        public static void Scan(string inFile, string keyFile, string outPath, bool extract, bool verbose)
        {
            try
            {
                if (extract && !Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);

                using (var fs = new FileStream(inFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Console.WriteLine("Scanning {0} for archive file headers", inFile);
                    byte[] buffer = new byte[65536];

                    for (long offset = 0; offset < fs.Length - 32; offset += (buffer.Length - 32))
                    {
                        fs.Seek(offset, SeekOrigin.Begin);
                        fs.Read(buffer, 0, buffer.Length);

                        for (int j = 0; j < buffer.Length - 32; j++)
                        {
                            uint dataLength;
                            if (SignatureFound(buffer, j) && ProcessFileExtent(fs, offset + j, extract, outPath, verbose, out dataLength))
                            {
                                offset += dataLength + j - (buffer.Length - 32);
                                break;
                            }
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

        private static bool ProcessFileExtent(Stream fs, long offset, bool extract, string outPath, bool verbose, out uint dataLength)
        {
            byte[] key;
            byte[] plaintextExtent;

            var header = ArchiveFileExtentHeader.TryRead(fs, offset);
            if (header != null)
            {
                if (verbose)
                    Console.WriteLine("  @{0,-16} {1} ({2}-{3}/{4}) valid: {5}", offset, header.FullName, header.ExtentOffset, header.ExtentLength, header.TotalLength, header.IsValid);

                if (header.IsValid)
                {
                    fs.Seek(offset + header.HeaderLength, SeekOrigin.Begin);
                    byte[] encryptedExtent = new byte[header.EncryptedDataLength];
                    fs.Read(encryptedExtent, 0, encryptedExtent.Length);

                    if (header.EncryptedDataCrc != Crc32C.Crc32CAlgorithm.Compute(encryptedExtent, 0, encryptedExtent.Length))
                        Console.WriteLine("  Skipping {0} ({1}-{2}/{3}) due to encrypted data CRC mismatch!", header.FullName, header.ExtentOffset, header.ExtentLength, header.TotalLength);
                    else
                    {
                        if (!ArchiveSetKeys.GetKeyByIndex(header.KeyIndex, out key))
                            Console.WriteLine("  Skipping {0} ({1}-{2}/{3}) due to missing encryption key!", header.FullName, header.ExtentOffset, header.ExtentLength, header.TotalLength);
                        else
                        {
                            using (var csp = new System.Security.Cryptography.AesCryptoServiceProvider() { KeySize = 256, Key = key, IV = header.IV, Padding = System.Security.Cryptography.PaddingMode.None })
                            using (var decryptor = csp.CreateDecryptor())
                                plaintextExtent = decryptor.TransformFinalBlock(encryptedExtent, 0, encryptedExtent.Length);

                            byte[] decompressedExtent = new byte[header.UncompressedDataLength];
                            var inHandle = GCHandle.Alloc(plaintextExtent, GCHandleType.Pinned);
                            var outHandle = GCHandle.Alloc(decompressedExtent, GCHandleType.Pinned);

                            int byteCount = NativeCode.Decompress(inHandle.AddrOfPinnedObject(), outHandle.AddrOfPinnedObject(), (int)header.CompressedDataLength, (int)header.UncompressedDataLength);
                            outHandle.Free();
                            inHandle.Free();

                            if (byteCount != header.UncompressedDataLength)
                                Console.WriteLine("  Skipping {0} ({1}-{2}/{3}) due to unexpected decompression failure: {4}", header.FullName, header.ExtentOffset, header.ExtentLength, header.TotalLength, byteCount);
                            else
                            {
                                if (header.FullName.StartsWith("//EncryptedKeys:"))
                                    ArchiveSetKeys.AddFromEncryptedBlob(decompressedExtent);
                                else if(extract)
                                {

                                }

                                if (!verbose)
                                    Console.Write(".");
                            }
                        }
                    }

                    dataLength = header.HeaderLength + header.EncryptedDataLength;
                    return true;
                }
            }
            dataLength = 0;
            return false;
        }

        /// <summary>Helper function for <see cref="Scan"/>: returns True if the first 8 bytes at the specified offset match the file extent header signature.</summary>
        private static bool SignatureFound(byte[] buffer, int offset)
        {
            for (int i = 0; i < 8; i++)
            {
                if (buffer[offset + i] != ArchiveFileExtentHeader.Signature[i])
                    return false;
            }
            return true;
        }

    }
}
