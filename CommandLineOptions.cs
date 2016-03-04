using CommandLine;

namespace ArchiveTool
{
    class CommandLineOptions
    {
        [Option('t', "type", Required = true, HelpText = "The type of input file: Media, Slice, Archive or SmallFileBundle")]
        public string ObjectType { get; set; }

        [Option('i', "infile", Required = true, HelpText = "The input file to process: media file (e.g. ISO) or (partial) archive set, depending on selected operation. May contain wildcards.")]
        public string InputFile { get; set; }

        [Option('x', "extract", DefaultValue = false, HelpText = "Extract any source files or archive set parts found")]
        public bool DoExtract { get; set; }

        [Option('r', "repair", DefaultValue = false, HelpText = "Attempt to repair any damaged section of media files or archive sets")]
        public bool DoRepair { get; set; }

        [Option('v', "verbose", DefaultValue = false, HelpText = "Display technical details while processing")]
        public bool Verbose { get; set; }

        [Option('o', "outpath", HelpText = "Output path to save any extracted files. If not specified, the current working directory will be used.")]
        public string OutputPath { get; set; }

        [Option('k', "keyfile", HelpText = "File containing a (certificate with a) private key (PEM or PKCS#12/PFX format) authorized for the archive set")]
        public string KeyFile { get; set; }

        [Option('e', "enumerate", DefaultValue = false, HelpText = "Enumerate top-level folders when looking for input file(s)")]
        public bool Enumerate { get; set; }
    }
}
