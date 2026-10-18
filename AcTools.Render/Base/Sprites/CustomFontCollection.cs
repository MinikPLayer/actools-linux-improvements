using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SharpDX;
using SharpDX.DirectWrite;

namespace AcTools.Render.Base.Sprites {
    /// <summary>
    /// Loads custom fonts for DirectWrite from files (it’s not as simple with SharpDX as it used to be with SlimDX).
    /// </summary>
    internal static class CustomFontCollection {
        private static readonly ConditionalWeakTable<FontCollection, FontLoader> Loaders = new ConditionalWeakTable<FontCollection, FontLoader>();

        public static FontCollection Create(Factory factory, string filename) {
            var loader = new FontLoader(factory, new[] { filename });
            var collection = new FontCollection(factory, loader, loader.Keys);
            Loaders.Add(collection, loader);
            return collection;
        }

        private class FontLoader : CallbackBase, FontCollectionLoader, FontFileLoader {
            private readonly List<FontStream> _streams = new List<FontStream>();
            private readonly List<FontEnumerator> _enumerators = new List<FontEnumerator>();
            private readonly Factory _factory;

            public DataStream Keys { get; }

            public FontLoader(Factory factory, string[] files) {
                _factory = factory;

                foreach (var file in files) {
                    var bytes = File.ReadAllBytes(file);
                    var stream = new DataStream(bytes.Length, true, true);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Position = 0;
                    _streams.Add(new FontStream(stream));
                }

                Keys = new DataStream(sizeof(int) * _streams.Count, true, true);
                for (var i = 0; i < _streams.Count; i++) {
                    Keys.Write(i);
                }

                Keys.Position = 0;

                _factory.RegisterFontFileLoader(this);
                _factory.RegisterFontCollectionLoader(this);
            }

            public FontFileEnumerator CreateEnumeratorFromKey(Factory factory, DataPointer collectionKey) {
                var enumerator = new FontEnumerator(factory, this, collectionKey);
                _enumerators.Add(enumerator);
                return enumerator;
            }

            public FontFileStream CreateStreamFromKey(DataPointer fontFileReferenceKey) {
                var index = Utilities.Read<int>(fontFileReferenceKey.Pointer);
                return _streams[index];
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    try {
                        _factory.UnregisterFontCollectionLoader(this);
                        _factory.UnregisterFontFileLoader(this);
                    } catch (Exception) {
                        // ignored
                    }

                    foreach (var enumerator in _enumerators) {
                        enumerator.Dispose();
                    }

                    foreach (var stream in _streams) {
                        stream.Dispose();
                    }

                    Keys.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private class FontEnumerator : CallbackBase, FontFileEnumerator {
            private readonly Factory _factory;
            private readonly FontFileLoader _loader;
            private readonly DataStream _keyStream;
            private FontFile _current;

            public FontEnumerator(Factory factory, FontFileLoader loader, DataPointer key) {
                _factory = factory;
                _loader = loader;
                _keyStream = new DataStream(key.Pointer, key.Size, true, false);
            }

            public FontFile CurrentFontFile => _current.QueryInterface<FontFile>();

            public bool MoveNext() {
                var result = _keyStream.RemainingLength != 0;
                if (result) {
                    _current?.Dispose();
                    _current = new FontFile(_factory, _keyStream.PositionPointer, sizeof(int), _loader);
                    _keyStream.Position += sizeof(int);
                }

                return result;
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _current?.Dispose();
                    _keyStream.Dispose();
                }

                base.Dispose(disposing);
            }
        }

        private class FontStream : CallbackBase, FontFileStream {
            private readonly DataStream _stream;

            public FontStream(DataStream stream) {
                _stream = stream;
            }

            public void ReadFileFragment(out IntPtr fragmentStart, long fileOffset, long fragmentSize, out IntPtr fragmentContext) {
                lock (this) {
                    fragmentContext = IntPtr.Zero;
                    _stream.Position = fileOffset;
                    fragmentStart = _stream.PositionPointer;
                }
            }

            public void ReleaseFileFragment(IntPtr fragmentContext) { }

            public long GetFileSize() {
                return _stream.Length;
            }

            public long GetLastWriteTime() {
                return 0;
            }

            protected override void Dispose(bool disposing) {
                if (disposing) {
                    _stream.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
