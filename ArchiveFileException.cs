using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    [Serializable]
    class ArchiveFileException : Exception
    {
        public string ConsoleMessage;
    
        /// <summary>This exception is thrown to indicate a problem processing a file for which a valid header has been found and that this file has been lost</summary>
        public ArchiveFileException(string message, params object[] args)
        {
            ConsoleMessage = string.Format(message, args);
        }
    }
}
