# archive-tool

This tool analyzes and (optionally) extracts files from Archiveware version 3 containers. Damaged containers are repaired if possible; in case of missing or broken containers, file contents are recovered to the maximum extent possible.

## Getting started

* Install the prerequisite Microsoft .NET Framework version 4.5 or higher on Windows, Mac or Linux, clone this repository and compile archive-tool
* Copy the Archiveware container and metadata files from the Blu-ray disks, tapes or Petablock disks you want to restore to a temporary folder. If read errors occur, copy as much data as possible, zeroing out truly unreadable parts of files
* Run archive-tool to analyze the container files in the temporary folder. If successful, files can be extracted, provided you provide a suitable decryption key (X.509 certificate authorized at the time of archiving)