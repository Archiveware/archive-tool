using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class ArchiveSetKeys
    {
        public static bool GetKeyByIndex(int index, out byte[] key)
        {
            if (index == 0)
            {
                key = new byte[16];
                return true;
            }
            key = null;
            return false;
        }

        internal static void AddFromEncryptedBlob(byte[] blob)
        {
            throw new NotImplementedException();
        }
    }
}
