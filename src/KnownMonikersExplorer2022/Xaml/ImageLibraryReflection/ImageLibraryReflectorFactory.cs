namespace KnownMonikersExplorer.Xaml.Reflection
{
    public static class ImageLibraryReflectorFactory
    {
        public static IXamlProvider Create()
        {
            var xamlProvider = new ImageLibraryReflector(new XamlResourceTranslator());
            var success = xamlProvider.Initialize();
            if (success)
            {
                return xamlProvider;
            }
            return new NoopXamlProvider();
        }
    }
}
