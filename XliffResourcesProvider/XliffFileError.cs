using System;

namespace XliffResourcesProvider
{
    internal class XliffFileError
    {
        public XliffFileError(string xliffFilePath, Exception ex)
        {
            XliffFilePath = xliffFilePath;
            Ex = ex;
        }

        public string XliffFilePath { get; }

        public Exception Ex { get; }
    }
}
