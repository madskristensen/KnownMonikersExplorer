using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KnownMonikersExplorer.Xaml.ManifestReading
{
    internal class ImageManifestFileFinder
    {
        private const string _matchAllPattern = "*";
        private const string _deleteMePattern = "*.deleteme";
        private const string _imageManifestPattern = "*.imagemanifest";

        internal List<string> GetImageManifestFiles(string searchPath, int searchDepth)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNullAndNotWhiteSpace(searchPath, nameof(searchPath));
            Microsoft.Internal.VisualStudio.Shell.Validate.IsWithinRange(searchDepth, 1, int.MaxValue, nameof(searchDepth));
            List<string> imageManifestFiles = new List<string>();
            try
            {
                string str1 = searchPath;
                char[] chArray = new char[1]
                {
        Path.PathSeparator
                };
                foreach (string str2 in str1.Split(chArray))
                {
                    string str3 = str2.Trim();
                    if (!Directory.Exists(str3))
                    {
                    }
                    else
                    {
                        List<string> list = this.FindManifests(str3, searchDepth).ToList<string>();
                        imageManifestFiles.AddRange((IEnumerable<string>)list);
                    }
                }
            }
            finally
            {
            }
            return imageManifestFiles;
        }

        private IEnumerable<string> FindManifests(string directory, int searchDepth)
        {
            IEnumerable<string> first = Enumerable.Empty<string>();
            try
            {
                if (this.ShouldSkipDirectory(directory, "*.deleteme"))
                    return first;
                first = first.Concat<string>(Directory.EnumerateFiles(directory, "*.imagemanifest", SearchOption.TopDirectoryOnly));
                if (--searchDepth < 1)
                    return first;
                foreach (string enumerateDirectory in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                    first = first.Concat<string>(this.FindManifests(enumerateDirectory, searchDepth));
            }
            catch
            {
               
            }
            return first;
        }

        private bool ShouldSkipDirectory(string directory, string searchPattern) => Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly).Any<string>();
    }
}
