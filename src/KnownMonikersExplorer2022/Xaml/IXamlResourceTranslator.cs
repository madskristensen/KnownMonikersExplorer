using System.IO;

namespace KnownMonikersExplorer.Xaml
{
    public interface IXamlResourceTranslator
    {
        string TranslateBaml(Stream stream);
        string TranslateXaml(Stream stream);
    }
}
