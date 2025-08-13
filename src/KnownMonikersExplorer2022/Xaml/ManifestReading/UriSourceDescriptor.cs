using System;
using System.IO.Packaging;

namespace KnownMonikersExplorer.Xaml.ManifestReading
{
    internal class UriSourceDescriptor
    {

        private readonly System.WeakReference<Uri> _weakUri = new System.WeakReference<Uri>((Uri)null);
        public UriSourceDescriptor(string source)
        {
            UriString = source;
            ValidateUriString(UriString);
        }

        public Uri Uri
        {
            get
            {
                Uri target;
                if (!this._weakUri.TryGetTarget(out target))
                {
                    target = UriSourceDescriptor.MakeUri(this.UriString);
                    this._weakUri.SetTarget(target);
                }
                return target;
            }
        }
        private static bool IsFileUri(Uri uri) => uri.IsAbsoluteUri && !(uri.Scheme != "file");
        private static void ValidateUriString(string uriString)
        {
            Uri uri = UriSourceDescriptor.MakeUri(uriString);
            if (uri.IsAbsoluteUri && !UriSourceDescriptor.IsFileUri(uri))
                throw new ArgumentException("string.Format(Microsoft.VisualStudio.Imaging.Resources.Error_UnsupportedUri, (object)uri.Scheme)", nameof(uriString));
        }
        private static Uri MakeUri(string uriString)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)uriString, nameof(uriString));
            UriKind uriKind = UriKind.RelativeOrAbsolute;
            if (uriString.StartsWith(PackUriHelper.UriSchemePack, StringComparison.OrdinalIgnoreCase))
                uriKind = UriKind.Absolute;
            return new Uri(uriString, uriKind);
        }
        private string UriString { get; }
    }
}
