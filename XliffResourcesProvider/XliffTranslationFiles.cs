using System;
using System.Collections.Generic;
using XliffParser;

namespace XliffResourcesProvider
{
    class XliffTranslationFiles
    {
        public string StorageLocation { get; }

        public ICollection<string> AvailableLocales { get => xliffFilesPerLocale.Keys; }

        public ICollection<XlfFile> XliffFiles { get => xliffFilesPerLocale.Values; }

        private readonly Dictionary<string, XlfFile> xliffFilesPerLocale = new Dictionary<string, XlfFile>();

        public XliffTranslationFiles(string storageLocation)
        {
            StorageLocation = storageLocation;
        }

        public void AddXliffFile(string fileLocale, XlfFile xlfFile)
        {
            if (xliffFilesPerLocale.ContainsKey(fileLocale))
            {
                throw new NotSupportedException(string.Format("It is not possible to have multiple translation files for the same locale: {0}", fileLocale));
            }

            xliffFilesPerLocale[fileLocale] = xlfFile;
        }

        public bool TryGetXliffFileForLocale(string fileLocale, out XlfFile xlfFile)
        {
            return xliffFilesPerLocale.TryGetValue(fileLocale, out xlfFile);
        }
    }
}
