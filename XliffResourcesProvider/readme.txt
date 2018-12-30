XLIFF provider for Babylon

The provider will read all XLIFF 1.2 files in the selected base directory and all folders underneath it.

Files are named using the pattern <filename>.[<culture code=code>].xlf. The provider assumes invariant strings are contained in a file with no culture code in the file name (e.g. strings.xlf). All files containing culture codes (e.g. strings.de-DE.json) will be treated as translations. The source language of the invariant file must match the Babylon.NET's Solution invariant locale.

Strings not present in the invariant file are ignored.

Relative paths are fully supported.

Comments are not supported. 