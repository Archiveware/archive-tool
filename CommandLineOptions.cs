using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiveTool
{
    class CommandLineOptions
    {
        [Option('o', "object", Required = true, HelpText = "The type of input file: Media, Slice or Archive")]
        public string ObjectType { get; set; }

        [Option('i', "infile", Required = true, HelpText = "The input file to process: media file (e.g. ISO) or (partial) archive set, depending on selected operation")]
        public string InputFile { get; set; }

        [Option('x', "extract", DefaultValue = false, HelpText = "Extract any source files or archive set parts found")]
        public bool DoExtract { get; set; }

        [Option('r', "repair", DefaultValue = false, HelpText = "Attempt to repair any damaged section of media files or archive sets")]
        public bool DoRepair { get; set; }

        [Option('v', "verbose", DefaultValue = false, HelpText = "Display technical details while processing")]
        public bool Verbose { get; set; }

        [Option('p', "outpath", HelpText = "Output path to save any extracted files")]
        public string OutputPath { get; set; }

        [Option('c', "keyfile", HelpText = "File containing all archive set keys (required to extract source files)")]
        public string KeyFile { get; set; }
    }
}
