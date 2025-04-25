using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KnownMonikersExplorer.Xaml.Reflection
{
    internal class SingleImageBundle {
        private static FieldInfo _sourcesField;

        public List<UriSourceDescriptor> UriSourceDescriptors { get; }
        public static bool Initialize(Assembly assembly)
        {
            var singleImageBundleType = assembly.DefinedTypes.FirstOrDefault(t => t.Name == "SingleImageBundle");
            if (singleImageBundleType == null) return false;
            _sourcesField = singleImageBundleType.GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_sourcesField == null) return false;
            return UriSourceDescriptor.Initialize(assembly);
        }
        public SingleImageBundle(object imageBundle)
        {
            if (imageBundle.GetType().Name == "SingleImageBundle")
            {
                var sourcesObject = _sourcesField.GetValue(imageBundle);
                Array sources = null;
                if (sourcesObject is Array sourcesArray)
                {
                    sources = sourcesArray;
                }
                else
                {
                    sources = new object[] { sourcesObject };
                }
                UriSourceDescriptors = sources.Cast<object>().Select(o => UriSourceDescriptorFactory.Create(o)).Where(usd => usd != null).ToList();
            }
        }
    }
}
