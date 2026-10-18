using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using AcTools.Utils.Helpers;
using SharpDX;
using SharpDX.Direct3D11;
using AcTools.Render.Base.Utils;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Rectangle = System.Drawing.Rectangle;
using Resource = SharpDX.Direct3D11.Resource;

namespace AcTools.Render.Base {
    /// <summary>
    /// Image file formats for <see cref="D3DX11Compat.SaveTexture2D(DeviceContext, Texture2D, ImageFileFormat, Stream)"/>.
    /// Used to be SlimDX.Direct3D11.ImageFileFormat.
    /// </summary>
    public enum ImageFileFormat {
        Bmp,
        Jpg,
        Png,
        Dds,
        Tiff,
        Gif,
        Wmp
    }

    /// <summary>
    /// SlimDX used D3DX11 to load and save textures. D3DX11 is deprecated and not available in SharpDX, so
    /// here is a replacement: DDS files are parsed and written manually, other formats are going through
    /// System.Drawing.
    /// </summary>
    public static unsafe class D3DX11Compat {
        #region Loading
        public static Texture2D Texture2DFromMemory(Device device, byte[] data) {
            return (Texture2D)LoadResource(device, data, false);
        }

        public static Texture3D Texture3DFromMemory(Device device, byte[] data) {
            return (Texture3D)LoadResource(device, data, true);
        }

        public static ShaderResourceView ShaderResourceViewFromMemory(Device device, byte[] data) {
            var resource = LoadResource(device, data, false);
            try {
                return new ShaderResourceView(device, resource);
            } finally {
                // View is holding its own reference
                resource.Dispose();
            }
        }

        public static ShaderResourceView ShaderResourceViewFromFile(Device device, string filename) {
            return ShaderResourceViewFromMemory(device, File.ReadAllBytes(filename));
        }

        public static Texture2D Texture2DFromFile(Device device, string filename) {
            return Texture2DFromMemory(device, File.ReadAllBytes(filename));
        }

        private static Resource LoadResource(Device device, byte[] data, bool expectVolume) {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length > 4 && data[0] == 'D' && data[1] == 'D' && data[2] == 'S' && data[3] == ' ') {
                return LoadDds(device, data);
            }

            return LoadWithSystemDrawing(device, data);
        }

        private static readonly Format[] BlockCompressed = {
            Format.BC1_Typeless, Format.BC1_UNorm, Format.BC1_UNorm_SRgb,
            Format.BC2_Typeless, Format.BC2_UNorm, Format.BC2_UNorm_SRgb,
            Format.BC3_Typeless, Format.BC3_UNorm, Format.BC3_UNorm_SRgb,
            Format.BC4_Typeless, Format.BC4_UNorm, Format.BC4_SNorm,
            Format.BC5_Typeless, Format.BC5_UNorm, Format.BC5_SNorm,
            Format.BC6H_Typeless, Format.BC6H_Uf16, Format.BC6H_Sf16,
            Format.BC7_Typeless, Format.BC7_UNorm, Format.BC7_UNorm_SRgb,
        };

        private static bool IsBlockCompressed(Format f) {
            return Array.IndexOf(BlockCompressed, f) >= 0;
        }

        private static int BlockSize(Format f) {
            switch (f) {
                case Format.BC1_Typeless:
                case Format.BC1_UNorm:
                case Format.BC1_UNorm_SRgb:
                case Format.BC4_Typeless:
                case Format.BC4_UNorm:
                case Format.BC4_SNorm:
                    return 8;
                default:
                    return 16;
            }
        }

        private static int BitsPerPixel(Format f) {
            switch (f) {
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return 128;
                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return 96;
                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                case Format.R32G8X24_Typeless:
                case Format.D32_Float_S8X24_UInt:
                case Format.R32_Float_X8X24_Typeless:
                case Format.X32_Typeless_G8X24_UInt:
                    return 64;
                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                case Format.R11G11B10_Float:
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                case Format.X24_Typeless_G8_UInt:
                case Format.R9G9B9E5_Sharedexp:
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8X8_UNorm:
                case Format.R10G10B10_Xr_Bias_A2_UNorm:
                case Format.B8G8R8A8_Typeless:
                case Format.B8G8R8A8_UNorm_SRgb:
                case Format.B8G8R8X8_Typeless:
                case Format.B8G8R8X8_UNorm_SRgb:
                    return 32;
                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_UInt:
                case Format.R16_SNorm:
                case Format.R16_SInt:
                case Format.B5G6R5_UNorm:
                case Format.B5G5R5A1_UNorm:
                case Format.B4G4R4A4_UNorm:
                    return 16;
                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                case Format.A8_UNorm:
                    return 8;
                case Format.R1_UNorm:
                    return 1;
                default:
                    throw new NotSupportedException($"Format {f} is not supported");
            }
        }

        private static void GetSurfaceInfo(int width, int height, Format format, out int rowPitch, out int slicePitch, out int rows) {
            if (IsBlockCompressed(format)) {
                var blocksWide = Math.Max(1, (width + 3) / 4);
                var blocksHigh = Math.Max(1, (height + 3) / 4);
                rowPitch = blocksWide * BlockSize(format);
                rows = blocksHigh;
                slicePitch = rowPitch * blocksHigh;
            } else {
                rowPitch = (width * BitsPerPixel(format) + 7) / 8;
                rows = height;
                slicePitch = rowPitch * height;
            }
        }

        private static uint ReadU32(byte[] data, int offset) {
            return BitConverter.ToUInt32(data, offset);
        }

        private const uint DdpfAlphaPixels = 0x1;
        private const uint DdpfAlpha = 0x2;
        private const uint DdpfFourCc = 0x4;
        private const uint DdpfRgb = 0x40;
        private const uint DdpfLuminance = 0x20000;

        private static uint FourCc(char a, char b, char c, char d) {
            return a | ((uint)b << 8) | ((uint)c << 16) | ((uint)d << 24);
        }

        /// <summary>
        /// Converts legacy DDS pixel format to DXGI format. If conversion is required (formats unsupported by DXGI), returns
        /// Format.Unknown and sets <paramref name="convertTo32"/>.
        /// </summary>
        private static Format GetDxgiFormat(byte[] data, int pfOffset, out bool convertTo32) {
            convertTo32 = false;

            var flags = ReadU32(data, pfOffset + 4);
            var fourCc = ReadU32(data, pfOffset + 8);
            var bits = ReadU32(data, pfOffset + 12);
            var rMask = ReadU32(data, pfOffset + 16);
            var gMask = ReadU32(data, pfOffset + 20);
            var bMask = ReadU32(data, pfOffset + 24);
            var aMask = ReadU32(data, pfOffset + 28);

            if ((flags & DdpfRgb) != 0) {
                switch (bits) {
                    case 32:
                        if (rMask == 0x000000ff && gMask == 0x0000ff00 && bMask == 0x00ff0000 && aMask == 0xff000000) return Format.R8G8B8A8_UNorm;
                        if (rMask == 0x000000ff && gMask == 0x0000ff00 && bMask == 0x00ff0000 && aMask == 0) {
                            convertTo32 = true;
                            return Format.Unknown;
                        }

                        if (rMask == 0x00ff0000 && gMask == 0x0000ff00 && bMask == 0x000000ff && aMask == 0xff000000) return Format.B8G8R8A8_UNorm;
                        if (rMask == 0x00ff0000 && gMask == 0x0000ff00 && bMask == 0x000000ff && aMask == 0) return Format.B8G8R8X8_UNorm;
                        if (rMask == 0x0000ffff && gMask == 0xffff0000 && bMask == 0 && aMask == 0) return Format.R16G16_UNorm;
                        if (rMask == 0x3ff00000 && gMask == 0x000ffc00 && bMask == 0x000003ff && aMask == 0xc0000000) {
                            convertTo32 = true;
                            return Format.Unknown;
                        }

                        if (rMask == 0x000003ff && gMask == 0x000ffc00 && bMask == 0x3ff00000 && aMask == 0xc0000000) return Format.R10G10B10A2_UNorm;
                        if (rMask == 0xffffffff && gMask == 0 && bMask == 0 && aMask == 0) return Format.R32_Float;
                        break;
                    case 24:
                        convertTo32 = true;
                        return Format.Unknown;
                    case 16:
                        if (rMask == 0xf800 && gMask == 0x07e0 && bMask == 0x001f && aMask == 0) return Format.B5G6R5_UNorm;
                        if (rMask == 0x7c00 && gMask == 0x03e0 && bMask == 0x001f && aMask == 0x8000) return Format.B5G5R5A1_UNorm;
                        convertTo32 = true;
                        return Format.Unknown;
                }
            } else if ((flags & DdpfLuminance) != 0) {
                if (bits == 8 && rMask == 0xff) return Format.R8_UNorm;
                if (bits == 16 && rMask == 0xffff) return Format.R16_UNorm;
                if (bits == 16 && rMask == 0x00ff && aMask == 0xff00) return Format.R8G8_UNorm;
            } else if ((flags & DdpfAlpha) != 0) {
                if (bits == 8) return Format.A8_UNorm;
            } else if ((flags & DdpfFourCc) != 0) {
                if (fourCc == FourCc('D', 'X', 'T', '1')) return Format.BC1_UNorm;
                if (fourCc == FourCc('D', 'X', 'T', '2') || fourCc == FourCc('D', 'X', 'T', '3')) return Format.BC2_UNorm;
                if (fourCc == FourCc('D', 'X', 'T', '4') || fourCc == FourCc('D', 'X', 'T', '5')) return Format.BC3_UNorm;
                if (fourCc == FourCc('A', 'T', 'I', '1') || fourCc == FourCc('B', 'C', '4', 'U')) return Format.BC4_UNorm;
                if (fourCc == FourCc('B', 'C', '4', 'S')) return Format.BC4_SNorm;
                if (fourCc == FourCc('A', 'T', 'I', '2') || fourCc == FourCc('B', 'C', '5', 'U')) return Format.BC5_UNorm;
                if (fourCc == FourCc('B', 'C', '5', 'S')) return Format.BC5_SNorm;
                switch (fourCc) {
                    case 36: return Format.R16G16B16A16_UNorm;
                    case 110: return Format.R16G16B16A16_SNorm;
                    case 111: return Format.R16_Float;
                    case 112: return Format.R16G16_Float;
                    case 113: return Format.R16G16B16A16_Float;
                    case 114: return Format.R32_Float;
                    case 115: return Format.R32G32_Float;
                    case 116: return Format.R32G32B32A32_Float;
                }
            }

            throw new NotSupportedException("Unsupported DDS pixel format");
        }

        /// <summary>
        /// Reads a pixel from legacy-format surface and returns it as RGBA32.
        /// </summary>
        private static uint ReadLegacyPixel(byte* src, int bits, uint rMask, uint gMask, uint bMask, uint aMask) {
            uint value;
            switch (bits) {
                case 32:
                    value = *(uint*)src;
                    break;
                case 24:
                    value = src[0] | ((uint)src[1] << 8) | ((uint)src[2] << 16);
                    break;
                default:
                    value = *(ushort*)src;
                    break;
            }

            return ExtractChannel(value, rMask, false)
                    | (ExtractChannel(value, gMask, false) << 8)
                    | (ExtractChannel(value, bMask, false) << 16)
                    | (ExtractChannel(value, aMask, true) << 24);
        }

        private static uint ExtractChannel(uint value, uint mask, bool defaultMax) {
            if (mask == 0) return defaultMax ? 255u : 0u;

            var shift = 0;
            while (((mask >> shift) & 1) == 0) shift++;
            var max = mask >> shift;
            var v = (value & mask) >> shift;
            return max == 255 ? v : (uint)((v * 255 + max / 2) / max);
        }

        private static Resource LoadDds(Device device, byte[] data) {
            if (data.Length < 128) throw new InvalidDataException("DDS file is too small");

            var headerSize = ReadU32(data, 4);
            if (headerSize != 124) throw new InvalidDataException("Invalid DDS header");

            var height = (int)ReadU32(data, 12);
            var width = (int)ReadU32(data, 16);
            var depth = (int)ReadU32(data, 24);
            var mipCount = (int)ReadU32(data, 28);
            var caps2 = ReadU32(data, 112);
            var pfOffset = 76;
            var dataOffset = 128;

            var flags = ReadU32(data, pfOffset + 4);
            var fourCc = ReadU32(data, pfOffset + 8);

            if (mipCount == 0) mipCount = 1;

            Format format;
            var convertTo32 = false;
            var arraySize = 1;
            var isCube = false;
            var isVolume = false;

            if ((flags & DdpfFourCc) != 0 && fourCc == FourCc('D', 'X', '1', '0')) {
                if (data.Length < 148) throw new InvalidDataException("DDS file is too small");
                format = (Format)ReadU32(data, 128);
                var dimension = ReadU32(data, 132);
                var misc = ReadU32(data, 136);
                arraySize = (int)ReadU32(data, 140);
                if (arraySize == 0) arraySize = 1;
                dataOffset = 148;

                if (dimension == 4) {
                    isVolume = true;
                } else if ((misc & 0x4) != 0) {
                    isCube = true;
                    arraySize *= 6;
                }
            } else {
                format = GetDxgiFormat(data, pfOffset, out convertTo32);
                if ((caps2 & 0x200000) != 0 && depth > 1) {
                    isVolume = true;
                } else if ((caps2 & 0x200) != 0) {
                    isCube = true;
                    arraySize = 6;
                }
            }

            if (depth < 1) depth = 1;
            if (height < 1) height = 1;
            if (width < 1) width = 1;

            var bitsLegacy = (int)ReadU32(data, pfOffset + 12);
            var rMask = ReadU32(data, pfOffset + 16);
            var gMask = ReadU32(data, pfOffset + 20);
            var bMask = ReadU32(data, pfOffset + 24);
            var aMask = ReadU32(data, pfOffset + 28);

            var actualFormat = convertTo32 ? Format.R8G8B8A8_UNorm : format;

            // Collecting subresources
            var count = isVolume ? mipCount : mipCount * arraySize;
            var pointers = new IntPtr[count];
            var pitches = new int[count];
            var slices = new int[count];

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            byte[] converted = null;
            GCHandle convertedHandle = default;

            try {
                var basePtr = handle.AddrOfPinnedObject();
                var offset = dataOffset;

                // Calculating converted size first
                var convertedOffset = 0;
                if (convertTo32) {
                    long total = 0;
                    for (var a = 0; a < (isVolume ? 1 : arraySize); a++) {
                        var w = width;
                        var h = height;
                        var d = depth;
                        for (var m = 0; m < mipCount; m++) {
                            total += (long)w * h * 4 * (isVolume ? d : 1);
                            w = Math.Max(1, w / 2);
                            h = Math.Max(1, h / 2);
                            d = Math.Max(1, d / 2);
                        }
                    }

                    converted = new byte[total];
                    convertedHandle = GCHandle.Alloc(converted, GCHandleType.Pinned);
                }

                var index = 0;
                for (var a = 0; a < (isVolume ? 1 : arraySize); a++) {
                    var w = width;
                    var h = height;
                    var d = isVolume ? depth : 1;
                    for (var m = 0; m < mipCount; m++) {
                        int rowPitch, slicePitch, rows;
                        if (convertTo32) {
                            var srcBytes = Math.Max(1, bitsLegacy / 8);
                            var srcRowPitch = (w * bitsLegacy + 7) / 8;
                            var srcSlicePitch = srcRowPitch * h;
                            var srcTotal = (long)srcSlicePitch * d;
                            if (offset + srcTotal > data.Length) throw new InvalidDataException("DDS file is truncated");

                            var dstRowPitch = w * 4;
                            var dstSlicePitch = dstRowPitch * h;
                            fixed (byte* srcBase = &data[offset])
                            fixed (byte* dstBase = &converted[convertedOffset]) {
                                for (var z = 0; z < d; z++) {
                                    for (var y = 0; y < h; y++) {
                                        var s = srcBase + z * srcSlicePitch + y * srcRowPitch;
                                        var t = (uint*)(dstBase + z * dstSlicePitch + y * dstRowPitch);
                                        for (var x = 0; x < w; x++) {
                                            t[x] = ReadLegacyPixel(s + x * srcBytes, bitsLegacy, rMask, gMask, bMask, aMask);
                                        }
                                    }
                                }
                            }

                            rowPitch = dstRowPitch;
                            slicePitch = dstSlicePitch;
                            pointers[index] = convertedHandle.AddrOfPinnedObject() + convertedOffset;
                            offset += (int)srcTotal;
                            convertedOffset += dstSlicePitch * d;
                        } else {
                            GetSurfaceInfo(w, h, format, out rowPitch, out slicePitch, out rows);
                            var total = (long)slicePitch * d;
                            if (offset + total > data.Length) throw new InvalidDataException("DDS file is truncated");
                            pointers[index] = basePtr + offset;
                            offset += (int)total;
                        }

                        pitches[index] = rowPitch;
                        slices[index] = slicePitch;
                        index++;

                        w = Math.Max(1, w / 2);
                        h = Math.Max(1, h / 2);
                        d = Math.Max(1, d / 2);
                    }
                }

                if (isVolume) {
                    var boxes = new DataBox[count];
                    for (var i = 0; i < count; i++) {
                        boxes[i] = new DataBox(pointers[i], pitches[i], slices[i]);
                    }

                    return new Texture3D(device, new Texture3DDescription {
                        Width = width,
                        Height = height,
                        Depth = depth,
                        MipLevels = mipCount,
                        Format = actualFormat,
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    }, boxes);
                } else {
                    var boxes = new DataRectangle[count];
                    for (var i = 0; i < count; i++) {
                        boxes[i] = new DataRectangle(pointers[i], pitches[i]);
                    }

                    return new Texture2D(device, new Texture2DDescription {
                        Width = width,
                        Height = height,
                        MipLevels = mipCount,
                        ArraySize = arraySize,
                        Format = actualFormat,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = isCube ? ResourceOptionFlags.TextureCube : ResourceOptionFlags.None
                    }, boxes);
                }
            } finally {
                handle.Free();
                if (convertedHandle.IsAllocated) {
                    convertedHandle.Free();
                }
            }
        }

        private static Resource LoadWithSystemDrawing(Device device, byte[] data) {
            using (var stream = new MemoryStream(data, false))
            using (var image = Image.FromStream(stream, false, false))
            using (var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb)) {
                using (var g = Graphics.FromImage(bitmap)) {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }

                var width = bitmap.Width;
                var height = bitmap.Height;

                // Converting to RGBA
                var rgba = new byte[width * height * 4];
                var locked = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try {
                    fixed (byte* dst = rgba) {
                        for (var y = 0; y < height; y++) {
                            var s = (byte*)locked.Scan0 + y * locked.Stride;
                            var t = dst + y * width * 4;
                            for (var x = 0; x < width; x++) {
                                t[x * 4] = s[x * 4 + 2];
                                t[x * 4 + 1] = s[x * 4 + 1];
                                t[x * 4 + 2] = s[x * 4];
                                t[x * 4 + 3] = s[x * 4 + 3];
                            }
                        }
                    }
                } finally {
                    bitmap.UnlockBits(locked);
                }

                // Mipmaps (box filter, D3DX11 was generating them by default as well)
                var mipCount = 1;
                for (var s = Math.Max(width, height); s > 1; s >>= 1) mipCount++;

                var levels = new byte[mipCount][];
                var widths = new int[mipCount];
                var heights = new int[mipCount];
                levels[0] = rgba;
                widths[0] = width;
                heights[0] = height;

                for (var i = 1; i < mipCount; i++) {
                    var pw = widths[i - 1];
                    var ph = heights[i - 1];
                    var w = Math.Max(1, pw / 2);
                    var h = Math.Max(1, ph / 2);
                    var next = new byte[w * h * 4];
                    var prev = levels[i - 1];
                    for (var y = 0; y < h; y++) {
                        var y0 = Math.Min(ph - 1, y * 2);
                        var y1 = Math.Min(ph - 1, y * 2 + 1);
                        for (var x = 0; x < w; x++) {
                            var x0 = Math.Min(pw - 1, x * 2);
                            var x1 = Math.Min(pw - 1, x * 2 + 1);
                            var a = (y0 * pw + x0) * 4;
                            var b = (y0 * pw + x1) * 4;
                            var c = (y1 * pw + x0) * 4;
                            var d = (y1 * pw + x1) * 4;
                            var o = (y * w + x) * 4;
                            for (var k = 0; k < 4; k++) {
                                next[o + k] = (byte)((prev[a + k] + prev[b + k] + prev[c + k] + prev[d + k] + 2) >> 2);
                            }
                        }
                    }

                    levels[i] = next;
                    widths[i] = w;
                    heights[i] = h;
                }

                var handles = new GCHandle[mipCount];
                try {
                    var rectangles = new DataRectangle[mipCount];
                    for (var i = 0; i < mipCount; i++) {
                        handles[i] = GCHandle.Alloc(levels[i], GCHandleType.Pinned);
                        rectangles[i] = new DataRectangle(handles[i].AddrOfPinnedObject(), widths[i] * 4);
                    }

                    return new Texture2D(device, new Texture2DDescription {
                        Width = width,
                        Height = height,
                        MipLevels = mipCount,
                        ArraySize = 1,
                        Format = Format.R8G8B8A8_UNorm,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    }, rectangles);
                } finally {
                    foreach (var handle in handles) {
                        if (handle.IsAllocated) handle.Free();
                    }
                }
            }
        }
        #endregion

        #region Saving
        public static void SaveTexture2DToFile(DeviceContext context, Texture2D texture, ImageFileFormat format, string filename) {
            using (var stream = File.Create(filename)) {
                SaveTexture2D(context, texture, format, stream);
            }
        }

        public static void SaveTexture2D(Texture2D texture, ImageFileFormat format, Stream stream) {
            SaveTexture2D(texture.Device.ImmediateContext, texture, format, stream);
        }

        public static void SaveTexture2D(DeviceContext context, Texture2D texture, ImageFileFormat format, Stream stream) {
            var description = texture.Description;
            var staging = new Texture2D(texture.Device, new Texture2DDescription {
                Width = description.Width,
                Height = description.Height,
                MipLevels = description.MipLevels,
                ArraySize = description.ArraySize,
                Format = description.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });

            try {
                if (description.SampleDescription.Count > 1) {
                    using (var resolved = new Texture2D(texture.Device, new Texture2DDescription {
                        Width = description.Width,
                        Height = description.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = description.Format,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.None,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    })) {
                        context.ResolveSubresource(texture, 0, resolved, 0, description.Format);
                        context.CopyResource(resolved, staging);
                    }
                } else {
                    context.CopyResource(texture, staging);
                }

                if (format == ImageFileFormat.Dds) {
                    WriteDds(context, staging, description, stream);
                } else {
                    WriteBitmap(context, staging, description, format, stream);
                }
            } finally {
                staging.Dispose();
            }
        }

        private static void WriteU32(BinaryWriter w, uint value) {
            w.Write(value);
        }

        private static void WriteDds(DeviceContext context, Texture2D staging, Texture2DDescription description, Stream stream) {
            var format = description.Format;
            var isCube = (description.OptionFlags & ResourceOptionFlags.TextureCube) != 0;
            var legacy = format == Format.R8G8B8A8_UNorm || format == Format.B8G8R8A8_UNorm;

            using (var w = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) {
                w.Write(0x20534444u); // magic
                WriteU32(w, 124);

                // DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_MIPMAPCOUNT | DDSD_LINEARSIZE or PITCH
                var compressed = IsBlockCompressed(format);
                uint flags = 0x1 | 0x2 | 0x4 | 0x1000 | (description.MipLevels > 1 ? 0x20000u : 0u) | (compressed ? 0x80000u : 0x8u);
                WriteU32(w, flags);
                WriteU32(w, (uint)description.Height);
                WriteU32(w, (uint)description.Width);
                GetSurfaceInfo(description.Width, description.Height, format, out var rowPitch, out var slicePitch, out _);
                WriteU32(w, (uint)(compressed ? slicePitch : rowPitch));
                WriteU32(w, 0);
                WriteU32(w, (uint)description.MipLevels);
                for (var i = 0; i < 11; i++) WriteU32(w, 0);

                // pixel format
                WriteU32(w, 32);
                if (legacy) {
                    WriteU32(w, DdpfRgb | DdpfAlphaPixels);
                    WriteU32(w, 0);
                    WriteU32(w, 32);
                    if (format == Format.R8G8B8A8_UNorm) {
                        WriteU32(w, 0x000000ff);
                        WriteU32(w, 0x0000ff00);
                        WriteU32(w, 0x00ff0000);
                    } else {
                        WriteU32(w, 0x00ff0000);
                        WriteU32(w, 0x0000ff00);
                        WriteU32(w, 0x000000ff);
                    }
                    WriteU32(w, 0xff000000);
                } else {
                    WriteU32(w, DdpfFourCc);
                    WriteU32(w, FourCc('D', 'X', '1', '0'));
                    for (var i = 0; i < 5; i++) WriteU32(w, 0);
                }

                // caps
                WriteU32(w, 0x1000 | (description.MipLevels > 1 ? 0x400008u : 0u));
                WriteU32(w, isCube ? 0xFE00u : 0u);
                WriteU32(w, 0);
                WriteU32(w, 0);
                WriteU32(w, 0);

                if (!legacy) {
                    WriteU32(w, (uint)format);
                    WriteU32(w, 3); // 2D
                    WriteU32(w, isCube ? 0x4u : 0u);
                    WriteU32(w, (uint)(isCube ? description.ArraySize / 6 : description.ArraySize));
                    WriteU32(w, 0);
                }

                for (var a = 0; a < description.ArraySize; a++) {
                    for (var m = 0; m < description.MipLevels; m++) {
                        var width = Math.Max(1, description.Width >> m);
                        var height = Math.Max(1, description.Height >> m);
                        GetSurfaceInfo(width, height, format, out var rp, out _, out var rows);

                        var box = context.MapSubresource(staging, m, a, MapMode.Read, MapFlags.None, out _);
                        try {
                            var row = new byte[rp];
                            for (var y = 0; y < rows; y++) {
                                Marshal.Copy(box.DataPointer + y * box.RowPitch, row, 0, rp);
                                w.Write(row);
                            }
                        } finally {
                            context.UnmapSubresource(staging, m + a * description.MipLevels);
                        }
                    }
                }
            }
        }

        private static float HalfToFloat(ushort h) {
            return (float)BitConverter.UInt16BitsToHalf(h);
        }

        private static byte ToByte(float v) {
            if (!(v > 0f)) return 0;
            if (v >= 1f) return 255;
            return (byte)(v * 255f + 0.5f);
        }

        private static void WriteBitmap(DeviceContext context, Texture2D staging, Texture2DDescription description, ImageFileFormat imageFormat,
                Stream stream) {
            var width = description.Width;
            var height = description.Height;
            var format = description.Format;

            using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb)) {
                var locked = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                var box = context.MapSubresource(staging, 0, 0, MapMode.Read, MapFlags.None, out _);
                try {
                    for (var y = 0; y < height; y++) {
                        var s = (byte*)box.DataPointer + y * box.RowPitch;
                        var t = (byte*)locked.Scan0 + y * locked.Stride;
                        ConvertRow(s, t, width, format);
                    }
                } finally {
                    context.UnmapSubresource(staging, 0);
                    bitmap.UnlockBits(locked);
                }

                switch (imageFormat) {
                    case ImageFileFormat.Png:
                        bitmap.Save(stream, ImageFormat.Png);
                        break;
                    case ImageFileFormat.Bmp:
                        bitmap.Save(stream, ImageFormat.Bmp);
                        break;
                    case ImageFileFormat.Gif:
                        bitmap.Save(stream, ImageFormat.Gif);
                        break;
                    case ImageFileFormat.Tiff:
                        bitmap.Save(stream, ImageFormat.Tiff);
                        break;
                    case ImageFileFormat.Jpg:
                        SaveJpeg(bitmap, stream, 92L);
                        break;
                    default:
                        throw new NotSupportedException($"Format {imageFormat} is not supported");
                }
            }
        }

        private static void SaveJpeg(Bitmap bitmap, Stream stream, long quality) {
            ImageCodecInfo codec = null;
            foreach (var c in ImageCodecInfo.GetImageEncoders()) {
                if (c.FormatID == ImageFormat.Jpeg.Guid) {
                    codec = c;
                    break;
                }
            }

            if (codec == null) {
                bitmap.Save(stream, ImageFormat.Jpeg);
                return;
            }

            using (var parameters = new EncoderParameters(1)) {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                bitmap.Save(stream, codec, parameters);
            }
        }

        private static void ConvertRow(byte* s, byte* t, int width, Format format) {
            switch (format) {
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8A8_UNorm_SRgb:
                case Format.B8G8R8A8_Typeless:
                    System.Buffer.MemoryCopy(s, t, width * 4, width * 4);
                    break;
                case Format.B8G8R8X8_UNorm:
                case Format.B8G8R8X8_UNorm_SRgb:
                case Format.B8G8R8X8_Typeless:
                    for (var x = 0; x < width; x++) {
                        t[x * 4] = s[x * 4];
                        t[x * 4 + 1] = s[x * 4 + 1];
                        t[x * 4 + 2] = s[x * 4 + 2];
                        t[x * 4 + 3] = 255;
                    }
                    break;
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_Typeless:
                    for (var x = 0; x < width; x++) {
                        t[x * 4] = s[x * 4 + 2];
                        t[x * 4 + 1] = s[x * 4 + 1];
                        t[x * 4 + 2] = s[x * 4];
                        t[x * 4 + 3] = s[x * 4 + 3];
                    }
                    break;
                case Format.R8_UNorm:
                    for (var x = 0; x < width; x++) {
                        t[x * 4] = t[x * 4 + 1] = t[x * 4 + 2] = s[x];
                        t[x * 4 + 3] = 255;
                    }
                    break;
                case Format.R16G16B16A16_Float:
                    for (var x = 0; x < width; x++) {
                        var p = (ushort*)s + x * 4;
                        t[x * 4] = ToByte(HalfToFloat(p[2]));
                        t[x * 4 + 1] = ToByte(HalfToFloat(p[1]));
                        t[x * 4 + 2] = ToByte(HalfToFloat(p[0]));
                        t[x * 4 + 3] = ToByte(HalfToFloat(p[3]));
                    }
                    break;
                case Format.R16G16B16A16_UNorm:
                    for (var x = 0; x < width; x++) {
                        var p = (ushort*)s + x * 4;
                        t[x * 4] = (byte)(p[2] >> 8);
                        t[x * 4 + 1] = (byte)(p[1] >> 8);
                        t[x * 4 + 2] = (byte)(p[0] >> 8);
                        t[x * 4 + 3] = (byte)(p[3] >> 8);
                    }
                    break;
                case Format.R32G32B32A32_Float:
                    for (var x = 0; x < width; x++) {
                        var p = (float*)s + x * 4;
                        t[x * 4] = ToByte(p[2]);
                        t[x * 4 + 1] = ToByte(p[1]);
                        t[x * 4 + 2] = ToByte(p[0]);
                        t[x * 4 + 3] = ToByte(p[3]);
                    }
                    break;
                case Format.R10G10B10A2_UNorm:
                    for (var x = 0; x < width; x++) {
                        var v = ((uint*)s)[x];
                        t[x * 4] = (byte)(((v >> 20) & 0x3ff) >> 2);
                        t[x * 4 + 1] = (byte)(((v >> 10) & 0x3ff) >> 2);
                        t[x * 4 + 2] = (byte)((v & 0x3ff) >> 2);
                        t[x * 4 + 3] = (byte)((v >> 30) * 85);
                    }
                    break;
                case Format.R32_Float:
                    for (var x = 0; x < width; x++) {
                        var b = ToByte(((float*)s)[x]);
                        t[x * 4] = t[x * 4 + 1] = t[x * 4 + 2] = b;
                        t[x * 4 + 3] = 255;
                    }
                    break;
                case Format.R16_Float:
                    for (var x = 0; x < width; x++) {
                        var b = ToByte(HalfToFloat(((ushort*)s)[x]));
                        t[x * 4] = t[x * 4 + 1] = t[x * 4 + 2] = b;
                        t[x * 4 + 3] = 255;
                    }
                    break;
                case Format.R16_UNorm:
                    for (var x = 0; x < width; x++) {
                        var b = (byte)(((ushort*)s)[x] >> 8);
                        t[x * 4] = t[x * 4 + 1] = t[x * 4 + 2] = b;
                        t[x * 4 + 3] = 255;
                    }
                    break;
                default:
                    throw new NotSupportedException($"Saving format {format} is not supported");
            }
        }
        #endregion
    }
}
