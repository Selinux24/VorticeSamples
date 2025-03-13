using DirectXTexNet;
using PrimalLike.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Utilities;

namespace TexturesImporter
{
    public static class TextureImporter
    {
        private static TexHelper Helper => TexHelper.Instance;

        public static void ShutDownTextureTools()
        {
            DeviceManager.ShutDown();
        }

        private static TexMetadata MetadataFromTextureInfo(TextureInfo info)
        {
            DXGI_FORMAT format = (DXGI_FORMAT)info.Format;

            bool is3d = info.Flags.HasFlag(TextureFlags.IsVolumeMap);

            int width = info.Width;
            int height = info.Height;
            int depth = is3d ? info.ArraySize : 1;
            int arraySize = is3d ? 1 : info.ArraySize;
            int mipLevels = info.MipLevels;
            TEX_MISC_FLAG miscFlags = info.Flags.HasFlag(TextureFlags.IsCubeMap) ? TEX_MISC_FLAG.TEXTURECUBE : 0;
            TEX_MISC_FLAG2 miscFlags2 = (TEX_MISC_FLAG2)(info.Flags.HasFlag(TextureFlags.IsPremultipliedAlpha) ?
                (uint)TEX_ALPHA_MODE.PREMULTIPLIED :
                info.Flags.HasFlag(TextureFlags.HasAlpha) ? (uint)TEX_ALPHA_MODE.STRAIGHT : (uint)TEX_ALPHA_MODE.OPAQUE);
            // TODO: what about 1D?
            TEX_DIMENSION dimension = is3d ? TEX_DIMENSION.TEXTURE3D : TEX_DIMENSION.TEXTURE2D;

            return new(width, height, depth, arraySize, mipLevels, miscFlags, miscFlags2, format, dimension);
        }
        private static void TextureInfoFromMetadata(TexMetadata metadata, ref TextureInfo info)
        {
            DXGI_FORMAT format = metadata.Format;
            info.Format = (uint)format;
            info.Width = metadata.Width;
            info.Height = metadata.Height;
            info.ArraySize = metadata.IsVolumemap() ? metadata.Depth : metadata.ArraySize;
            info.MipLevels = metadata.MipLevels;
            SetOrClearFlag(ref info.Flags, TextureFlags.HasAlpha, Helper.HasAlpha(format));
            SetOrClearFlag(ref info.Flags, TextureFlags.IsHdr, format == DXGI_FORMAT.BC6H_UF16 || format == DXGI_FORMAT.BC6H_SF16);
            SetOrClearFlag(ref info.Flags, TextureFlags.IsPremultipliedAlpha, metadata.IsPMAlpha());
            SetOrClearFlag(ref info.Flags, TextureFlags.IsCubeMap, metadata.IsCubemap());
            SetOrClearFlag(ref info.Flags, TextureFlags.IsVolumeMap, metadata.IsVolumemap());
            SetOrClearFlag(ref info.Flags, TextureFlags.IsSRGB, Helper.IsSRGB(format));
        }
        private static void SetOrClearFlag(ref TextureFlags flags, TextureFlags flag, bool set)
        {
            if (set)
            {
                flags |= flag;
            }
            else
            {
                flags &= ~flag;
            }
        }

        private static long GetImageSize(Image image)
        {
            return
                sizeof(int) +       // Width
                sizeof(int) +       // Height
                sizeof(uint) +      // RowPitch
                sizeof(uint) +      // SlicePitch
                image.SlicePitch;   // Pixels size
        }
        private static void WriteImage(BlobStreamWriter blob, Image image)
        {
            Debug.Assert(image.SlicePitch <= int.MaxValue);

            blob.Write(image.Width);
            blob.Write(image.Height);
            blob.Write((uint)image.RowPitch);
            blob.Write((uint)image.SlicePitch);
            blob.Write(image.Pixels, (int)image.SlicePitch);
        }
        private static Image ReadImage(BlobStreamReader blob, DXGI_FORMAT format)
        {
            int width = blob.Read<int>();
            int height = blob.Read<int>();
            uint rowPitch = blob.Read<uint>();
            uint slicePitch = blob.Read<uint>();
            IntPtr pixels = blob.Position;

            Image image = new(width, height, format, rowPitch, slicePitch, pixels, null);

            blob.Skip(slicePitch);

            return image;
        }

        public static void Import(ref TextureData data)
        {
            var settings = data.ImportSettings;
            Debug.Assert(settings.Sources != null && settings.SourceCount > 0);

            List<ScratchImage> scratchImages = [];
            List<Image> images = [];

            uint width = 0;
            uint height = 0;
            DXGI_FORMAT format = DXGI_FORMAT.UNKNOWN;
            var files = settings.Sources.Split(';');
            Debug.Assert(files.Length == settings.SourceCount);

            for (uint i = 0; i < settings.SourceCount; i++)
            {
                var scratchFile = LoadFromFile(ref data, files[i]);
                if (data.Info.ImportError != 0)
                {
                    return;
                }
                scratchImages.Add(scratchFile);

                var metadata = scratchFile.GetMetadata();
                if (i == 0)
                {
                    width = (uint)metadata.Width;
                    height = (uint)metadata.Height;
                    format = metadata.Format;
                }

                // All image sources should have the same size.
                if (width != metadata.Width || height != metadata.Height)
                {
                    data.Info.ImportError = ImportErrors.SizeMismatch;
                    return;
                }

                // All image sources should have the same format.
                if (format != metadata.Format)
                {
                    data.Info.ImportError = ImportErrors.FormatMismatch;
                    return;
                }

                uint arraySize = (uint)metadata.ArraySize;
                uint depth = (uint)metadata.Depth;

                for (int arrayIndex = 0; arrayIndex < arraySize; arrayIndex++)
                {
                    for (int depthIndex = 0; depthIndex < depth; depthIndex++)
                    {
                        var image = scratchFile.GetImage(0, arrayIndex, depthIndex);
                        if (image == null)
                        {
                            data.Info.ImportError = ImportErrors.Unknown;
                            return;
                        }

                        if (width != image.Width || height != image.Height)
                        {
                            data.Info.ImportError = ImportErrors.SizeMismatch;
                            return;
                        }

                        images.Add(image);
                    }
                }
            }

            using var scratch = InitializeFromImages(ref data, [.. images]);
            if (data.Info.ImportError != 0)
            {
                return;
            }

            for (int i = 0; i < scratchImages.Count; i++)
            {
                scratchImages[i].Dispose();
            }
            scratchImages.Clear();

            if (!settings.Compress)
            {
                CopySubresources(scratch, ref data);
                TextureInfoFromMetadata(scratch.GetMetadata(), ref data.Info);
                return;
            }

            using var bcScratch = CompressImage(ref data, scratch);
            if (data.Info.ImportError != 0)
            {
                return;
            }

            // Decompress the first image to be used for the icon.
            CopyIcon(bcScratch, ref data);
            CopySubresources(bcScratch, ref data);
            TextureInfoFromMetadata(bcScratch.GetMetadata(), ref data.Info);
        }
        private static ScratchImage LoadFromFile(ref TextureData data, string fileName)
        {
            Debug.Assert(File.Exists(fileName));
            if (!File.Exists(fileName))
            {
                data.Info.ImportError = ImportErrors.FileNotFound;
                return null;
            }

            data.Info.ImportError = ImportErrors.Load;

            WIC_FLAGS wicFlags = WIC_FLAGS.NONE;

            if (data.ImportSettings.OutputFormat == (uint)DXGI_FORMAT.BC4_UNORM ||
                data.ImportSettings.OutputFormat == (uint)DXGI_FORMAT.BC5_UNORM)
            {
                wicFlags |= WIC_FLAGS.IGNORE_SRGB;
            }

            string file = fileName;

            // Try one of WIC formats first (e.g. BMP, JPEG, PNG, etc.).
            wicFlags |= WIC_FLAGS.FORCE_RGB;

            var scratch = Helper.LoadFromWICFile(file, wicFlags);

            // It wasn't a WIC format. Try TGA.
            scratch ??= Helper.LoadFromTGAFile(file);

            // It wasn't a TGA either. Try HDR.
            if (scratch == null)
            {
                scratch = Helper.LoadFromHDRFile(file);
                if (scratch != null)
                {
                    data.Info.Flags |= TextureFlags.IsHdr;
                }
            }

            // It wasn't HDR. Try DDS.
            if (scratch == null)
            {
                scratch = Helper.LoadFromDDSFile(file, DDS_FLAGS.FORCE_RGB);
                if (scratch != null)
                {
                    data.Info.ImportError = ImportErrors.Decompress;
                    var mipScratch = scratch.Decompress(0, DXGI_FORMAT.UNKNOWN);
                    if (mipScratch != null)
                    {
                        scratch = mipScratch;
                    }
                }
            }

            if (scratch != null)
            {
                data.Info.ImportError = ImportErrors.Succeeded;
            }

            return scratch;
        }
        private static ScratchImage InitializeFromImages(ref TextureData data, Image[] images)
        {
            var settings = data.ImportSettings;

            ScratchImage scratch;
            {
                // Scope for working scratch
                var metadata = MetadataFromTextureInfo(data.Info);
                using var workingScratch = Helper.InitializeTemporary(images, metadata);
                int arraySize = images.Length;

                if (settings.Dimension == TextureDimensions.Texture1D || settings.Dimension == TextureDimensions.Texture2D)
                {
                    bool allow1d = settings.Dimension == TextureDimensions.Texture1D;
                    Debug.Assert(arraySize >= 1 && images.Length >= 1);
                    scratch = workingScratch.CreateArrayCopy(0, arraySize, allow1d, CP_FLAGS.NONE);
                }
                else if (settings.Dimension == TextureDimensions.TextureCube)
                {
                    if ((arraySize % 6) != 0)
                    {
                        data.Info.ImportError = ImportErrors.NeedSixImages;
                        return null;
                    }
                    scratch = workingScratch.CreateCubeCopy(0, arraySize, CP_FLAGS.NONE);
                }
                else
                {
                    Debug.Assert(settings.Dimension == TextureDimensions.Texture3D);
                    scratch = workingScratch.CreateVolumeCopy(0, arraySize, CP_FLAGS.NONE);
                }

                if (scratch == null)
                {
                    data.Info.ImportError = ImportErrors.Unknown;
                    return null;
                }
            }

            if (settings.MipLevels == 1)
            {
                return scratch;
            }

            ScratchImage mipScratch;
            {
                var metadata = scratch.GetMetadata();
                int mipLevels = Math.Clamp(settings.MipLevels, 0, GetMaxMipCount(metadata.Width, metadata.Height, metadata.Depth));

                if (settings.Dimension != TextureDimensions.Texture3D)
                {
                    mipScratch = scratch.GenerateMipMaps(0, TEX_FILTER_FLAGS.DEFAULT, mipLevels, false);
                }
                else
                {
                    mipScratch = scratch.GenerateMipMaps3D(0, scratch.GetImageCount(), TEX_FILTER_FLAGS.DEFAULT, mipLevels);
                }

                if (mipScratch == null)
                {
                    data.Info.ImportError = ImportErrors.MipmapGeneration;
                    return null;
                }

                scratch.Dispose();

                return mipScratch;
            }
        }
        private static void CopyIcon(ScratchImage bcScratch, ref TextureData data)
        {
            Debug.Assert(bcScratch.GetImageCount() > 0);
            using var scratch = bcScratch.Decompress(0, DXGI_FORMAT.UNKNOWN);

            Debug.Assert(scratch.GetImageCount() > 0);
            var image = scratch.GetImage(0);

            long size = GetImageSize(image);
            Debug.Assert(size <= int.MaxValue);
            data.IconSize = (uint)size;

            data.Icon = Marshal.AllocHGlobal((int)size);
            Debug.Assert(data.Icon != IntPtr.Zero);

            BlobStreamWriter blob = new(data.Icon, (int)size);
            WriteImage(blob, image);
        }
        private static ScratchImage CompressImage(ref TextureData data, ScratchImage scratch)
        {
            Debug.Assert(data.ImportSettings.Compress && scratch.GetImageCount() > 0);

            var image = scratch.GetImage(0, 0, 0);
            if (image == null)
            {
                data.Info.ImportError = ImportErrors.Unknown;
                return null;
            }

            var outputFormat = DetermineOutputFormat(ref data, scratch, image);

            ScratchImage bcScratch;
            if (DeviceManager.CanUseGpu(outputFormat))
            {
                bcScratch = DeviceManager.CompressGpu(scratch, outputFormat);
            }
            else
            {
                bcScratch = scratch.Compress(outputFormat, TEX_COMPRESS_FLAGS.PARALLEL, data.ImportSettings.AlphaThreshold);
            }

            if (bcScratch == null)
            {
                data.Info.ImportError = ImportErrors.Compress;
                return null;
            }

            return bcScratch;
        }
        private static DXGI_FORMAT DetermineOutputFormat(ref TextureData data, ScratchImage scratch, Image image)
        {
            Debug.Assert(data.ImportSettings.Compress);
            var imageFormat = image.Format;

            if (data.ImportSettings.OutputFormat == (uint)DXGI_FORMAT.UNKNOWN)
            {
                // Determine the best block compressed format if import settings
                // don't explicitly specify a format.

                if (data.Info.Flags.HasFlag(TextureFlags.IsHdr) || imageFormat == DXGI_FORMAT.BC6H_UF16 || imageFormat == DXGI_FORMAT.BC6H_SF16)
                {
                    data.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC6H_UF16;
                }
                else if (imageFormat == DXGI_FORMAT.R8_UNORM || imageFormat == DXGI_FORMAT.BC4_UNORM || imageFormat == DXGI_FORMAT.BC4_SNORM)
                {
                    // If the source image is gray scale or a single channel block compressed format (BC4),
                    // then output format will be BC4.
                    data.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC4_UNORM;
                }
                else if (NormalMapIdentification.IsNormalMap(image) || imageFormat == DXGI_FORMAT.BC5_UNORM || imageFormat == DXGI_FORMAT.BC5_SNORM)
                {
                    // Test if the source image is a normal map and if so, use BC5 format for the output.
                    data.Info.Flags |= TextureFlags.IsImportedAsNormalMap;
                    data.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC5_UNORM;

                    if (Helper.IsSRGB(imageFormat))
                    {
                        scratch.OverrideFormat(Helper.MakeTypelessUNORM(Helper.MakeTypeless(imageFormat)));
                    }
                }
                else
                {
                    // We exhausted all options. use an RGBA block compressed format.
                    if (data.ImportSettings.PreferBc7)
                    {
                        data.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC7_UNORM;
                    }
                    else
                    {
                        if (scratch.IsAlphaAllOpaque())
                        {
                            data.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC1_UNORM;
                        }
                        else
                        {
                            data.ImportSettings.OutputFormat = (uint)DXGI_FORMAT.BC3_UNORM;
                        }
                    }
                }
            }

            Debug.Assert(Helper.IsCompressed((DXGI_FORMAT)data.ImportSettings.OutputFormat));

            if (Helper.HasAlpha((DXGI_FORMAT)data.ImportSettings.OutputFormat))
            {
                data.Info.Flags |= TextureFlags.HasAlpha;
            }

            if (Helper.IsSRGB(image.Format))
            {
                return Helper.MakeSRGB((DXGI_FORMAT)data.ImportSettings.OutputFormat);
            }
            else
            {
                return (DXGI_FORMAT)data.ImportSettings.OutputFormat;
            }
        }
        private static void CopySubresources(ScratchImage scratch, ref TextureData data)
        {
            var metadata = scratch.GetMetadata();
            Debug.Assert(metadata.MipLevels > 0 && metadata.MipLevels <= TextureData.MaxMips);

            ulong subresourceSize = 0;

            int imageCount = scratch.GetImageCount();
            for (int i = 0; i < imageCount; i++)
            {
                subresourceSize += (ulong)GetImageSize(scratch.GetImage(i));
            }

            if (subresourceSize > uint.MaxValue)
            {
                // Support up to 4GB per resource.
                data.Info.ImportError = ImportErrors.MaxSizeExceeded;
                return;
            }

            data.SubresourceSize = (uint)subresourceSize;
            data.SubresourceData = Marshal.AllocHGlobal((IntPtr)data.SubresourceSize);
            Debug.Assert(data.SubresourceData != IntPtr.Zero);

            BlobStreamWriter blob = new(data.SubresourceData, (int)data.SubresourceSize);

            for (int i = 0; i < imageCount; i++)
            {
                var image = scratch.GetImage(i);

                WriteImage(blob, image);
            }
        }
        private static int GetMaxMipCount(int width, int height, int depth)
        {
            int mipLevels = 1;
            while (width > 1 || height > 1 || depth > 1)
            {
                width >>= 1;
                height >>= 1;
                depth >>= 1;

                mipLevels++;
            }

            return mipLevels;
        }

        public static void Decompress(ref TextureData data)
        {
            Debug.Assert(Helper.IsCompressed((DXGI_FORMAT)data.Info.Format));

            Debug.Assert(data.ImportSettings.Compress);
            Image[] images = SubresourceDataToImages(ref data);

            var metadata = MetadataFromTextureInfo(data.Info);
            var tmp = Helper.InitializeTemporary([.. images], metadata);

            var scratch = tmp.Decompress(0, DXGI_FORMAT.UNKNOWN);
            if (scratch != null)
            {
                CopySubresources(scratch, ref data);
                TextureInfoFromMetadata(scratch.GetMetadata(), ref data.Info);
            }
            else
            {
                data.Info.ImportError = ImportErrors.Decompress;
            }
        }
        private static Image[] SubresourceDataToImages(ref TextureData data)
        {
            Debug.Assert(data.SubresourceData != IntPtr.Zero && data.SubresourceSize > 0);
            Debug.Assert(data.Info.MipLevels > 0 && data.Info.MipLevels <= TextureData.MaxMips);
            Debug.Assert(data.Info.ArraySize > 0);

            var info = data.Info;
            int imageCount = info.ArraySize;

            if (info.Flags.HasFlag(TextureFlags.IsVolumeMap))
            {
                int depthPerMipLevel = info.ArraySize;
                for (int i = 1; i < info.MipLevels; i++)
                {
                    depthPerMipLevel = Math.Max(depthPerMipLevel >> 1, 1);
                    imageCount += depthPerMipLevel;
                }
            }
            else
            {
                imageCount *= info.MipLevels;
            }

            BlobStreamReader blob = new(data.SubresourceData);
            Image[] images = new Image[imageCount];

            for (uint i = 0; i < imageCount; i++)
            {
                images[i] = ReadImage(blob, (DXGI_FORMAT)info.Format);
            }

            return images;
        }
    }
}
