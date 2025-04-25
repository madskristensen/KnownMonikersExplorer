using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Resources;
using System.Windows;

namespace KnownMonikersExplorer.Xaml.ManifestReading
{
    internal class ManifestReadingXamlProvider : IXamlProvider
    {
        private bool _initialized;
        private readonly IXamlResourceTranslator _xamlResourceTranslator;

        public List<ManifestReading.SingleImageBundle> Images { get; private set; }

        public ManifestReadingXamlProvider(IXamlResourceTranslator xamlResourceTranslator)
        {
            _xamlResourceTranslator = xamlResourceTranslator;
        }
        public string GetXaml(ImageMoniker imageMoniker)
        {
            EnsureInitiaized();
            if(this.Images != null)
            {
                var imageBundle = this.Images.FirstOrDefault(i => i.ImageMoniker.Guid == imageMoniker.Guid && i.ImageMoniker.Id == imageMoniker.Id);
                if(imageBundle != null)
                {
                    var xamlDescriptor =  imageBundle.UriSourceDescriptors.FirstOrDefault(d => d.Uri.ToString().EndsWith(".xaml"));
                    if(xamlDescriptor != null)
                    {
                        StreamResourceInfo resourceStream = Application.GetResourceStream(xamlDescriptor.Uri);
                        var contentType = GetResourceType(resourceStream.ContentType);
                        var stream = resourceStream.Stream;
                        return contentType == ResourceType.Baml ? _xamlResourceTranslator.TranslateBaml(stream) : _xamlResourceTranslator.TranslateXaml(stream);
                    }
                }
            }
            return null;
        }
        private ResourceType GetResourceType(string contentType)
        {
            switch (contentType.ToLowerInvariant())
            {
                case "application/xaml+xml":
                    return ResourceType.Xaml;
                case "application/baml+xml":
                    return ResourceType.Baml;
                
                default:
                    return ResourceType.Other;
            }
        }
        private void EnsureInitiaized()
        {
            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }
        }
  
        private static SettingsStore GetConfigurationStore()
        {
            var settingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return settingsManager.GetReadOnlySettingsStore(SettingsScope.Configuration);
        }

        private static string GetManifestSearchPath()
        {
            return GetConfigurationStore().GetString("Initialization", "ImageManifestSearchPath");
        }
        private void Initialize()
        {
            var manifestFiles = new ImageManifestFileFinder().GetImageManifestFiles(GetManifestSearchPath(), 5);
            var manifest = manifestFiles.FirstOrDefault(f => f.EndsWith("Microsoft.VisualStudio.ImageCatalog.imagemanifest"));
            // experimental for 2019...
            if(manifest != null)
            {
                this.Images = new ManifestReader().Read(manifest).Images;
            }
        }
    }
}
