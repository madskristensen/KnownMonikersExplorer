using System.Reflection;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace KnownMonikersExplorer.Xaml.Reflection
{
    internal class ImageBundleProvider
    {
        private FieldInfo _imageBundlesFieldInfo;
        private object _imageBundlesObject;
        private MethodInfo _tryGetBundleMethod;
        private MethodInfo _monikerToInternalTypeMethodInfo;
        private readonly ImageLibrary _imageLibrary;

        public ImageBundleProvider(ImageLibrary imageLibrary)
        {
            _imageLibrary = imageLibrary;
        }

        public bool Initialize()
        {
            _imageBundlesFieldInfo = typeof(ImageLibrary).GetField("_imageBundles", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_imageBundlesFieldInfo == null) return false;
            _imageBundlesObject = _imageBundlesFieldInfo.GetValue(_imageLibrary);
            _tryGetBundleMethod = _imageBundlesObject.GetType().GetMethod("TryGetBundle");
            if (_tryGetBundleMethod == null) return false;
            _monikerToInternalTypeMethodInfo = typeof(ExtensionMethods).GetMethod("ToInternalType", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(ImageMoniker) }, null);
            if (_monikerToInternalTypeMethodInfo == null) return false;
            return true;
        }

        public bool TryGetBundle(ImageMoniker imageMoniker, out object bundle)
        {
            bundle = null;
            object[] args = new object[] { _monikerToInternalTypeMethodInfo.Invoke(null, new object[] { imageMoniker }), null };
            bool success = (bool)_tryGetBundleMethod.Invoke(_imageBundlesObject, args);

            if (success)
            {
                bundle = args[1];
            }
            return success;
        }
    }
}
