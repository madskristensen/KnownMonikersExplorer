using System.IO;
using System.Linq;
using System.Reflection;

namespace KnownMonikersExplorer.Xaml.Reflection
{
    internal class UriSourceDescriptor
    {
        private static PropertyInfo _resourceTypePropertyInfo;
        private static PropertyInfo _resourceByteArrayPropertyInfo;
        private readonly object _uriSourceDescriptor;
        private static ResourceType GetResourceType(object uriSourceDescriptor)
        {
            var resourceTypeString = _resourceTypePropertyInfo.GetValue(uriSourceDescriptor).ToString();
            switch (resourceTypeString)
            {
                case "Baml":
                    return ResourceType.Baml;
                case "Xaml":
                    return ResourceType.Xaml;
            }
            return ResourceType.Other;
        }

        public UriSourceDescriptor(object uriSourceDescriptor) {
            ResourceType = GetResourceType(uriSourceDescriptor);
            _uriSourceDescriptor = uriSourceDescriptor;
        }
        public ResourceType ResourceType { get; }

        internal bool IsBamlOrXaml()
        {
            return ResourceType == ResourceType.Baml || ResourceType == ResourceType.Xaml;
        }

        internal Stream GetResourceStream()
        {
            var resourceBytes = _resourceByteArrayPropertyInfo.GetValue(_uriSourceDescriptor) as byte[];
            if (resourceBytes != null)
            {
                return new MemoryStream(resourceBytes);
            }
            return null;
        }

        internal static bool Initialize(Assembly assembly)
        {
            var uriSourceDescriptorType = assembly.DefinedTypes.FirstOrDefault(t => t.Name == "UriSourceDescriptor");
            if (uriSourceDescriptorType == null) return false;
            _resourceByteArrayPropertyInfo = uriSourceDescriptorType.GetProperty("ResourceByteArray", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_resourceByteArrayPropertyInfo == null) return false;
            _resourceTypePropertyInfo = uriSourceDescriptorType.GetProperty("ResourceType", BindingFlags.NonPublic | BindingFlags.Instance);
            return _resourceTypePropertyInfo != null;
        }
    }
}
