using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FirstFloor.ModernUI.Presentation;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// Avalonia registers x:Name only for controls, but WPF code-behind also refers to named non-visual objects (links, groups and so on).
    /// Such objects are located by walking through the properties of the XAML root and its descendants.
    /// </summary>
    public static class NamedObjects {
        private static readonly Dictionary<Type, PropertyInfo[]> Properties = new Dictionary<Type, PropertyInfo[]>();

        public static object Find(object root, string name) {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var queue = new Queue<object>();
            queue.Enqueue(root);

            var roots = new List<object>();
            while (queue.Count > 0) {
                var item = queue.Dequeue();
                if (item == null || !visited.Add(item)) continue;

                if (item is Displayable d && d.Name == name) return d;

                if (item is StyledElement element) {
                    foreach (var child in element.GetLogicalDescendants()) {
                        queue.Enqueue(child);
                    }

                    if (element is Visual visual) {
                        foreach (var child in visual.GetVisualDescendants()) {
                            queue.Enqueue(child);
                        }
                    }
                }

                foreach (var property in GetProperties(item.GetType())) {
                    object value;
                    try {
                        value = property.GetValue(item);
                    } catch (Exception) {
                        continue;
                    }

                    if (value is Displayable || value is IEnumerable && !(value is string) && IsOfInterest(value)) {
                        queue.Enqueue(value);
                        if (value is IEnumerable enumerable && !(value is Displayable)) {
                            foreach (var inner in enumerable) {
                                queue.Enqueue(inner);
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static bool IsOfInterest(object value) {
            var type = value.GetType();
            return type.IsGenericType && type.GetGenericArguments().Any(x => typeof(Displayable).IsAssignableFrom(x))
                    || typeof(Displayable).IsAssignableFrom(type.BaseType?.IsGenericType == true ? type.BaseType.GetGenericArguments().FirstOrDefault() : null);
        }

        private static PropertyInfo[] GetProperties(Type type) {
            lock (Properties) {
                if (!Properties.TryGetValue(type, out var result)) {
                    var isDisplayable = typeof(Displayable).IsAssignableFrom(type);
                    result = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(x => x.GetIndexParameters().Length == 0 && x.CanRead
                                    && (typeof(Displayable).IsAssignableFrom(x.PropertyType)
                                            || x.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(x.PropertyType)
                                                    && (isDisplayable || typeof(StyledElement).IsAssignableFrom(type) && !x.PropertyType.IsArray)))
                            .ToArray();
                    Properties[type] = result;
                }

                return result;
            }
        }
    }
}
