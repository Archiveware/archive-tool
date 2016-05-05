using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class ArchiveFileExtentHeader
    {
        public static byte[] Signature = ASCIIEncoding.ASCII.GetBytes("!!!!!!!!");
        public UInt32 ExtentSequence;
        public UInt32 IsCopyOfExtentSequence;
        public UInt64 ExtentOffset;
        public UInt32 ExtentLength;
        public UInt64 TotalLength;
        public DateTime Created;
        public DateTime LastModified;
        public byte[] DataHash = new byte[48];
        public byte[] IV = new byte[16];
        public UInt16 KeyIndex;
        public UInt32 CompressedDataCrc;
        public UInt32 CompressedDataLength;
        public UInt32 EncryptedDataLength;
        public UInt32 EncryptedDataCrc;
        public UInt16 FullNameLength;
        public byte[] FullNameUtf8Bytes;
        public UInt32 Crc;

        public bool IsValid;
        public string FullName;
        public UInt16 HeaderLength;

        public static ArchiveFileExtentHeader TryRead(Stream fs, long offset)
        {
            byte[] headerData = new byte[65792];

            try
            {
                var header = new ArchiveFileExtentHeader();
                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(headerData, 0, headerData.Length);

                header.ExtentSequence = BitConverter.ToUInt32(headerData, 8);
                header.IsCopyOfExtentSequence = BitConverter.ToUInt32(headerData, 12);
                header.ExtentOffset = BitConverter.ToUInt64(headerData, 16);
                header.ExtentLength = BitConverter.ToUInt32(headerData, 24);
                header.TotalLength = BitConverter.ToUInt64(headerData, 28);
                header.Created = new DateTime(BitConverter.ToInt64(headerData, 36));
                header.LastModified = new DateTime(BitConverter.ToInt64(headerData, 44));
                Array.ConstrainedCopy(headerData, 52, header.DataHash, 0, 48);
                Array.ConstrainedCopy(headerData, 100, header.IV, 0, 16);
                header.KeyIndex = BitConverter.ToUInt16(headerData, 116);
                header.CompressedDataCrc = BitConverter.ToUInt32(headerData, 118);
                header.CompressedDataLength = BitConverter.ToUInt32(headerData, 122);
                header.EncryptedDataLength = BitConverter.ToUInt32(headerData, 126);
                header.EncryptedDataCrc = BitConverter.ToUInt32(headerData, 130);
                header.FullNameLength = BitConverter.ToUInt16(headerData, 134);
                header.FullNameUtf8Bytes = new byte[header.FullNameLength];
                Array.ConstrainedCopy(headerData, 136, header.FullNameUtf8Bytes, 0, header.FullNameLength);
                header.Crc = BitConverter.ToUInt32(headerData, 136 + header.FullNameLength);

                header.IsValid = headerData.Take(8).SequenceEqual(Signature) && (header.Crc == Crc32CWrapper.ComputeCrc32C(headerData, 0, 136 + header.FullNameLength));
                header.FullName = UTF8Encoding.UTF8.GetString(header.FullNameUtf8Bytes);
                header.HeaderLength = (UInt16)(140 + header.FullNameLength + 1);

                return header;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Unexpected error reading archive file extent header: {0}", ex.Message);
                Console.WriteLine();
                return null;
            }
        }
    }
}
