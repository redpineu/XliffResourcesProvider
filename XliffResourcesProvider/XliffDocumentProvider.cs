using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XliffParser;

namespace XliffResourcesProvider
{
    internal class XliffDocumentProvider
    {
        private readonly string baseDirectory;

        private List<XlfDocument> xlfDocuments;

        private List<XlfDocument> savedXlfDocuments;

        private List<XliffFileError> fileLoadErrors;

        private List<XliffFileError> fileSaveErrors;

        public IReadOnlyCollection<XlfDocument> XlfDocuments => xlfDocuments;

        public IReadOnlyCollection<XliffFileError> FileLoadErrors => fileLoadErrors;

        public IReadOnlyCollection<XlfDocument> SavedXlfDocuments => savedXlfDocuments;

        public IReadOnlyCollection<XliffFileError> FileSaveErrors => fileSaveErrors;

        public XliffDocumentProvider(string baseDirectory)
        {
            this.baseDirectory = baseDirectory;

            xlfDocuments = new List<XlfDocument>();
            savedXlfDocuments = new List<XlfDocument>();
            fileLoadErrors = new List<XliffFileError>();
            fileSaveErrors = new List<XliffFileError>();
        }

        public void LoadXlfDocuments()
        {
            try
            {
                foreach (var xliffFilePath in IterateXliffFilesInPath())
                {
                    try
                    {
                        xlfDocuments.Add(new XlfDocument(xliffFilePath));
                    }
                    catch (Exception ex)
                    {
                        fileLoadErrors.Add(new XliffFileError(xliffFilePath, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                fileLoadErrors.Add(new XliffFileError(baseDirectory, ex));
            }
        }

        public Dictionary<string, XliffTranslationFiles> GetXliffFilesPerLocale()
        {
            var files = new Dictionary<string, XliffTranslationFiles>();

            foreach (var document in xlfDocuments)
            {
                string xliffFilePath = XliffFileHelpers.GetRelativeFilePathTo(baseDirectory, document.FileName);
                string plainFileName = XliffFileHelpers.GetPlainFileName(baseDirectory, xliffFilePath);

                foreach (var xlfFile in document.Files)
                {
                    string storageLocation = XliffFileHelpers.GetStorageLocation(plainFileName, xlfFile.Original);
                    string locale = XliffFileHelpers.ExtractLocaleFromFileName(xliffFilePath);

                    if (!files.TryGetValue(storageLocation, out XliffTranslationFiles translationFiles))
                    {
                        translationFiles = new XliffTranslationFiles(storageLocation);
                        files.Add(storageLocation, translationFiles);
                    }

                    translationFiles.AddXliffFile(locale, xlfFile);
                }
            }

            return files;
        }

        public XlfDocument CreateEmptyXlfDocument(string location, string original, string dataType, string sourceLang)
        {
            try
            {
                using (StreamWriter sw = File.CreateText(location))
                {
                    sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    sw.WriteLine("<xliff version=\"1.2\" xmlns=\"urn: oasis:names: tc:xliff: document:1.2\">");
                    sw.WriteLine(string.Format("  <file source-language=\"{0}\" datatype=\"{1}\" original=\"{2}\">", sourceLang, dataType, original));
                    sw.WriteLine("  </file>");
                    sw.WriteLine("</xliff>");
                }

                XlfDocument doc = new XlfDocument(location);
                xlfDocuments.Add(doc);
                return doc;
            }
            catch (Exception ex)
            {
                fileSaveErrors.Add(new XliffFileError(location, ex));
                return null;
            }
        }

        public void SaveDocuments(IEnumerable<string> localesToSave)
        {
            foreach (var document in xlfDocuments)
            {
                string xliffFilePath = XliffFileHelpers.GetRelativeFilePathTo(baseDirectory, document.FileName);
                string plainFileName = XliffFileHelpers.GetPlainFileName(baseDirectory, xliffFilePath);
                string locale = XliffFileHelpers.ExtractLocaleFromFileName(xliffFilePath);

                if (!localesToSave.Contains(locale))
                {
                    continue;
                }

                try
                {
                    document.Save();
                    savedXlfDocuments.Add(document);
                }
                catch (Exception ex)
                {
                    fileSaveErrors.Add(new XliffFileError(document.FileName, ex));
                }
            }
        }

        private IEnumerable<string> IterateXliffFilesInPath()
        {
            return (new[] { "*.xliff", "*.xlf" }).AsParallel()
                .SelectMany(searchPattern =>
                    Directory.EnumerateFiles(baseDirectory, searchPattern, SearchOption.AllDirectories));
        }
    }
}
