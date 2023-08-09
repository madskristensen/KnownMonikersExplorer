using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Windows
{
    public class ImageMonikerProperty
    {
        public ImageMonikerProperty(string propertyName, ImageMoniker imageMoniker)
        {
            PropertyName = propertyName;
            ImageMoniker = imageMoniker;
        }

        public string PropertyName { get; }

        public ImageMoniker ImageMoniker { get; }
    }
}
