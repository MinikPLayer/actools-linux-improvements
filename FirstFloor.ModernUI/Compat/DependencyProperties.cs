using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Reactive;

// ReSharper disable once CheckNamespace
namespace System.Windows {
    public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);

    public delegate object CoerceValueCallback(DependencyObject d, object baseValue);

    public delegate bool ValidateValueCallback(object value);

    [Flags]
    public enum FrameworkPropertyMetadataOptions {
        None = 0,
        AffectsMeasure = 1,
        AffectsArrange = 2,
        AffectsParentMeasure = 4,
        AffectsParentArrange = 8,
        AffectsRender = 16,
        Inherits = 32,
        OverridesInheritanceBehavior = 64,
        NotDataBindable = 128,
        BindsTwoWayByDefault = 256,
        Journal = 1024,
        SubPropertiesDoNotAffectRender = 2048
    }

    /// <summary>
    /// Mimics WPF’s PropertyChangedEventArgs for dependency properties on top of Avalonia’s properties.
    /// </summary>
    public readonly struct DependencyPropertyChangedEventArgs {
        public DependencyPropertyChangedEventArgs(DependencyProperty property, object oldValue, object newValue) {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public DependencyProperty Property { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        internal static DependencyPropertyChangedEventArgs From(AvaloniaPropertyChangedEventArgs e) {
            return new DependencyPropertyChangedEventArgs(e.Property, e.GetOldValue<object>(), e.GetNewValue<object>());
        }
    }

    public class PropertyMetadata {
        public PropertyMetadata() { }

        public PropertyMetadata(object defaultValue) {
            DefaultValue = defaultValue;
        }

        public PropertyMetadata(PropertyChangedCallback propertyChangedCallback) {
            PropertyChangedCallback = propertyChangedCallback;
        }

        public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback) {
            DefaultValue = defaultValue;
            PropertyChangedCallback = propertyChangedCallback;
        }

        public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) {
            DefaultValue = defaultValue;
            PropertyChangedCallback = propertyChangedCallback;
            CoerceValueCallback = coerceValueCallback;
        }

        public object DefaultValue { get; set; }
        public PropertyChangedCallback PropertyChangedCallback { get; set; }
        public CoerceValueCallback CoerceValueCallback { get; set; }
    }

    public class FrameworkPropertyMetadata : PropertyMetadata {
        public FrameworkPropertyMetadata() { }

        public FrameworkPropertyMetadata(object defaultValue) : base(defaultValue) { }

        public FrameworkPropertyMetadata(PropertyChangedCallback propertyChangedCallback) : base(propertyChangedCallback) { }

        public FrameworkPropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback) : base(defaultValue, propertyChangedCallback) { }

        public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags) : base(defaultValue) {
            Options = flags;
        }

        public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback)
                : base(defaultValue, propertyChangedCallback) {
            Options = flags;
        }

        public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback,
                CoerceValueCallback coerceValueCallback) : base(defaultValue, propertyChangedCallback, coerceValueCallback) {
            Options = flags;
        }

        public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback,
                CoerceValueCallback coerceValueCallback, bool isAnimationProhibited) : base(defaultValue, propertyChangedCallback, coerceValueCallback) {
            Options = flags;
        }

        public FrameworkPropertyMetadataOptions Options { get; set; }

        public bool BindsTwoWayByDefault {
            get => Options.HasFlag(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault);
            set => Options = value ? Options | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault : Options & ~FrameworkPropertyMetadataOptions.BindsTwoWayByDefault;
        }
    }

    /// <summary>
    /// Read-only properties in WPF come with a key for setting them. In here it’s just a container for a regular property.
    /// </summary>
    public sealed class DependencyPropertyKey {
        public DependencyPropertyKey(AvaloniaProperty property) {
            DependencyProperty = property;
        }

        public DependencyProperty DependencyProperty { get; }
    }

    /// <summary>
    /// Replacement for WPF’s DependencyProperty.Register family, creates Avalonia’s styled properties.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class DP {
        private static readonly MethodInfo RegisterMethod = typeof(DP).GetMethod(nameof(RegisterCore), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo RegisterAttachedMethod = typeof(DP).GetMethod(nameof(RegisterAttachedCore), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo AddOwnerMethod = typeof(DP).GetMethod(nameof(AddOwnerCore), BindingFlags.NonPublic | BindingFlags.Static);

        public static StyledProperty<T> Register<T>(string name, Type ownerType, PropertyMetadata metadata = null, ValidateValueCallback validate = null) {
            return (StyledProperty<T>)Register(name, typeof(T), ownerType, metadata, validate);
        }

        public static AttachedProperty<T> RegisterAttached<T>(string name, Type ownerType, PropertyMetadata metadata = null, ValidateValueCallback validate = null) {
            return (AttachedProperty<T>)RegisterAttached(name, typeof(T), ownerType, metadata, validate);
        }

        public static DependencyProperty Register(string name, Type propertyType, Type ownerType) {
            return Register(name, propertyType, ownerType, null);
        }

        public static DependencyProperty Register(string name, Type propertyType, Type ownerType, PropertyMetadata metadata, ValidateValueCallback validate = null) {
            try {
                return (DependencyProperty)RegisterMethod.MakeGenericMethod(ownerType, propertyType).Invoke(null, new object[] { name, metadata, validate });
            } catch (TargetInvocationException e) when (e.InnerException != null) {
                throw new Exception($"Can’t register property {ownerType.Name}.{name}: {e.InnerException.Message}", e.InnerException);
            }
        }

        public static DependencyPropertyKey RegisterReadOnly(string name, Type propertyType, Type ownerType, PropertyMetadata metadata) {
            return new DependencyPropertyKey(Register(name, propertyType, ownerType, metadata));
        }

        public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType) {
            return RegisterAttached(name, propertyType, ownerType, null);
        }

        public static DependencyProperty RegisterAttached(string name, Type propertyType, Type ownerType, PropertyMetadata metadata, ValidateValueCallback validate = null) {
            try {
                return (DependencyProperty)RegisterAttachedMethod.MakeGenericMethod(ownerType, propertyType).Invoke(null, new object[] { name, metadata, validate });
            } catch (TargetInvocationException e) when (e.InnerException != null) {
                throw new Exception($"Can’t register attached property {ownerType.Name}.{name}: {e.InnerException.Message}", e.InnerException);
            }
        }

        public static DependencyPropertyKey RegisterAttachedReadOnly(string name, Type propertyType, Type ownerType, PropertyMetadata metadata) {
            return new DependencyPropertyKey(RegisterAttached(name, propertyType, ownerType, metadata));
        }

        public static DependencyProperty AddOwner(DependencyProperty property, Type ownerType) {
            return (DependencyProperty)AddOwnerMethod.MakeGenericMethod(ownerType, property.PropertyType).Invoke(null, new object[] { property });
        }

        public static DependencyProperty AddOwner(DependencyProperty property, Type ownerType, PropertyMetadata metadata) {
            var result = AddOwner(property, ownerType);
            if (metadata?.PropertyChangedCallback != null) {
                result.Changed.Subscribe(args => {
                    if (args.Sender != null && ownerType.IsInstanceOfType(args.Sender)) {
                        metadata.PropertyChangedCallback(args.Sender, DependencyPropertyChangedEventArgs.From(args));
                    }
                });
            }
            return result;
        }

        public static void OverrideMetadata(DependencyProperty property, Type ownerType, PropertyMetadata metadata) {
            var method = typeof(DP).GetMethod(nameof(OverrideMetadataCore), BindingFlags.NonPublic | BindingFlags.Static);
            method.MakeGenericMethod(ownerType, property.PropertyType).Invoke(null, new object[] { property, metadata });
        }

        private static void OverrideMetadataCore<TOwner, TValue>(AvaloniaProperty property, PropertyMetadata metadata) where TOwner : AvaloniaObject {
            if (metadata?.DefaultValue != null && property is StyledProperty<TValue> styled) {
                styled.OverrideDefaultValue<TOwner>(Cast<TValue>(metadata.DefaultValue));
            }

            if (metadata?.PropertyChangedCallback != null) {
                property.Changed.Subscribe(args => {
                    if (args.Sender is TOwner) {
                        metadata.PropertyChangedCallback(args.Sender, DependencyPropertyChangedEventArgs.From(args));
                    }
                });
            }
        }

        internal static T Cast<T>(object value) {
            if (value == null) return default;
            if (value is T t) return t;
            var type = typeof(T);
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying.IsEnum) return (T)Enum.ToObject(underlying, value);
            return (T)Convert.ChangeType(value, underlying, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static StyledProperty<TValue> RegisterCore<TOwner, TValue>(string name, PropertyMetadata metadata, ValidateValueCallback validate) where TOwner : AvaloniaObject {
            var options = (metadata as FrameworkPropertyMetadata)?.Options ?? FrameworkPropertyMetadataOptions.None;
            var defaultValue = metadata?.DefaultValue != null ? Cast<TValue>(metadata.DefaultValue) : default;
            var mode = options.HasFlag(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault) ? BindingMode.TwoWay : BindingMode.OneWay;

            Func<TValue, bool> validateFn = validate == null ? null : v => validate(v);
            Func<AvaloniaObject, TValue, TValue> coerce = metadata?.CoerceValueCallback == null ? null : (o, v) => Cast<TValue>(metadata.CoerceValueCallback(o, v));

            var property = AvaloniaProperty.Register<TOwner, TValue>(name, defaultValue, options.HasFlag(FrameworkPropertyMetadataOptions.Inherits), mode, validateFn, coerce);
            Setup<TOwner>(property, metadata, options);
            return property;
        }

        private static AttachedProperty<TValue> RegisterAttachedCore<TOwner, TValue>(string name, PropertyMetadata metadata, ValidateValueCallback validate) {
            var options = (metadata as FrameworkPropertyMetadata)?.Options ?? FrameworkPropertyMetadataOptions.None;
            var defaultValue = metadata?.DefaultValue != null ? Cast<TValue>(metadata.DefaultValue) : default;
            var mode = options.HasFlag(FrameworkPropertyMetadataOptions.BindsTwoWayByDefault) ? BindingMode.TwoWay : BindingMode.OneWay;

            Func<TValue, bool> validateFn = validate == null ? null : v => validate(v);
            Func<AvaloniaObject, TValue, TValue> coerce = metadata?.CoerceValueCallback == null ? null : (o, v) => Cast<TValue>(metadata.CoerceValueCallback(o, v));

            var property = AvaloniaProperty.RegisterAttached<TOwner, AvaloniaObject, TValue>(name, defaultValue, options.HasFlag(FrameworkPropertyMetadataOptions.Inherits), mode, validateFn, coerce);
            Setup<AvaloniaObject>(property, metadata, options);
            return property;
        }

        private static StyledProperty<TValue> AddOwnerCore<TOwner, TValue>(AvaloniaProperty source) where TOwner : AvaloniaObject {
            return ((StyledProperty<TValue>)source).AddOwner<TOwner>();
        }

        private static void Setup<TOwner>(AvaloniaProperty property, PropertyMetadata metadata, FrameworkPropertyMetadataOptions options) {
            if (options.HasFlag(FrameworkPropertyMetadataOptions.AffectsMeasure)) {
                Affect(property, typeof(Layoutable), nameof(Layoutable.InvalidateMeasure));
            }

            if (options.HasFlag(FrameworkPropertyMetadataOptions.AffectsArrange)) {
                Affect(property, typeof(Layoutable), nameof(Layoutable.InvalidateArrange));
            }

            if (options.HasFlag(FrameworkPropertyMetadataOptions.AffectsRender)) {
                Affect(property, typeof(Visual), nameof(Visual.InvalidateVisual));
            }

            if (options.HasFlag(FrameworkPropertyMetadataOptions.AffectsParentMeasure)) {
                property.Changed.Subscribe(args => {
                    if ((args.Sender as Layoutable)?.Parent is Layoutable p) Invalidate(p, true);
                });
            }

            if (options.HasFlag(FrameworkPropertyMetadataOptions.AffectsParentArrange)) {
                property.Changed.Subscribe(args => {
                    if ((args.Sender as Layoutable)?.Parent is Layoutable p) Invalidate(p, false);
                });
            }

            if (metadata?.PropertyChangedCallback != null) {
                var callback = metadata.PropertyChangedCallback;
                property.Changed.Subscribe(args => {
                    if (args.Sender is TOwner) {
                        callback(args.Sender, DependencyPropertyChangedEventArgs.From(args));
                    }
                });
            }
        }

        private static bool Invalidate(Layoutable p, bool measure) {
            if (measure) p.InvalidateMeasure();
            else p.InvalidateArrange();
            return true;
        }

        private static void Affect(AvaloniaProperty property, Type target, string method) {
            property.Changed.Subscribe(args => {
                if (args.Sender is Layoutable l && target == typeof(Layoutable)) {
                    if (method == nameof(Layoutable.InvalidateMeasure)) l.InvalidateMeasure();
                    else l.InvalidateArrange();
                } else if (args.Sender is Visual v) {
                    v.InvalidateVisual();
                }
            });
        }
    }

    /// <summary>
    /// Extension methods for the WPF-like access to properties and values.
    /// </summary>
    public static class DependencyPropertyExtension {
        public static void SetValue(this DependencyObject o, DependencyPropertyKey key, object value) {
            o.SetValue(key.DependencyProperty, value);
        }

        public static void SetCurrentValue(this DependencyObject o, DependencyPropertyKey key, object value) {
            o.SetCurrentValue(key.DependencyProperty, value);
        }

        public static object GetValue(this DependencyObject o, DependencyPropertyKey key) {
            return o.GetValue(key.DependencyProperty);
        }

        public static object ReadLocalValue(this DependencyObject o, DependencyProperty property) {
            return o.IsSet(property) ? o.GetValue(property) : AvaloniaProperty.UnsetValue;
        }

        public static Type GetPropertyType(this DependencyProperty property) {
            return property.PropertyType;
        }
    }
}

namespace System.Windows {
    using Avalonia;

    public static class DPTyped {
    }
}
