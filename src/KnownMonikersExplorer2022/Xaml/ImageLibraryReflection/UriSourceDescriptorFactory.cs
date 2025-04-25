namespace KnownMonikersExplorer.Xaml.Reflection
{
    internal static class UriSourceDescriptorFactory
    {
        public static UriSourceDescriptor Create(object uriSourceDescriptor)
        {
            if (uriSourceDescriptor.GetType().Name == "UriSourceDescriptor")
            {
                return new UriSourceDescriptor(uriSourceDescriptor);
            }
            return null;
        }
    }
}
