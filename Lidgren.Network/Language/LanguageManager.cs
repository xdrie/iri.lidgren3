using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Lidgren.Network.Language
{
    public static class LanguageManager
    {
        private static DefaultLanguage _default;
        private static ILibraryLanguage _current;

        public static ILibraryLanguage Default
        {
            get
            {
                if (_default == null)
                    _default = new DefaultLanguage();

                return _default;
            }
        }

        public static ILibraryLanguage Current
        {
            get
            {
                if (_current == null)
                    _current = Default;

                return _current;
            }
            set
            {
                _current = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        private class DefaultLanguage : ILibraryLanguage
        {
            private Dictionary<string, string> _pairs;

            public string this[string key] => GetString(key);

            public string Culture => "en_us";
            public IEnumerable<KeyValuePair<string, string>> Pairs => _pairs;

            public DefaultLanguage()
            {
                _pairs = new Dictionary<string, string>();

                Assembly assembly = Assembly.GetExecutingAssembly();
                string name = "Lidgren.Network.Properties.Resources.Strings.resources";
                using (var stream = assembly.GetManifestResourceStream(name))
                using (var reader = new ResourceReader(stream))
                {
                    var enumerator = reader.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        string key = enumerator.Entry.Key as string;
                        string value = enumerator.Entry.Value as string;
                        _pairs.Add(key, value);
                    }
                }
            }

            public string GetString(string key)
            {
                if (_pairs.TryGetValue(key, out string value))
                    return value;

                return $"{Culture}[{key}]";
            }
        }
    }
}
