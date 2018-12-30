using System.Collections.Generic;
using Babylon.ResourcesProvider;

namespace XliffResourcesProvider
{
    public class XliffResourcesProvider : IResourcesProvider
    {
        private readonly string InvariantLanguage = string.Empty;

        /// <summary>
        /// The name of the resource provider. Use this to indicate what type of files/storage the provider is working with.
        /// </summary>
        public string Name
        {
            get
            {
                return "XLIFF Resources Provider";
            }
        }

        /// <summary>
        /// Detailed description of how the provider works.
        /// </summary>
        public string Description
        {
            get
            {
                return "XLIFF 1.2 Resources Provider. Implements the XLIFF 1.2 standard";
            }
        }

        /// <summary>
        /// Path to the solution file. Set by Babylon.NET. Use this in the provider to build absolute paths if the storage location is relative to the solution.
        /// </summary>
        public string SolutionPath { get; set; }

        /// <summary>
        /// The storage location where the string resources are read and written to are located. This generally is a file or a base directory but could also be a connection string to the database.
        /// </summary>
        public string StorageLocation { get; set; }

        /// <summary>
        /// Text to be displayed to the user when the storage location should be spefied: e.g. "Please select the base directory".
        /// </summary>
        public string StorageLocationUserText
        {
            get
            {
                return "Base Directory where language files are located";
            }
        }

        /// <summary>
        /// Indicates what kind of storage is used for the storage location. This will determine whether Babylon.NET displays a file selection or directory selection control or a simple textx control.
        /// </summary>
        public StorageType StorageType
        {
            get
            {
                return StorageType.Directory;
            }
        }

        /// <summary>
        /// Imports the resource strings to be translated into the Babylon.NET project
        /// </summary>
        /// <param name="projectName">Name of the project the resources will be imported into</param>
        /// <param name="projectLocale">The project invariant locale</param>
        /// <returns>The imported strings</returns>
        public ICollection<StringResource> ImportResourceStrings(string projectName, string projectLocale)
        {
            var resourceImporter = new XliffResourceImporter(projectLocale, XliffFileHelpers.GetBaseDirectory(StorageLocation, SolutionPath));
            return resourceImporter.Import();
        }

        /// <summary>
        /// Exports the resource strings from the Babylon.NET project
        /// </summary>
        /// <param name="projectName">Name of the project the resources will be exported from</param>
        /// <param name="projectLocale">The project invariant locale</param>
        /// <param name="resourceStrings">The strings to be exported</param>
        /// <param name="resultDelegate">Callback delegate to provide progress information and storage operation results</param>
        public void ExportResourceStrings(string projectName, string projectLocale, IReadOnlyCollection<string> localesToExport, ICollection<StringResource> resourceStrings, ResourceStorageOperationResultDelegate resultDelegate)
        {
            XliffResourceExporter xliffResourceExporter = new XliffResourceExporter(XliffFileHelpers.GetBaseDirectory(StorageLocation, SolutionPath), projectLocale, localesToExport, resourceStrings,
                (string xliffFileName) =>
                {
                    resultDelegate?.Invoke(new ResourceStorageOperationResultItem(xliffFileName)
                    {
                        ProjectName = projectName
                    });
                },
                (XliffFileError fileError) =>
                {
                    resultDelegate?.Invoke(new ResourceStorageOperationResultItem(fileError.XliffFilePath)
                    {
                        ProjectName = projectName,
                        Result = ResourceStorageOperationResult.Error,
                        Message = fileError.Ex.Message
                    });
                });
            xliffResourceExporter.Export();
        }
    }
}
