﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class NativeMethods
    {
        [DllImport("NativeCode.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Decode(int k, int m, int w, IntPtr data, IntPtr coding, int blockSize, int[] erasures);

        [DllImport("NativeCode.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Decompress(IntPtr source, IntPtr destination, int compressedSize, int maxDecompressedSize);

        [DllImport("NativeCode.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Test(byte[] testBytes);

        [DllImport("NativeCode.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "crc32c")]
        public static extern uint Crc32C(uint crc, IntPtr data, int length);
    }
}
