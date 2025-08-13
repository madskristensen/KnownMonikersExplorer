using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace KnownMonikersExplorer.Xaml.ManifestReading
{
    internal class ProcessedManifest
    {
        private readonly string _filename;
        private readonly ITracer _tracer;
        private readonly Dictionary<string, object> _symbols;
        private readonly Lazy<IReadOnlyDictionary<string, object>> _symbolsRO;
        private readonly List<SingleImageBundle> _images;
        private readonly List<Exception> _exceptions;
        private readonly Lazy<IReadOnlyList<Exception>> _exceptionsRO;
        private readonly Regex _regex = new Regex("(\\$\\(([a-zA-Z_]{1}[0-9a-zA-Z_]*)\\))");

        public ProcessedManifest(string filename, ITracer tracer)
        {
            this._filename = filename;
            this._tracer = tracer ?? Tracer.Null;
            this._symbols = new Dictionary<string, object>();
            this._symbolsRO = new Lazy<IReadOnlyDictionary<string, object>>((() => new ReadOnlyDictionary<string, object>(this._symbols)));
            this._images = new List<SingleImageBundle>();
            this._exceptions = new List<Exception>();
            this._exceptionsRO = new Lazy<IReadOnlyList<Exception>>((() => this._exceptions.AsReadOnly()));
        }

        public Guid PackageGuid { get; set; }

        public string Filename => this._filename;

        public IReadOnlyDictionary<string, object> Symbols => this._symbolsRO.Value;

        public int BuiltInSymbolCount { get; private set; }

        public int UserSymbolCount { get; private set; }

        public int SymbolCount => this.BuiltInSymbolCount + this.UserSymbolCount;

        public List<SingleImageBundle> Images => this._images;

        public IReadOnlyList<Exception> Exceptions => this._exceptionsRO.Value;

        public void AddUserSymbol(string symbol, object value)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNullAndNotWhiteSpace(symbol, nameof(symbol));
            if (value == null)
            {
                value = (object)string.Empty;
                this._tracer.TraceError("AddUserSymbol: Undefined value for symbol {0}", (object)symbol);
            }
            object obj = value;
            if (value is string s)
                obj = (object)this.ResolveSymbols(s);
            this._symbols.Add(symbol, obj);
            ++this.UserSymbolCount;
        }

        public void AddBuiltInSymbol(string symbol, string value)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNullAndNotWhiteSpace(symbol, nameof(symbol));
            if (value == null)
            {
                value = string.Empty;
                this._tracer.TraceError("AddBuiltInSymbol: Undefined value for symbol {0}", (object)symbol);
            }
            this._symbols.Add(symbol, (object)value);
            ++this.BuiltInSymbolCount;
        }

        public string ResolveSymbols(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return s;
            Match match = this._regex.Match(s);
            if (!match.Success)
                return s;
            var resource = new StringBuilder();
            int startIndex = 0;
            for (; match.Success; match = match.NextMatch())
            {
                Group group1 = match.Groups[1];
                Group group2 = match.Groups[2];
                resource.Append(s, startIndex, group1.Index - startIndex);
                string key = group2.Value;
                object obj;
                if (!this._symbols.TryGetValue(key, out obj))
                    //todo
                    throw new ManifestParseException(string.Format("Resources.Error_UndefinedSymbol", (object)key));
                resource.Append(obj.ToString());
                startIndex = group1.Index + group1.Length;
            }
            resource.Append(s, startIndex, s.Length - startIndex);
            return resource.ToString();
            
        }

        public void AddException(Exception ex)
        {
            Microsoft.Internal.VisualStudio.Shell.Validate.IsNotNull((object)ex, nameof(ex));
            this._exceptions.Add(ex);
        }
    }
}
