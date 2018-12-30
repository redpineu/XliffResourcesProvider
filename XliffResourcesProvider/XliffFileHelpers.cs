using System.IO;

namespace XliffResourcesProvider
{
    public static class XliffFileHelpers
    {
        public static string ExtractLocaleFromFileName(string file)
        {
            return Path.GetExtension(Path.GetFileNameWithoutExtension(file)).TrimStart(new char[] { '.' });
        }

        public static string GetStorageLocation(string plainFileName, string xlfFileOriginal)
        {
            return $"{plainFileName}\\{xlfFileOriginal}";
        }

        public static string GetPlainFileName(string baseDirectory, string file)
        {
            string relativeDirectory = Path.GetDirectoryName(file.Replace(baseDirectory, "")).TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(relativeDirectory, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileName(file))));
        }

        public static string GetBaseDirectory(string storageLocation, string solutionPath)
        {
            if (Path.IsPathRooted(storageLocation))
            {
                return storageLocation;
            }

            return Path.GetFullPath(Path.Combine(solutionPath, storageLocation));
        }

        public static string GetRelativeFilePathTo(string baseDirectory, string location)
        {
            return location.Replace(baseDirectory, "");
        }
    }
}
