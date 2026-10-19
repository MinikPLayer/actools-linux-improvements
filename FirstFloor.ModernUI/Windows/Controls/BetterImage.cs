using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public enum ImageCropMode {
        Relative,
        Absolute
    }

    /// <summary>
    /// Image control which loads files in background and keeps a cache of decoded images. Simplified version of the WPF one
    /// (no cropping, no custom thread pools).
    /// </summary>
    public class BetterImage : Decorator {
        #region Static options (kept from the WPF version, some of them aren’t used anymore)
        public static bool OptionReadFileSync = false;
        public static int OptionDecodeImageSyncThreshold = 30 * 1000;
        public static int OptionMaxSyncDecodedInSecond = 8;
        public static bool OptionEnsureCacheIsFresh = false;
        public static TimeSpan OptionAdditionalDelay = TimeSpan.FromMilliseconds(30);
        public static long OptionCacheTotalSize = 100 * 1024 * 1024;
        public static long OptionCacheMaxSize = 2 * 1024 * 1024;
        public static bool OptionMarkCached = false;
        public static string RemoteUserAgent;
        public static string RemoteCacheDirectory;
        public static object WineDecodeLock;
        #endregion

        #region Wrapper with size
        public struct Image {
            public readonly IImage ImageSource;
            public readonly int Width, Height;
            public readonly bool Downsized;
            public readonly DateTime Date;

            // In bytes
            public int Size => Width * Height * 4;

            public static Image Empty => new Image();

            public Image(IImage source, int width, int height, bool downsized) {
                ImageSource = source;
                Width = width;
                Height = height;
                Downsized = downsized;
                Date = DateTime.Now;
            }

            public Image(IImage source) {
                ImageSource = source;
                Width = (int)(source?.Size.Width ?? 0);
                Height = (int)(source?.Size.Height ?? 0);
                Downsized = true;
                Date = DateTime.Now;
            }

            public bool IsBroken => ImageSource == null;
        }
        #endregion

        #region Cache
        private class CacheEntry {
            public Image Image;
            public DateTime Modified;
            public int DecodeWidth, DecodeHeight;
        }

        private static readonly object CacheLock = new object();
        private static readonly Dictionary<string, CacheEntry> Cache = new Dictionary<string, CacheEntry>();
        private static long _cacheSize;

        public static void CleanUpCache() {
            lock (CacheLock) {
                Cache.Clear();
                _cacheSize = 0;
            }
        }

        public static void Refresh([CanBeNull] string filename) {
            if (filename == null) return;
            lock (CacheLock) {
                var keys = Cache.Keys.Where(x => x.StartsWith(filename + "|")).ToList();
                foreach (var key in keys) {
                    _cacheSize -= Cache[key].Image.Size;
                    Cache.Remove(key);
                }
            }

            Dispatcher.UIThread.Post(() => {
                foreach (var image in Application.Current.Windows.SelectMany(x => x.FindVisualChildren<BetterImage>()).Where(x => x.Filename == filename)) {
                    image.Reload();
                }
            });
        }

        private static void Store(string key, Image image, DateTime modified, int decodeWidth, int decodeHeight) {
            if (image.IsBroken || image.Size > OptionCacheMaxSize) return;
            lock (CacheLock) {
                if (Cache.TryGetValue(key, out var existing)) {
                    _cacheSize -= existing.Image.Size;
                }

                Cache[key] = new CacheEntry { Image = image, Modified = modified, DecodeWidth = decodeWidth, DecodeHeight = decodeHeight };
                _cacheSize += image.Size;

                if (_cacheSize > OptionCacheTotalSize) {
                    foreach (var pair in Cache.OrderBy(x => x.Value.Image.Date).Take(Cache.Count / 2).ToList()) {
                        _cacheSize -= pair.Value.Image.Size;
                        Cache.Remove(pair.Key);
                    }
                }
            }
        }
        #endregion

        #region Loading
        public static MemoryStream ReadFileAsStream(string filename) {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
                var result = new MemoryStream((int)stream.Length);
                stream.CopyTo(result);
                result.Position = 0;
                return result;
            }
        }

        public static Image LoadBitmapSourceFromBytes([NotNull] byte[] data, int decodeWidth = -1, int decodeHeight = -1, int attempt = 0,
                string filename = null) {
            return LoadBitmapSourceFromMemoryStream(new MemoryStream(data), decodeWidth, decodeHeight, attempt, filename);
        }

        public static Image LoadBitmapSourceFromFilename([NotNull] string filename, int decodeWidth = -1, int decodeHeight = -1, int attempt = 0) {
            return LoadBitmapSourceFromMemoryStream(ReadFileAsStream(filename), decodeWidth, decodeHeight, attempt, filename);
        }

        public static Image LoadBitmapSourceFromMemoryStream([NotNull] MemoryStream data, int decodeWidth = -1, int decodeHeight = -1, int attempt = 0,
                string filename = null) {
            try {
                using (data) {
                    var original = new Bitmap(data);
                    var size = original.PixelSize;
                    data.Position = 0;

                    Bitmap result;
                    if (decodeWidth > 0 && size.Width > decodeWidth && (decodeHeight <= 0 || (double)decodeWidth / size.Width <= (double)decodeHeight / size.Height)) {
                        original.Dispose();
                        result = Bitmap.DecodeToWidth(data, decodeWidth);
                        return new Image(result, size.Width, size.Height, true);
                    }

                    if (decodeHeight > 0 && size.Height > decodeHeight) {
                        original.Dispose();
                        result = Bitmap.DecodeToHeight(data, decodeHeight);
                        return new Image(result, size.Width, size.Height, true);
                    }

                    return new Image(original, size.Width, size.Height, false);
                }
            } catch (Exception e) {
                Logging.Warning($"Can’t decode image {filename}: {e.Message}");
                return Image.Empty;
            }
        }

        public static Image LoadBitmapSource([CanBeNull] string filename, int decodeWidth = -1, int decodeHeight = -1) {
            if (string.IsNullOrWhiteSpace(filename)) return Image.Empty;

            DateTime modified;
            try {
                if (!File.Exists(filename)) return Image.Empty;
                modified = File.GetLastWriteTimeUtc(filename);
            } catch {
                return Image.Empty;
            }

            var key = $"{filename}|{decodeWidth}|{decodeHeight}";
            lock (CacheLock) {
                if (Cache.TryGetValue(key, out var entry) && entry.Modified == modified) {
                    return entry.Image;
                }
            }

            Image image;
            try {
                image = LoadBitmapSourceFromFilename(filename, decodeWidth, decodeHeight);
            } catch (Exception e) {
                Logging.Warning(e);
                return Image.Empty;
            }

            Store(key, image, modified, decodeWidth, decodeHeight);
            return image;
        }

        public static Task<Image> LoadBitmapSourceAsync([CanBeNull] string filename, int decodeWidth = -1, int decodeHeight = -1) {
            return Task.Run(() => LoadBitmapSource(filename, decodeWidth, decodeHeight));
        }

        private static HttpClient _httpClient;

        public static async Task<Image> LoadRemoteBitmapAsync(Uri uri, int decodeWidth = -1, int decodeHeight = -1) {
            try {
                if (_httpClient == null) {
                    _httpClient = new HttpClient();
                    if (RemoteUserAgent != null) _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(RemoteUserAgent);
                }

                var data = await _httpClient.GetByteArrayAsync(uri).ConfigureAwait(false);
                return LoadBitmapSourceFromBytes(data, decodeWidth, decodeHeight);
            } catch (Exception e) {
                Logging.Warning(e.Message);
                return Image.Empty;
            }
        }

        /// <summary>
        /// Finds a bounding box (in pixels) of not transparent pixels, null if the whole image is transparent or nothing could be cropped.
        /// </summary>
        [CanBeNull]
        public static unsafe Rect? FindTransparentCropMask([CanBeNull] Bitmap bitmap) {
            if (bitmap == null || bitmap.PixelSize.Width == 0 || bitmap.PixelSize.Height == 0) return null;
            try {
                var w = bitmap.PixelSize.Width;
                var h = bitmap.PixelSize.Height;
                var stride = w * 4;
                var buffer = new byte[stride * h];
                fixed (byte* p = buffer) {
                    bitmap.CopyPixels(new PixelRect(0, 0, w, h), (IntPtr)p, buffer.Length, stride);
                }

                // alpha is the fourth channel for both BGRA and RGBA
                int left = w, top = h, right = -1, bottom = -1;
                for (var y = 0; y < h; y++) {
                    for (var x = 0; x < w; x++) {
                        if (buffer[y * stride + x * 4 + 3] == 0) continue;
                        if (x < left) left = x;
                        if (x > right) right = x;
                        if (y < top) top = y;
                        if (y > bottom) bottom = y;
                    }
                }

                if (right < 0) return null;
                return new Rect(left, top, right - left + 1, bottom - top + 1);
            } catch (Exception e) {
                Logging.Warning(e.Message);
                return null;
            }
        }
        #endregion

        #region Properties
        public static readonly StyledProperty<bool> ShowBrokenProperty = DP.Register<bool>(nameof(ShowBroken), typeof(BetterImage), new PropertyMetadata(false, OnChanged));
        public bool ShowBroken { get => (bool)GetValue(ShowBrokenProperty); set => SetValue(ShowBrokenProperty, value); }

        public static readonly StyledProperty<bool> HideBrokenProperty = DP.Register<bool>(nameof(HideBroken), typeof(BetterImage), new PropertyMetadata(false, OnChanged));
        public bool HideBroken { get => (bool)GetValue(HideBrokenProperty); set => SetValue(HideBrokenProperty, value); }

        public static readonly StyledProperty<bool> ClearOnChangeProperty = DP.Register<bool>(nameof(ClearOnChange), typeof(BetterImage), new PropertyMetadata(false));
        public bool ClearOnChange { get => (bool)GetValue(ClearOnChangeProperty); set => SetValue(ClearOnChangeProperty, value); }

        public static readonly StyledProperty<bool> DelayedCreationProperty = DP.Register<bool>(nameof(DelayedCreation), typeof(BetterImage), new PropertyMetadata(false));
        public bool DelayedCreation { get => (bool)GetValue(DelayedCreationProperty); set => SetValue(DelayedCreationProperty, value); }

        public static readonly StyledProperty<int> DecodeHeightProperty = DP.Register<int>(nameof(DecodeHeight), typeof(BetterImage), new PropertyMetadata(-1, OnChanged));
        public int DecodeHeight { get => (int)GetValue(DecodeHeightProperty); set => SetValue(DecodeHeightProperty, value); }

        public static readonly StyledProperty<int> DecodeWidthProperty = DP.Register<int>(nameof(DecodeWidth), typeof(BetterImage), new PropertyMetadata(-1, OnChanged));
        public int DecodeWidth { get => (int)GetValue(DecodeWidthProperty); set => SetValue(DecodeWidthProperty, value); }

        public static readonly StyledProperty<bool> AsyncDecodeProperty = DP.Register<bool>(nameof(AsyncDecode), typeof(BetterImage), new PropertyMetadata(true));
        public bool AsyncDecode { get => (bool)GetValue(AsyncDecodeProperty); set => SetValue(AsyncDecodeProperty, value); }

        public static readonly StyledProperty<Stretch> StretchProperty = DP.Register<Stretch>(nameof(Stretch), typeof(BetterImage), new PropertyMetadata(Stretch.Uniform, OnStretchChanged));
        public Stretch Stretch { get => (Stretch)GetValue(StretchProperty); set => SetValue(StretchProperty, value); }

        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty = DP.Register<StretchDirection>(nameof(StretchDirection), typeof(BetterImage),
                new PropertyMetadata(StretchDirection.Both, OnStretchChanged));
        public StretchDirection StretchDirection { get => (StretchDirection)GetValue(StretchDirectionProperty); set => SetValue(StretchDirectionProperty, value); }

        public static readonly StyledProperty<string> FilenameProperty = DP.Register<string>(nameof(Filename), typeof(BetterImage), new PropertyMetadata(null, OnChanged));
        public string Filename { get => (string)GetValue(FilenameProperty); set => SetValue(FilenameProperty, value); }

        public static readonly StyledProperty<object> SourceProperty = DP.Register<object>(nameof(Source), typeof(BetterImage), new PropertyMetadata(null, OnChanged));

        /// <summary>
        /// Anything: URI, filename, BitmapSource.
        /// </summary>
        public object Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

        public static readonly StyledProperty<IImage> ImageSourceProperty = DP.Register<IImage>(nameof(ImageSource), typeof(BetterImage), new PropertyMetadata(null, OnChanged));
        public IImage ImageSource { get => (IImage)GetValue(ImageSourceProperty); set => SetValue(ImageSourceProperty, value); }

        public static readonly StyledProperty<Rect?> CropProperty = DP.Register<Rect?>(nameof(Crop), typeof(BetterImage), new PropertyMetadata(null, OnChanged));
        public Rect? Crop { get => (Rect?)GetValue(CropProperty); set => SetValue(CropProperty, value); }

        public static readonly StyledProperty<ImageCropMode> CropUnitsProperty = DP.Register<ImageCropMode>(nameof(CropUnits), typeof(BetterImage));
        public ImageCropMode CropUnits { get => (ImageCropMode)GetValue(CropUnitsProperty); set => SetValue(CropUnitsProperty, value); }

        public static readonly StyledProperty<bool> CropTransparentAreasProperty = DP.Register<bool>(nameof(CropTransparentAreas), typeof(BetterImage), new PropertyMetadata(false, OnChanged));
        public bool CropTransparentAreas { get => (bool)GetValue(CropTransparentAreasProperty); set => SetValue(CropTransparentAreasProperty, value); }

        public static readonly StyledProperty<bool> ForceFillProperty = DP.Register<bool>(nameof(ForceFill), typeof(BetterImage));
        public bool ForceFill { get => (bool)GetValue(ForceFillProperty); set => SetValue(ForceFillProperty, value); }

        public static readonly StyledProperty<bool> CollapseIfMissingProperty = DP.Register<bool>(nameof(CollapseIfMissing), typeof(BetterImage), new PropertyMetadata(false, OnChanged));
        public bool CollapseIfMissing { get => (bool)GetValue(CollapseIfMissingProperty); set => SetValue(CollapseIfMissingProperty, value); }

        public static readonly StyledProperty<bool> CollapseIfNullProperty = DP.Register<bool>(nameof(CollapseIfNull), typeof(BetterImage), new PropertyMetadata(false, OnChanged));
        public bool CollapseIfNull { get => (bool)GetValue(CollapseIfNullProperty); set => SetValue(CollapseIfNullProperty, value); }

        public static readonly StyledProperty<bool> HideIfNullProperty = DP.Register<bool>(nameof(HideIfNull), typeof(BetterImage), new PropertyMetadata(false, OnChanged));
        public bool HideIfNull { get => (bool)GetValue(HideIfNullProperty); set => SetValue(HideIfNullProperty, value); }

        public static readonly StyledProperty<bool> IsBrokenProperty = DP.Register<bool>(nameof(IsBroken), typeof(BetterImage));
        public bool IsBroken { get => (bool)GetValue(IsBrokenProperty); private set => SetValue(IsBrokenProperty, value); }
        #endregion

        private readonly Avalonia.Controls.Image _image;
        private int _version;

        /// <summary>Replaces WPF’s overriding of metadata of RenderOptions.BitmapScalingMode.</summary>
        public static BitmapScalingMode DefaultBitmapScalingMode = BitmapScalingMode.Unspecified;

        public BetterImage() {
            _image = new Avalonia.Controls.Image();
            if (DefaultBitmapScalingMode != BitmapScalingMode.Unspecified) {
                System.Windows.Media.RenderOptions.SetBitmapScalingMode(_image, DefaultBitmapScalingMode);
            }
            Child = _image;
            _image[!Avalonia.Controls.Image.StretchProperty] = this[!StretchProperty];
            _image[!Avalonia.Controls.Image.StretchDirectionProperty] = this[!StretchDirectionProperty];
        }

        private static void OnStretchChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) { }

        private static void OnChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((BetterImage)o).Reload();
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e) {
            base.OnAttachedToVisualTree(e);
            if (_image.Source == null) Reload();
        }

        protected int InnerDecodeWidth => DecodeWidth;

        /// <summary>Sets an already loaded image (for derived controls which load images on their own).</summary>
        protected void SetCurrent(Image image, string filename) {
            Apply(image, ++_version);
        }

        protected virtual void OnFilenameChanged(string filename) {
            Filename = filename;
        }

        public void Reload() {
            var version = ++_version;
            if (ClearOnChange) _image.Source = null;

            var direct = ImageSource ?? Source as IImage;
            if (direct != null) {
                Apply(new Image(direct), version);
                return;
            }

            var filename = Filename ?? (Source is string s ? s : (Source as Uri)?.LocalPath);
            if (filename == null) {
                Apply(Image.Empty, version, true);
                return;
            }

            if (Source is Uri uri && uri.IsAbsoluteUri && (uri.Scheme == "http" || uri.Scheme == "https")) {
                LoadRemoteBitmapAsync(uri, DecodeWidth, DecodeHeight).ContinueWith(t => Dispatcher.UIThread.Post(() => Apply(t.Result, version)));
                return;
            }

            if (Source is Uri avares && avares.IsAbsoluteUri && avares.Scheme == "avares") {
                try {
                    using (var stream = AssetLoader.Open(avares)) {
                        Apply(new Image(new Bitmap(stream)), version);
                    }
                } catch {
                    Apply(Image.Empty, version);
                }
                return;
            }

            var decodeWidth = DecodeWidth;
            var decodeHeight = DecodeHeight;
            if (AsyncDecode) {
                Task.Run(() => LoadBitmapSource(filename, decodeWidth, decodeHeight)).ContinueWith(t => Dispatcher.UIThread.Post(() => Apply(t.Result, version)));
            } else {
                Apply(LoadBitmapSource(filename, decodeWidth, decodeHeight), version);
            }
        }

        [CanBeNull]
        private IImage ApplyCrop([CanBeNull] IImage source) {
            if (!(source is Bitmap bitmap)) return source;

            var crop = Crop;
            if (crop == null && CropTransparentAreas) {
                crop = FindTransparentCropMask(bitmap);
            } else if (crop != null && CropUnits == ImageCropMode.Relative) {
                var size = bitmap.PixelSize;
                crop = new Rect(crop.Value.X * size.Width, crop.Value.Y * size.Height, crop.Value.Width * size.Width, crop.Value.Height * size.Height);
            }

            if (crop == null) return source;
            var rect = new PixelRect((int)crop.Value.X, (int)crop.Value.Y, (int)crop.Value.Width, (int)crop.Value.Height);
            if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0 || rect.Right > bitmap.PixelSize.Width || rect.Bottom > bitmap.PixelSize.Height) {
                return source;
            }

            return new CroppedBitmap(bitmap, rect);
        }


        private void Apply(Image image, int version, bool isNull = false) {
            if (version != _version) return;
            _image.Source = ApplyCrop(image.ImageSource) ?? (ShowBroken ? BrokenIcon.ImageSource : null);
            IsBroken = image.IsBroken && !isNull;
            if (CollapseIfMissing || CollapseIfNull) {
                IsVisible = !image.IsBroken || (ShowBroken && !isNull);
            }

            if (HideBroken || HideIfNull) {
                Opacity = image.IsBroken ? 0d : 1d;
            }
        }
    }
}
