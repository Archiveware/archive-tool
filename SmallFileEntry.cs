using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class SmallFileEntry
    {
        public UInt32 DataLength;
        public DateTime Created;
        public DateTime LastModified;
        public UInt32 DataCrc;
        public UInt16 FullNameLength;
        public byte[] FullNameUtf8Bytes;
        public byte[] Data;

        public string FullName;
        private bool Verbose;

        public SmallFileEntry(bool verbose)
        {
            Verbose = verbose;
        }

        public bool TryRead(Stream fs)
        {
            try
            {
                byte[] buffer = new byte[33000000];
                long initialStreamPosition = fs.Position;
                fs.Read(buffer, 0, buffer.Length);

                DataLength = BitConverter.ToUInt32(buffer, 0);
                Created = new DateTime(BitConverter.ToInt64(buffer, 4));
                LastModified = new DateTime(BitConverter.ToInt64(buffer, 12));
                DataCrc = BitConverter.ToUInt32(buffer, 20);
                FullNameLength = BitConverter.ToUInt16(buffer, 24);
                FullNameUtf8Bytes = new byte[FullNameLength];
                Array.ConstrainedCopy(buffer, 26, FullNameUtf8Bytes, 0, FullNameLength);
                FullName = UTF8Encoding.UTF8.GetString(FullNameUtf8Bytes);
                Data = new byte[DataLength];
                Array.ConstrainedCopy(buffer, 26 + FullNameLength, Data, 0, (int)DataLength);

                if (Verbose)
                    Console.WriteLine("  @{0,-16} {1} ({2})", initialStreamPosition, FullName, DataLength);
                else
                    Console.Write(".");

                fs.Seek(initialStreamPosition + (26 + FullNameLength + DataLength + 1), SeekOrigin.Begin);

                if (Crc32CWrapper.ComputeCrc32C(Data, 0, Data.Length) != DataCrc)
                    throw new ApplicationException("CRC mismatch");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("    Unexpected error reading small file entry: {0}", ex.Message);
                return false;
            }
        }

        public void Save(string path)
        {
            var filePath = Path.Combine(path, FullName);

            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (Stream fs = new FileStream(filePath, FileMode.Create))
                fs.Write(Data, 0, Data.Length);

            var info = new FileInfo(filePath);
            info.CreationTime = Created;
            info.LastWriteTime = LastModified;
        }
    }
}
