using KnownMonikersExplorer.Xaml.ManifestReading;
using KnownMonikersExplorer.Xaml.Reflection;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Xaml
{
    internal class XamlProvider : IXamlProvider
    {
        private IXamlProvider _manifestReadingXamlProvider;
        private IXamlProvider _reflectionXamlProvider;
        public XamlProvider() {
            var resourceTranslator = new XamlResourceTranslator();
            _manifestReadingXamlProvider = new ManifestReadingXamlProvider(resourceTranslator);
            _reflectionXamlProvider = ImageLibraryReflectorFactory.Create();
        }

        public string GetXaml(ImageMoniker imageMoniker)
        {
            var xaml = _manifestReadingXamlProvider.GetXaml(imageMoniker);
            return xaml ?? _reflectionXamlProvider.GetXaml(imageMoniker);
        }
    }
}
