using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ArchiveTool
{
    class MainClass
    {
        internal delegate void WildcardExpanderDelegate(string singleFilePath);

        public static void Main(string[] args)
        {
            CommandLine.ParserResult<ArchiveTool.CommandLineOptions> result = null;

            try
            {
                result = CommandLine.Parser.Default.ParseArguments<CommandLineOptions>(args);
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid command line. Use --help to see valid options.");
                Environment.Exit(1);
            }

            if (Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture != ProcessorArchitecture.Amd64)
                Console.WriteLine("WARNING: This version of archive-tool was not created using the x64 build target, and will most likely run out of memory when extracting files!");

            try
            {
                int test = NativeMethods.Test(new byte[] { 0x78, 0x56, 0x34, 0x12 });
                if (test != 0x12345678)
                    throw new ApplicationException("endianness mismatch");
            }
            catch (Exception ex)
            {
                Console.WriteLine("WARNING: Error invoking native code library: {0}. This will cause most archive-tool functionality to fail!", ex.Message);
            }

            if (!result.Errors.Any())
            {
                string inputPath = result.Value.InputFile;
                if (!string.IsNullOrEmpty(inputPath) && !inputPath.Contains(Path.DirectorySeparatorChar))
                    inputPath = Path.Combine(Environment.CurrentDirectory, inputPath);

                if (MatchingFileCount(inputPath) == 0)
                {
                    Console.WriteLine("Specified input file(s) could not be found");
                    Environment.Exit(1);
                }

                string outputPath = result.Value.OutputPath;
                if (string.IsNullOrEmpty(outputPath))
                {
                    if (result.Value.DoExtract)
                        Console.WriteLine("Output path not specified: defaulting to current directory");
                    outputPath = Environment.CurrentDirectory;
                }

                if (!outputPath.Contains(Path.DirectorySeparatorChar))
                    outputPath = Path.Combine(Environment.CurrentDirectory, outputPath);

                KeyParser explicitKey = null;
                if (!string.IsNullOrEmpty(result.Value.KeyFile))
                    explicitKey = new KeyParser(result.Value.KeyFile);

                if (result.Value.ObjectType.ToLower().StartsWith("media"))
                    WildcardExpander(inputPath, file => MediaProcessor.Scan(file, outputPath, result.Value.DoRepair, result.Value.DoExtract, result.Value.Verbose));
                else if (result.Value.ObjectType.ToLower().StartsWith("slice"))
                    WildcardExpander(inputPath, file => ArchiveSliceProcessor.Process(file, outputPath, result.Value.DoRepair, result.Value.DoExtract, result.Value.Verbose));
                else if (result.Value.ObjectType.ToLower().StartsWith("archive"))
                    WildcardExpander(inputPath, file => ArchiveSetProcessor.Scan(file, explicitKey, outputPath, result.Value.DoExtract, result.Value.Verbose));
                else if (result.Value.ObjectType.ToLower().StartsWith("small"))
                    WildcardExpander(inputPath, file => SmallFileBundleProcessor.Scan(file, outputPath, result.Value.DoExtract, result.Value.Verbose));
                else
                {
                    Console.WriteLine("Invalid object type: should be Media, Slice, Archive or SmallFileBundle");
                    Environment.Exit(1);
                }
            }
        }

        internal static void WildcardExpander(string path, WildcardExpanderDelegate action)
        {
            var di = new DirectoryInfo(Path.GetDirectoryName(path));
            foreach (var file in di.GetFiles(Path.GetFileName(path)))
                action.Invoke(file.FullName);
        }

        internal static int MatchingFileCount(string path)
        {
            var di = new DirectoryInfo(Path.GetDirectoryName(path));
            return di.GetFiles(Path.GetFileName(path)).Count();
        }
    }
}
