using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class Crc32CWrapper
    {
        static bool UseNativeCode;

        static Crc32CWrapper()
        {
            try
            {
                byte[] buffer = new byte[1];
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                NativeMethods.Crc32C(0, handle.AddrOfPinnedObject(), 1);
                handle.Free();
                UseNativeCode = true;
            }
            catch (EntryPointNotFoundException)
            {
                //Not compiled into native library: this happens on Windows and is OK, as that's the platform where we use the mixed-mode Crc32C.NET assembly
            }
        }

        public static uint ComputeCrc32C(byte[] data, int offset, int length)
        {
            if (UseNativeCode)
            {
                var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var crc = NativeMethods.Crc32C(0, new IntPtr((long)handle.AddrOfPinnedObject() + offset), length);
                handle.Free();
                return crc;
            }
            else
                return Crc32C.Crc32CAlgorithm.Compute(data, offset, length);
        }

    }
}
