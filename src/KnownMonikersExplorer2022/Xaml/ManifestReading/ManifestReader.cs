using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Imaging.Serialization;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;
using System.Windows.Resources;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using System.Windows;

namespace KnownMonikersExplorer.Xaml.ManifestReading
{
    internal class ManifestReader
    {
        private static readonly Uri _schemaUri = new Uri("/Microsoft.VisualStudio.Imaging;Component/ImageManifest.xsd", UriKind.Relative);
        internal static readonly string[] BuiltInSymbolNames = new string[7]
        {
      "CommonProgramFiles",
      "LocalAppData",
      "ManifestFolder",
      "MyDocuments",
      "ProgramFiles",
      "System",
      "WinDir"
        };
        private readonly XmlReaderSettings _readerSettings = new XmlReaderSettings()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = (XmlResolver)null
        };
        private readonly HashSet<string> _alreadyProcessedManifests = new HashSet<string>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
        private readonly bool _preloadResources;

        public ManifestReader()
          : this(false)
        {
        }

        public ManifestReader(bool preloadResources)
        {
            StreamResourceInfo resourceStream = Application.GetResourceStream(ManifestReader._schemaUri);
            XmlSchema schema = XmlSchema.Read(XmlReader.Create(resourceStream.Stream, this._readerSettings), (ValidationEventHandler)null);
            if (resourceStream.Stream != null)
                resourceStream.Stream.Close();
            this._readerSettings.ValidationType = ValidationType.Schema;
            this._readerSettings.Schemas.Add(schema);
            this._preloadResources = preloadResources;
        }

        public ProcessedManifest Read(string manifestFile, ITracer tracer = null)
        {
            if (tracer == null)
                tracer = Tracer.Null;
            return this.Read(manifestFile, tracer, false);
        }

        internal ProcessedManifest Read(string manifestFile, ITracer tracer, bool symbolsOnly)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNullAndNotEmpty(manifestFile, nameof(manifestFile));
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)tracer, nameof(tracer));
            int num = symbolsOnly ? 1 : 0;
            if (!this._alreadyProcessedManifests.Add(manifestFile))
            {
                tracer.TraceInformation("\"{0}\" has already been processed, skipping", (object)manifestFile);
                return (ProcessedManifest)null;
            }
            string str = symbolsOnly ? "importing symbols from" : "reading";
            try
            {
                tracer.TraceInformation("Begin {0} manifest {1}", (object)str, (object)manifestFile);
                using (StreamReader reader = new StreamReader(manifestFile))
                    return this.Read((TextReader)reader, manifestFile, tracer, symbolsOnly);
            }
            finally
            {
                tracer.TraceInformation("End {0} manifest {1}", (object)str, (object)manifestFile);
                if (!symbolsOnly)
                    this._alreadyProcessedManifests.Clear();
            }
        }

        internal ProcessedManifest Read(
          TextReader reader,
          string manifestFile,
          ITracer tracer,
          bool symbolsOnly)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)reader, nameof(reader));
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)tracer, nameof(tracer));
            ProcessedManifest processedManifest = new ProcessedManifest(manifestFile, tracer);
            ManifestReader.ProcessManifestSection(processedManifest, (Action)(() =>
            {
                using (XmlReader xmlReader = XmlReader.Create(reader, this._readerSettings))
                {
                    ImageManifest imageManifest = (ImageManifest)new XmlSerializer(typeof(ImageManifest)).Deserialize(xmlReader);
                    this.AddBuiltInSymbols(processedManifest, manifestFile);
                    this.ProcessSymbols(processedManifest, imageManifest.Symbols, tracer);
                    if (symbolsOnly)
                        return;
                    ManifestReader.ProcessPackageGuid(processedManifest, imageManifest.PackageGuid);
                    ManifestReader.ProcessImages(processedManifest, imageManifest.Images, this._preloadResources);
                }
            }));
            return processedManifest;
        }

        private void AddBuiltInSymbols(ProcessedManifest processedManifest, string manifestFile)
        {
            this.AddEnvironmentSymbol(processedManifest, "CommonProgramFiles");
            this.AddEnvironmentSymbol(processedManifest, "LocalAppData");
            this.AddEnvironmentSymbol(processedManifest, "ProgramFiles");
            string str1 = Path.Combine(this.AddEnvironmentSymbol(processedManifest, "WinDir"), "system32");
            processedManifest.AddBuiltInSymbol("System", str1);
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            processedManifest.AddBuiltInSymbol("MyDocuments", folderPath);
            string str2 = string.IsNullOrWhiteSpace(manifestFile) ? string.Empty : Path.GetDirectoryName(manifestFile);
            processedManifest.AddBuiltInSymbol("ManifestFolder", str2);
        }

        private string AddEnvironmentSymbol(ProcessedManifest processedManifest, string variable)
        {
            string environmentVariable = Environment.GetEnvironmentVariable(variable);
            processedManifest.AddBuiltInSymbol(variable, environmentVariable);
            return environmentVariable;
        }

        private void ProcessSymbols(
          ProcessedManifest processedManifest,
          object[] symbols,
          ITracer tracer)
        {
            if (symbols == null)
                return;
            foreach (object symbol1 in symbols)
            {
                object symbol = symbol1;
                ManifestReader.ProcessManifestSection(processedManifest, (Action)(() =>
                {
                    KeyValuePair<string, object> entry = this.ProcessSymbol(processedManifest, symbol, tracer);
                    if (entry.Value is IEnumerable<KeyValuePair<string, object>> keyValuePairs2)
                    {
                        foreach (KeyValuePair<string, object> keyValuePair in keyValuePairs2)
                        {
                            KeyValuePair<string, object> e = keyValuePair;
                            ManifestReader.ProcessManifestSection(processedManifest, (Action)(() =>
                            {
                                if (ManifestReader.IsBuiltInSymbol(e.Key))
                                    return;
                                ManifestReader.AddUserSymbol(processedManifest, e);
                            }));
                        }
                    }
                    else
                    {
                        if (entry.Key == null || entry.Value == null)
                            return;
                        ManifestReader.AddUserSymbol(processedManifest, entry);
                    }
                }));
            }
        }

        private static void AddUserSymbol(
          ProcessedManifest processedManifest,
          KeyValuePair<string, object> entry)
        {
            if (processedManifest.Symbols.ContainsKey(entry.Key))
                //todo
                throw new ManifestParseException(string.Format("Microsoft.VisualStudio.Imaging.Resources.Error_SymbolAlreadyDefined", (object)entry.Key), XmlSeverityType.Warning);
            processedManifest.AddUserSymbol(entry.Key, entry.Value);
        }

        private KeyValuePair<string, object> ProcessSymbol(
          ProcessedManifest processedManifest,
          object symbol,
          ITracer tracer)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull(symbol, nameof(symbol));
            return ManifestReader.ProcessManifestElement<object, KeyValuePair<string, object>>(symbol, (Func<object, KeyValuePair<string, object>>)(s =>
            {
                Guid result;
                switch (symbol)
                {
                    case ImageManifestID imageManifestId2:
                        return new KeyValuePair<string, object>(imageManifestId2.Name, (object)imageManifestId2.Value);
                    case ImageManifestGuid imageManifestGuid2 when Guid.TryParse(imageManifestGuid2.Value, out result):
                        return new KeyValuePair<string, object>(imageManifestGuid2.Name, (object)result);
                    case ImageManifestString imageManifestString2:
                        return new KeyValuePair<string, object>(imageManifestString2.Name, (object)imageManifestString2.Value);
                    case ImageManifestImport imageManifestImport2:
                        return this.ImportSymbols(processedManifest, imageManifestImport2.Manifest, tracer);
                    default:
                        throw ManifestReader.CreateSchemaValidationException(symbol);
                }
            }));
        }

        private KeyValuePair<string, object> ImportSymbols(
          ProcessedManifest processedManifest,
          string manifestName,
          ITracer tracer)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)tracer, nameof(tracer));
            using (tracer.Indent())
            {
                ProcessedManifest processedManifest1 = this.Read(processedManifest.ResolveSymbols(manifestName), tracer, true);
                return processedManifest1 == null ? new KeyValuePair<string, object>() : new KeyValuePair<string, object>((string)null, (object)processedManifest1.Symbols);
            }
        }

        private static void ProcessImages(
          ProcessedManifest processedManifest,
          ImageManifestImage[] images,
          bool preloadResources)
        {
            if (images == null)
                return;
            List<SingleImageBundle> bundles = new List<SingleImageBundle>(images.Length);
            foreach (ImageManifestImage image1 in images)
            {
                ImageManifestImage image = image1;
                ManifestReader.ProcessManifestSection(processedManifest, (Action)(() => bundles.Add(ManifestReader.ProcessImage(processedManifest, image, preloadResources))));
            }
            processedManifest.Images.AddRange(bundles);
            // todo
            //ImageLibrary.InsertRange<ImageBundle>(processedManifest, processedManifest.Images, (IEnumerable<SingleImageBundle>)bundles);
        }

        private static SingleImageBundle ProcessImage(
          ProcessedManifest processedManifest,
          ImageManifestImage image,
          bool preloadResources)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)image, nameof(image));
            return /*(ImageBundle)*/ManifestReader.ProcessManifestElement<ImageManifestImage, SingleImageBundle>(image, (Func<ImageManifestImage, SingleImageBundle>)(i => new SingleImageBundle(ManifestReader.CreateImageMoniker(image, processedManifest), ManifestReader.ProcessSources(processedManifest, image.Source, preloadResources)/*, !image.AllowColorInversionSpecified || image.AllowColorInversion*/)));
        }

        private static ICollection<UriSourceDescriptor> ProcessSources(
          ProcessedManifest processedManifest,
          ImageManifestImageSource[] sources,
          bool preloadResources)
        {
            if (sources == null)
                Enumerable.Empty<UriSourceDescriptor>();
            List<UriSourceDescriptor> descriptors = new List<UriSourceDescriptor>(sources.Length);
            foreach (ImageManifestImageSource source1 in sources)
            {
                ImageManifestImageSource source = source1;
                ManifestReader.ProcessManifestSection(processedManifest, (Action)(() => descriptors.Add(ManifestReader.ProcessSource(processedManifest, source, preloadResources))));
            }
            return descriptors;
        }

        private static UriSourceDescriptor ProcessSource(
          ProcessedManifest processedManifest,
          ImageManifestImageSource source,
          bool preloadResources)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)source, nameof(source));
            return ManifestReader.ProcessManifestElement<ImageManifestImageSource, UriSourceDescriptor>(source, (Func<ImageManifestImageSource, UriSourceDescriptor>)(s =>
            {
                string uriString = processedManifest.ResolveSymbols(source.Uri);
                return new UriSourceDescriptor(uriString);
            }));
        }

        private static void ProcessPackageGuid(ProcessedManifest processedManifest, string packageGuid)
        {
            if (packageGuid == null)
                return;
            try
            {
                packageGuid = processedManifest.ResolveSymbols(packageGuid);
                processedManifest.PackageGuid = Guid.Parse(packageGuid);
            }
            catch (FormatException ex)
            {
                //todo
                throw new ManifestParseException("string.Format(Microsoft.VisualStudio.Imaging.Resources.Error_InvalidPackageGuid, (object)packageGuid)", (Exception)ex);
            }
        }

        private static bool IsCatchableException(Exception ex)
        {
            if (ex == null)
                return true;
            Type type = ex.GetType();
            return type.IsAssignableFrom(typeof(ManifestParseException)) || type.IsAssignableFrom(typeof(XmlException)) || type.IsAssignableFrom(typeof(XmlSchemaValidationException)) || type.IsAssignableFrom(typeof(FileNotFoundException)) || type.IsAssignableFrom(typeof(UnauthorizedAccessException)) || type.IsAssignableFrom(typeof(InvalidOperationException)) && ManifestReader.IsCatchableException(ex.InnerException);
        }

        private static void ProcessManifestSection(ProcessedManifest processedManifest, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (!ManifestReader.IsCatchableException(ex))
                    throw;
                else
                    processedManifest.AddException(ex);
            }
        }

        private static TResult ProcessManifestElement<TElement, TResult>(
          TElement element,
          Func<TElement, TResult> process)
        {
            try
            {
                return process(element);
            }
            catch (Exception ex)
            {
                if (!ManifestReader.IsCatchableException(ex))
                    throw ManifestReader.CreateSchemaValidationException((object)element, ex);
                throw;
            }
        }

        private static ImageMoniker CreateImageMoniker(
          ImageManifestImage image,
          ProcessedManifest processedManifest)
        {
            return ManifestReader.CreateImageMoniker(image.Guid, image.ID, processedManifest);
        }

        private static ImageMoniker CreateImageMoniker(
          string guidString,
          string idString,
          ProcessedManifest processedManifest)
        {
            string input = processedManifest.ResolveSymbols(guidString);
            ImageMoniker imageMoniker;
            imageMoniker.Guid = Guid.Parse(input);
            string s = processedManifest.ResolveSymbols(idString);
            imageMoniker.Id = int.Parse(s);
            return imageMoniker;
        }

        private static XmlSchemaValidationException CreateSchemaValidationException(
          object obj,
          Exception innerException = null)
        {
            return new XmlSchemaValidationException(string.Format("The object doesn't conform to the schema: {0}", (object)ManifestReader.ToString(obj)), innerException);
        }

        private static string ToString(object obj)
        {
            Type type = obj.GetType();
            if (type == typeof(ImageManifestImageListContainedImage))
                return "ContainedImage";
            if (type == typeof(ImageManifestImageSourceDimensions))
                return "Dimensions";
            if (type == typeof(ImageManifestImageSourceDimensionRange))
                return "DimensionRange";
            if (type == typeof(ImageManifestImage))
                return "Image";
            if (type == typeof(ImageManifestImageList))
                return "ImageList";
            if (type == typeof(ImageManifest))
                return "ImageManifest";
            if (type == typeof(ImageManifestImageSourceSize))
                return "Size";
            if (type == typeof(ImageManifestImageSourceSizeRange))
                return "SizeRange";
            if (type == typeof(ImageManifestImageSource))
                return "Source";
            if (type == typeof(ImageManifestImport))
                return "ImportSymbol";
            if (type == typeof(ImageManifestGuid))
                return "GuidSymbol";
            if (type == typeof(ImageManifestID))
                return "IdSymbol";
            if (type == typeof(ImageManifestString))
                return "StringSymbol";
            return obj.ToString();
        }

        private static bool IsBuiltInSymbol(string symbol) => Array.IndexOf<string>(ManifestReader.BuiltInSymbolNames, symbol) >= 0;
    }
}
