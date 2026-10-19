using System;
using System.Collections.Generic;
using System.Windows;

namespace FirstFloor.ModernUI.Presentation {
    public class SharedResourceDictionary : ResourceDictionary {
        /// <summary>
        /// Internal cache of loaded dictionaries
        /// </summary>
        private static readonly Dictionary<Uri, ResourceDictionary> SharedDictionaries =
                new Dictionary<Uri, ResourceDictionary>();

        private Uri _sourceUri;

        /// <summary>
        /// Gets or sets the uniform resource identifier (URI) to load resources from.
        /// </summary>
        public new Uri Source {
            get => _sourceUri;
            set {
                _sourceUri = value;

                if (SharedDictionaries.TryGetValue(value, out var result)) {
                    // If the dictionary is already loaded, get it from the cache
                    MergedDictionaries.Add(result);
                } else {
                    // If the dictionary is not yet loaded, load it and add it to the cache
                    ((ResourceDictionary)this).Source = value;
                    SharedDictionaries.Add(value, this);
                }
            }
        }

        public static void ClearCache() {
            SharedDictionaries.Clear();
        }
    }
}
