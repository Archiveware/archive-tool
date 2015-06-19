using System;
using System.Linq;

namespace ArchiveTool
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var result = CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args);
            if (!result.Errors.Any())
            {
                if (result.Value.ObjectType.ToLower().StartsWith("media"))
                    MediaProcessor.Scan(result.Value.InputFile, result.Value.OutputPath, result.Value.DoRepair, result.Value.DoExtract, result.Value.Verbose);
                else if (result.Value.ObjectType.ToLower().StartsWith("slice"))
                    ArchiveSliceProcessor.Scan(result.Value.InputFile, result.Value.OutputPath, result.Value.DoRepair, result.Value.DoExtract);
                else if (result.Value.ObjectType.ToLower().StartsWith("archive"))
                    ArchiveSetProcessor.Scan(result.Value.InputFile, result.Value.KeyFile, result.Value.OutputPath, result.Value.DoExtract, result.Value.Verbose);
                else if (result.Value.ObjectType.ToLower().StartsWith("small"))
                    SmallFileBundleProcessor.Scan(result.Value.InputFile, result.Value.OutputPath, result.Value.DoExtract, result.Value.Verbose);
                else
                {
                    Console.WriteLine("Invalid object type: should be Media, Slice or Archive");
                    Environment.Exit(1);
                }
            }
        }

    }
}
