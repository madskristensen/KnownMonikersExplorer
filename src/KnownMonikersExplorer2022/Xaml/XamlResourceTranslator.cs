using System.IO;
using Reflector.BamlViewer;

namespace KnownMonikersExplorer.Xaml
{
    internal class XamlResourceTranslator : IXamlResourceTranslator
    {
        public string TranslateBaml(Stream stream)
        {
            var bamlTranslator = new BamlTranslator(stream);
            return bamlTranslator.ToString();
        }

        public string TranslateXaml(Stream stream)
        {
            return null;
        }
    }
}
