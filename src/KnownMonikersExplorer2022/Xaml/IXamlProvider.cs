using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Xaml
{
    public interface IXamlProvider
    {
        string GetXaml(ImageMoniker imageMoniker);
    }
}
