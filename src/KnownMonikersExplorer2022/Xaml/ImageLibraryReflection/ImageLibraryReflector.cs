using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Xaml.Reflection
{
    public class ImageLibraryReflector : IXamlProvider
    {
        private readonly IXamlResourceTranslator _xamlResourceTranslator;
        private ImageBundleProvider _imageBundleProvider;
        
        public ImageLibraryReflector(IXamlResourceTranslator xamlResourceTranslator)
        {
            _xamlResourceTranslator = xamlResourceTranslator;
        }

        public bool Initialize()
        {
            _imageBundleProvider = new ImageBundleProvider(ImageLibrary.Default);
            var success = _imageBundleProvider.Initialize();
            return success ? SingleImageBundle.Initialize(typeof(ImageLibrary).Assembly) : false;
        }

        public string GetXaml(ImageMoniker imageMoniker)
        {
            try
            {
                var success = _imageBundleProvider.TryGetBundle(imageMoniker, out var imageBundle);
                if (success)
                {
                    var singleImageBundle = new SingleImageBundle(imageBundle);
                    var bamlOrXamlUriSourceDescriptor = singleImageBundle.UriSourceDescriptors.FirstOrDefault(uriSourceDescriptor => uriSourceDescriptor.IsBamlOrXaml());
                    if (bamlOrXamlUriSourceDescriptor == null) return null;
                    Stream resourceStream = bamlOrXamlUriSourceDescriptor.GetResourceStream();
                    if (resourceStream == null) return null;
                    return bamlOrXamlUriSourceDescriptor.ResourceType == ResourceType.Baml ? _xamlResourceTranslator.TranslateBaml(resourceStream) : _xamlResourceTranslator.TranslateXaml(resourceStream);
                }
            }
            catch { }
            return null;
        }
    }
}
