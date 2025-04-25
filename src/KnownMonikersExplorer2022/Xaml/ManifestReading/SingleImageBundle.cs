using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Xaml.ManifestReading
{
    internal class SingleImageBundle { 
        public SingleImageBundle(ImageMoniker imageMoniker, ICollection<UriSourceDescriptor> uriSourceDescriptors)
        {
            ImageMoniker = imageMoniker;
            UriSourceDescriptors = uriSourceDescriptors;
        }

        public ImageMoniker ImageMoniker { get; }
        public ICollection<UriSourceDescriptor> UriSourceDescriptors { get; }
    }
}
