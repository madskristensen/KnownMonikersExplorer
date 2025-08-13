using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Xaml.Reflection
{
    public class NoopXamlProvider : IXamlProvider
    {
        public string GetXaml(ImageMoniker imageMoniker)
        {
            return null;
        }
    }
}
