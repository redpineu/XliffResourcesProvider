using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Babylon.ResourcesProvider;
using XliffParser;

namespace XliffResourcesProvider
{
    class XliffResourceExporter
    {
        private readonly string InvariantLanguage = string.Empty;

        private readonly string baseDirectory;

        private readonly string projectLocale;

        private readonly ICollection<StringResource> resourceStrings;

        private readonly IReadOnlyCollection<string> localesToExport;

        private readonly XliffDocumentProvider xliffDocumentProvider;

        private readonly Action<string> fileSavedSuccessfulAction;

        private readonly Action<XliffFileError> fileErrorAction;

        public XliffResourceExporter(string baseDirectory, string projectLocale, IReadOnlyCollection<string> localesToExport, ICollection<StringResource> resourceStrings, Action<string> fileSavedSuccessfulAction, Action<XliffFileError> fileSaveErrorAction)
        {
            this.baseDirectory = baseDirectory;
            this.projectLocale = projectLocale;
            this.localesToExport = localesToExport;
            this.resourceStrings = resourceStrings;
            this.fileSavedSuccessfulAction = fileSavedSuccessfulAction;
            this.fileErrorAction = fileSaveErrorAction;

            xliffDocumentProvider = new XliffDocumentProvider(baseDirectory);
        }

        public void Export()
        {
            xliffDocumentProvider.LoadXlfDocuments();
            ReportFileLoadErrors();
            Dictionary<string, XliffTranslationFiles> xliffFiles = xliffDocumentProvider.GetXliffFilesPerLocale();

            IEnumerable<IGrouping<string, StringResource>> groupedStorageLocations = resourceStrings.GroupBy(t => t.StorageLocation);

            foreach (var grouping in groupedStorageLocations)
            {
                IEnumerable<string> missingLocales;
                if (xliffFiles.TryGetValue(grouping.Key, out XliffTranslationFiles translationFiles))
                {
                    missingLocales = localesToExport.Where(t => !translationFiles.AvailableLocales.Contains(t));
                }
                else
                {
                    translationFiles = new XliffTranslationFiles(grouping.Key);
                    xliffFiles.Add(grouping.Key, translationFiles);
                    missingLocales = localesToExport;
                }

                if (!translationFiles.TryGetXliffFileForLocale(InvariantLanguage, out XlfFile invariantFile))
                {
                    if (grouping.Key.Contains("\\"))
                    {
                        int index = grouping.Key.LastIndexOf("\\");
                        if ((index < 0) || (index + 1 == grouping.Key.Length))
                        {
                            continue;
                        }

                        string originalFileName = grouping.Key.Substring(grouping.Key.LastIndexOf("\\") + 1);
                        if (originalFileName.Length == 0)
                        {
                            continue;
                        }

                        invariantFile = CreateXliffFile(grouping.Key, InvariantLanguage, originalFileName, "plaintext", projectLocale);
                        translationFiles.AddXliffFile(InvariantLanguage, invariantFile);
                    }
                }

                foreach (var missingLocale in missingLocales)
                {
                    var xlfFile = CreateXliffFile(grouping.Key, missingLocale, invariantFile);
                    translationFiles.AddXliffFile(missingLocale, xlfFile);
                }

                foreach (var stringResource in grouping)
                {
                    foreach (var locale in stringResource.GetLocales())
                    {
                        if (!localesToExport.Contains(locale))
                        {
                            continue;
                        }

                        if (!translationFiles.TryGetXliffFileForLocale(locale, out XlfFile xlfFile))
                        {
                            continue;
                        }

                        if (xlfFile.TryGetTransUnit(stringResource.Name, XlfDialect.Standard, out XlfTransUnit transUnit))
                        {
                            transUnit.Target = stringResource.GetLocaleText(locale);
                        }
                        else
                        {
                            string source;
                            if (invariantFile.TryGetTransUnit(stringResource.Name, XlfDialect.Standard, out XlfTransUnit invariantTransUnit))
                            {
                                source = invariantTransUnit.Source;
                            }
                            else if (stringResource.IsLocalePresent(InvariantLanguage))
                            {
                                source = stringResource.GetLocaleText(InvariantLanguage);
                            }
                            else
                            {
                                continue;
                            }

                            xlfFile.AddTransUnit(stringResource.Name, source, stringResource.GetLocaleText(locale), XlfFile.AddMode.FailIfExists, XlfDialect.Standard);
                        }
                    }
                }
            }

            xliffDocumentProvider.SaveDocuments(localesToExport);
            ReportFileSaveStatus();
        }

        private XlfFile CreateXliffFile(string storageLocation, string locale, XlfFile invariantFile)
        {
            return CreateXliffFile(storageLocation, locale, invariantFile.Original, invariantFile.DataType, invariantFile.SourceLang);
        }

        private XlfFile CreateXliffFile(string storageLocation, string locale, string originalFileName, string dataType, string sourceLang)
        {
            if (IsLocaleInvariantSourceLanguage(locale, sourceLang))
            {
                throw new Exception();
            }

            string location = GetTargetFileNameForStringResource(storageLocation, locale, originalFileName);
            if (IsXliffFileAKnownFileSaveError(location))
            {
                throw new Exception();
            }

            XlfDocument doc = xliffDocumentProvider.CreateEmptyXlfDocument(location, originalFileName, dataType, sourceLang);
            if (doc is null)
            {
                throw new Exception();
            }

            return doc.Files.Single();
        }

        private void ReportFileLoadErrors()
        {
            foreach (var fileLoadError in xliffDocumentProvider.FileLoadErrors)
            {
                fileErrorAction(fileLoadError);
            }
        }

        private void ReportFileSaveStatus()
        {
            foreach (var xlfDocument in xliffDocumentProvider.SavedXlfDocuments)
            {
                fileSavedSuccessfulAction(xlfDocument.FileName);
            }

            foreach (var fileSaveError in xliffDocumentProvider.FileSaveErrors)
            {
                fileErrorAction(fileSaveError);
            }
        }

        private bool IsXliffFileAKnownFileSaveError(string location)
        {
            return xliffDocumentProvider.FileSaveErrors.Any(t => t.XliffFilePath == location);
        }

        private static XliffTranslationFiles GetLocaleFiles(IReadOnlyDictionary<string, XliffTranslationFiles> xliffFiles, StringResource stringResource)
        {
            if (xliffFiles.TryGetValue(stringResource.StorageLocation, out XliffTranslationFiles translationFiles))
            {
                return translationFiles;
            }

            return null;
        }

        private static bool IsLocaleInvariantSourceLanguage(string locale, string sourceLang)
        {
            return locale == sourceLang;
        }

        private string GetTargetFileNameForStringResource(string storageLocation, string targetLocale, string originalFileName)
        {
            string location = storageLocation;
            if (storageLocation.EndsWith(originalFileName))
            {
                location = location.Replace($"\\{originalFileName}", "");
            }

            location += $".{targetLocale}.xlf";

            location = Path.Combine(baseDirectory, location);
            return location;
        }
    }
}
