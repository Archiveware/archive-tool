using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ArchiveTool
{
    class ArchiveSetProcessor
    {
        static ArchiveSetKeys Keys = new ArchiveSetKeys();
        static ArchiveFileExtentCopies CopiedExtents = new ArchiveFileExtentCopies();

        public static void Scan(string inFile, KeyParser explicitKey, string outPath, bool extract, bool verbose)
        {
            try
            {
                if (extract && !Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);

                Keys.ExplicitKey = explicitKey;

                using (var fs = new FileStream(inFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

                if (extract)
                {
                    CopiedExtents.WriteToDestination();

                    var di = new DirectoryInfo(Path.Combine(outPath, "SmallFileBundles"));
                    if (di.Exists && di.GetFiles().Any())
                    {
                        foreach (var smallFileBundle in di.GetFiles())
                            SmallFileBundleProcessor.Scan(smallFileBundle.FullName, Path.Combine(outPath, "ArchiveFiles"), extract, verbose);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal error: {0}", ex.Message);
                Environment.Exit(2);
            }
        }

        internal static bool ProcessFileExtent(Stream fs, long offset, bool extract, string outPath, bool verbose, out uint dataLength)
        {
            byte[] key;
            byte[] plaintextExtent;

            var header = ArchiveFileExtentHeader.TryRead(fs, offset);
            if (header != null)
            {
                if (verbose)
                    Console.WriteLine("  @{0,-16} {1} ({2}-{3}/{4}) valid: {5}", offset, header.FullName, header.ExtentOffset, header.ExtentOffset + header.ExtentLength, header.TotalLength, header.IsValid);

                if (header.IsValid)
                {
                    fs.Seek(offset + header.HeaderLength, SeekOrigin.Begin);
                    byte[] encryptedExtent = new byte[header.EncryptedDataLength];
                    fs.Read(encryptedExtent, 0, encryptedExtent.Length);

                    try
                    {
                        if (header.EncryptedDataCrc != Crc32CWrapper.ComputeCrc32C(encryptedExtent, 0, encryptedExtent.Length))
                            throw new ArchiveFileException("encrypted data CRC mismatch");

                        if (!Keys.GetKeyByIndex(header.KeyIndex, out key))
                            throw new ArchiveFileException("missing encryption key");

                        using (var csp = new System.Security.Cryptography.AesCryptoServiceProvider() { Key = key, IV = header.IV, Padding = PaddingMode.None })
                        {
                            using (var decryptor = csp.CreateDecryptor())
                                try
                                {
                                    plaintextExtent = decryptor.TransformFinalBlock(encryptedExtent, 0, encryptedExtent.Length);
                                }
                                catch (Exception ex)
                                {
                                    throw new ArchiveFileException("unexpected decryption failure: {0}", ex.Message);
                                }

                            if (header.CompressedDataCrc != Crc32CWrapper.ComputeCrc32C(plaintextExtent, 0, (int)header.CompressedDataLength))
                                throw new ArchiveFileException("content CRC mismatch after decryption");
                        }

                        if (header.IsCopyOfExtentSequence != 0)
                            CopiedExtents.Add(header);
                        else
                        {
                            byte[] decompressedExtent = new byte[header.ExtentLength];
                            var inHandle = GCHandle.Alloc(plaintextExtent, GCHandleType.Pinned);
                            var outHandle = GCHandle.Alloc(decompressedExtent, GCHandleType.Pinned);

                            int byteCount = NativeMethods.Decompress(inHandle.AddrOfPinnedObject(), outHandle.AddrOfPinnedObject(), (int)header.CompressedDataLength, (int)header.ExtentLength);
                            outHandle.Free();
                            inHandle.Free();

                            if (byteCount != header.ExtentLength)
                                throw new ArchiveFileException("unexpected decompression failure: {0}", byteCount);

                            VerifyExtent(header, decompressedExtent);

                            if (header.FullName.StartsWith("//EncryptedKeys:"))
                                Keys.AddFromPkcs7Message(decompressedExtent);
                            else if (extract)
                            {
                                var filePath = Path.Combine(outPath, header.FullName.StartsWith("//SmallFileBundle") ? "SmallFileBundles" : "ArchiveFiles", header.FullName.Replace("//", ""));
                                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                                using (var outFileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                                {
                                    outFileStream.Seek((long)header.ExtentOffset, SeekOrigin.Begin);
                                    outFileStream.Write(decompressedExtent, 0, decompressedExtent.Length);
                                }

                                var info = new FileInfo(filePath);
                                info.CreationTime = header.Created;
                                info.LastWriteTime = header.LastModified;
                            }
                        }
                        if (!verbose)
                            Console.Write(".");
                    }
                    catch (ArchiveFileException ex)
                    {
                        Console.WriteLine("      Skipping {0} ({1}-{2}/{3}) due to {4}", header.FullName, header.ExtentOffset, header.ExtentLength, header.TotalLength, ex.ConsoleMessage);
                    }

                    dataLength = header.HeaderLength + header.EncryptedDataLength;
                    return true;
                }
            }
            dataLength = 0;
            return false;
        }

        /// <summary>Helper function for <see cref="Scan"/>: returns True if the first 8 bytes at the specified offset match the file extent header signature.</summary>
        internal static bool SignatureFound(byte[] buffer, int offset)
        {
            for (int i = 0; i < 8; i++)
            {
                if (buffer[offset + i] != ArchiveFileExtentHeader.Signature[i])
                    return false;
            }
            return true;
        }

        /// <summary>Helper function for <see cref="Scan"/>: to calculate the SHA-384 hash of a file extent.</summary>
        public static void VerifyExtent(ArchiveFileExtentHeader header, byte[] extent)
        {
            using (SHA384Cng cng = new SHA384Cng())
            {
                cng.TransformFinalBlock(extent, 0, extent.Length);

                if (!cng.Hash.SequenceEqual(header.DataHash))
                    throw new ArchiveFileException("SHA384 mismatch");
            }
        }

    }
}
