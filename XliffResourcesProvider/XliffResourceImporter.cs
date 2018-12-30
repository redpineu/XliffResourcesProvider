using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Babylon.ResourcesProvider;
using XliffParser;

namespace XliffResourcesProvider
{
    class XliffResourceImporter
    {
        private readonly string InvariantLanguage = string.Empty;

        private readonly string projectLocale;

        private readonly string baseDirectory;

        private readonly Dictionary<string, Dictionary<string, StringResource>> resourcesPerFile;

        private readonly XliffDocumentProvider xliffDocumentProvider;

        private readonly Regex removeMultipleSpaces;

        public XliffResourceImporter(string projectLocale, string baseDirectory)
        {
            this.projectLocale = projectLocale;
            this.baseDirectory = baseDirectory;

            resourcesPerFile = new Dictionary<string, Dictionary<string, StringResource>>();
            xliffDocumentProvider = new XliffDocumentProvider(baseDirectory);

            removeMultipleSpaces = new Regex(@"\s{2,}");
        }

        public ICollection<StringResource> Import()
        {
            var matchedXlfFile = false;
            xliffDocumentProvider.LoadXlfDocuments();

            foreach (var doc in xliffDocumentProvider.XlfDocuments)
            {
                string plainFileName = XliffFileHelpers.GetPlainFileName(baseDirectory, doc.FileName);

                foreach (var xlfFile in doc.Files)
                {
                    if (!FileMatchesProjectInvariantLocale(xlfFile))
                    {
                        continue;
                    }

                    matchedXlfFile = true;
                    string storageLocation = XliffFileHelpers.GetStorageLocation(plainFileName, xlfFile.Original);
                    string locale = GetLocale(doc.FileName, xlfFile);

                    Dictionary<string, StringResource> fileStringResources = GetStringResourcesForFile(storageLocation);

                    foreach (var transUnit in xlfFile.TransUnits)
                    {
                        StringResource stringResource = GetOrCreateStringResource(fileStringResources, storageLocation, transUnit);

                        if (ShouldWriteTargetLanguage(locale, transUnit))
                        {
                            stringResource.SetLocaleText(locale, transUnit.Target);
                        }
                    }
                }
            }

            ValidateXlfFilesFound(matchedXlfFile, xliffDocumentProvider.XlfDocuments.Count);

            return resourcesPerFile.SelectMany(t => t.Value.Select(x => x.Value)).ToList();
        }

        private static void ValidateXlfFilesFound(bool matchedXlfFile, int xlfDocumentCount)
        {
            if (!matchedXlfFile && xlfDocumentCount != 0)
            {
                throw new Exception("Found XLF-Files in directory, but none matched the project locale.");
            }
        }

        private bool FileMatchesProjectInvariantLocale(XlfFile xlfFile)
        {
            return xlfFile.SourceLang == projectLocale;
        }

        private StringResource GetOrCreateStringResource(Dictionary<string, StringResource> fileStringResources, string storageLocation, XlfTransUnit u)
        {
            if (!fileStringResources.TryGetValue(u.Id, out StringResource stringResource))
            {
                stringResource = new StringResource(u.Id, u.Optional.Notes.FirstOrDefault()?.Value ?? "")
                {
                    Name = u.Id,
                    StorageLocation = storageLocation
                };
                stringResource.SetLocaleText(InvariantLanguage, RemoveMultipleWhitespaces(u.Source));

                fileStringResources.Add(u.Id, stringResource);
            }

            return stringResource;
        }

        private string RemoveMultipleWhitespaces(string target)
        {
            return removeMultipleSpaces.Replace(target, string.Empty);
        }

        private Dictionary<string, StringResource> GetStringResourcesForFile(string storageLocation)
        {
            if (!resourcesPerFile.TryGetValue(storageLocation, out Dictionary<string, StringResource> fileStringResources))
            {
                var matchingFileNames = resourcesPerFile
                    .Where(t => ResourceLocationIsSubLocation(storageLocation, t.Key))
                    .ToList();

                if (matchingFileNames.Count == 1)
                {
                    fileStringResources = matchingFileNames[0].Value;
                }
                else
                {
                    fileStringResources = new Dictionary<string, StringResource>();
                    resourcesPerFile.Add(storageLocation, fileStringResources);
                }
            }

            return fileStringResources;
        }

        private static bool ResourceLocationIsSubLocation(string storageLocation, string path)
        {
            return path.EndsWith(storageLocation)
                || storageLocation.EndsWith(path);
        }

        private string GetLocale(string fileName, XlfFile xlfFile)
        {
            if (!string.IsNullOrWhiteSpace(xlfFile.Optional.TargetLang))
            {
                return xlfFile.Optional.TargetLang;
            }

            return XliffFileHelpers.ExtractLocaleFromFileName(fileName);
        }

        private static bool ShouldWriteTargetLanguage(string locale, XlfTransUnit translationUnit)
        {
            return (translationUnit.Target != null)
                && (!string.IsNullOrWhiteSpace(locale));
        }
    }
}