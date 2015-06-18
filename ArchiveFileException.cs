using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class ArchiveFileException : Exception
    {
        public string ConsoleMessage;

        public ArchiveFileException(string message, params object[] args)
        {
            ConsoleMessage = string.Format(message, args);
        }
    }
}
