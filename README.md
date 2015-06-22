archive-tool
============

`archive-tool` analyzes and (optionally) extracts files from Archiveware various version 3 containers. Damaged containers are repaired if requested and possible; in case of missing or broken containers, valid file contents are recovered to the maximum extent possible. 

Note that `archive-tool` is *not* intended for everyday use in production environments: it's minimal open source example code, documenting the various Archiveware file formats and enabling third parties to easily implement their own recovery solutions.

Installation
------------

Requirements/Dependencies:
* A 64-bit OS: due to the use of large file buffers (up to 2 GB), this tool will not work on systems with a small address space;
* The Microsoft .NET Framework, version 4.5 or higher on Windows, OS X or Linux. Either the official Microsoft CLR or Mono should work;
* Three packages from the [NuGet Gallery](https://www.nuget.org/). These packages are included with the binary distribution found in `Archiveware.Readme.ZIP` on most archive media created with Archiveware, and will be automatically downloaded (by Visual Studio or MonoDevelop) when building from source.

### Building from source

Using either Visual Studio or MonoDevelop is highly recommended: simply opening `archive-tool.sln` should download the NuGet-based dependencies and allow you to build the managed code.

When using MonoDevelop, or a version of Visual Studio which does not include C++ support, you will need to build the native code library separately. Any C compiler, including `gcc` should work for this: see the `Makefile` included in the `NativeCode` directory for details.   

When all files all built, put the binaries anywhere in your PATH for easy access.	

Usage
-----

### Obtaining media images

Copy the Archiveware container files from the Blu-ray disks, tapes or Petablock disks you want to restore to a temporary folder. *If read errors occur, copy as much data as possible, zeroing out truly unreadable parts of files*.

For testing purposes, you can [download](https://global-disk.com/downloads/Archiveware/TestData.ZIP) a 581 MB ZIP file containing 5 example ISO images: these images are used in the examples below.

### Extracting slices from the media images

During the archiving process, Archiveware archive sets are first divided into *slices*, with a maximum size of 2 GB each. Each slice is evenly distributed over all media, in data structures called *partitions* which include a user-defined percentage of redundant data to allow recovery in case of media defects.

By repeatedly invoking `archive-tool` on each media image (which can be automated by specifying a wildcard), all slices that make up the archive set can be exported. For this test, let's skip image #1 and start with the second image:

~~~
$ archive-tool.exe -t media -i TestData-0002.iso -x
Scanning TestData-0002.iso for media partition headers
..............
Validating / Extracting data associated with 14 headers
.......
~~~

You'll now have 7 `.slice` files in your working folder, each containing the second partition of that slice, plus a lot of empty space at the start of the file, which is where the first partition belongs. Since each partition header is stored twice, the tool reports 14 headers in total. Since this media image is complete and entirely undamaged, only the first 7 headers are processed when extracting the data.

Before extracting media image #3, let's introduce some damage to see how that works:

~~~ csharp
using System;
using System.IO;

namespace IsoDamager
{
    class Program
    {
        static void Main(string[] args)
        {
            DamageMedia(args[0]);
        }

        static void DamageMedia(string path)
        {
            var rnd = new Random();
            var buffer = new byte[2048];

            using (var fs = new FileStream(path, FileMode.Open))
            {
                for (int i = 1; i <= 32; i++)
                {
                    fs.Seek(Convert.ToInt64(rnd.NextDouble() * fs.Length), SeekOrigin.Begin);
                    fs.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
~~~

After running this against TestData-0003.iso (or simply stomping on the file a bit with your favorite hex editor: do note that the first 512KB or so are UDF data structures that `archive-tool` ignores), the export will initially fail with output similar to the following:

~~~
$ archive-tool.exe -t media -i TestData-0003.iso -x
Scanning TestData-0002.iso for media partition headers
..............
Validating / Extracting data associated with 14 headers
dddddddddddddd
~~~

This indicates that all data chunks have some damage, but this can still be repaired using the embedded redundant data: if the data were damaged beyond repair, the `d` would be replaced by an `X`, indicating data loss (if this happens to you during testing, simply extract the ISO from the example ZIP again). Running `archive-tool` with the `-v` or `--verbose` option will list the exact details in such situations.

To export all data with the necessary repairs, re-run `archive-tool` with the `-r` or `--repair` option: you should now see `r` indicators, i.e. successful repair.

To complete the slice extraction, run `archive-tool` against TestData-0004.iso and TestData-0005.iso as well (but not against TestData-0001.iso: that one is excluded on purpose to test the slice repair functionality)

### Extracting data from slices to the archive set

Once you have a collection of slice files, you can combine these into an archive set. If all data is undamaged, this is a rather simple copy operation, but in case of incomplete slices (as will be the case if you didn't extract TestData-0001.iso in the previous step), repairs will be required first:

~~~
$ archive-tool.exe -t slice -i *.slice -r -x
Processing / Repairing / Extracting archive slice c0fddea4-d1f1-4c7a-9add-e3f050487c10-0000000001.slice
 ...
Processing / Repairing / Extracting archive slice c0fddea4-d1f1-4c7a-9add-e3f050487c10-0000000007.slice
~~~

After successful completion of this process, you will have a single archive set file, from which the originally archived files can be extracted as shown in the next section.

### Extracting files from the archive set

To extract files from an archive set, you'll also need a private key authorized for this particular set. This private key can be supplied in various ways:
* By importing the associated certificate into the Windows or Mono certificate store using certmgr
* By supplying the private key file using the `-k` command line option. This file should be in one of the following formats:
  * Unencrypted PEM (the file most likely contains the line `-----BEGIN RSA PRIVATE KEY-----`, but no header options that hint at encryption). If your PEM key file happens to be encrypted, use OpenSSL to [remove the pass phrase](http://openssl.org/docs/apps/rsa.html) first
  * PKCS#12 a.k.a. PFX: such files are almost always encrypted, and `archive-tool` will prompt you for the password (the file extension should be *.pfx* for this to work)

For the archive set contained in the TestData-* ISO files, key files in both formats are provided inside the file `Archiveware.ReadMe.ZIP`, which is present in each image.

After obtaining this key, extracting the archive files is straightforward:

~~~
$ archive-tool.exe -t archive -i c0fddea4-d1f1-4c7a-9add-e3f050487c10.ArchivewareSet -x -k TestData.pfx
PFX password: ****
Using private key from certificate 'O=FOR TESTING PURPOSES ONLY - NO LIABILITY ACCEPTED, CN=DO NOT TRUST - Archiveware Test Certificate'
Loaded private key with ID c87dee3cf16ef9af1fa0a19b314be032d9d4e37b from TestData.pfx
Scanning c0fddea4-d1f1-4c7a-9add-e3f050487c10.ArchivewareSet for archive file headers
................
~~~

Extracted files can be found in the `ArchiveFiles` subdirectory of your working directory (or the output path if you specified it on the command line). All extracted files are validated using a SHA-384 hash, guaranteeing that any extracted data is in fact identical to that in the originally archived files.
