using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class SmallFileBundleProcessor
    {
        /// <summary>Scan, and optionally extract file from, a small file bundle. 
        /// Since small file bundles are stored as regular archive files, they are guaranteed to be undamaged; thus, error detection/correction is not required.</summary>
        public static void Scan(string inFile, string outPath, bool extract, bool verbose)
        {
            var sfe = new SmallFileEntry(verbose);

            if (verbose)
                Console.WriteLine("Scanning{0} small file bundle {1}", extract ? "" : "/extracting", inFile);

            using (var fs = new FileStream(inFile, FileMode.Open, FileAccess.Read))
            {
                while (fs.Position < fs.Length && sfe.TryRead(fs))
                    if (extract)
                        sfe.Save(outPath);
            }
        }
    }
}
