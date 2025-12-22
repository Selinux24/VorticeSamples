using DirectXTexNet;
using PrimalLike.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TexturesImporter.EnvMaps;
using Utilities;

namespace TexturesImporter
{
    public static class TextureImporter
    {
        const int SampleCount = 1024;

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
            SetOrClearFlag(ref info.Flags, TextureFlags.IsHdr, DeviceManager.IsHdr(format));
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

        private static ulong GetImageAssetSize(ScratchImage scratch)
        {
            ulong totalSize = 0;

            int imageCount = scratch.GetImageCount();
            for (int i = 0; i < imageCount; i++)
            {
                totalSize += GetImageAssetSize(scratch.GetImage(i));
            }

            return totalSize;
        }
        private static ulong GetImageAssetSize(Image image)
        {
            long totalSize =
                sizeof(int) +       // Width
                sizeof(int) +       // Height
                sizeof(uint) +      // RowPitch
                sizeof(uint) +      // SlicePitch
                image.SlicePitch;   // Pixels size

            return (ulong)totalSize;
        }
        private static void WriteImageAsset(this BlobStreamWriter blob, ScratchImage scratch)
        {
            int imageCount = scratch.GetImageCount();

            for (int i = 0; i < imageCount; i++)
            {
                blob.WriteImageAsset(scratch.GetImage(i));
            }
        }
        private static void WriteImageAsset(this BlobStreamWriter blob, Image image)
        {
            Debug.Assert(image.SlicePitch <= int.MaxValue);

            blob.Write(image.Width);
            blob.Write(image.Height);
            blob.Write((uint)image.RowPitch);
            blob.Write((uint)image.SlicePitch);
            blob.Write(image.Pixels, (int)image.SlicePitch);
        }
        private static Image ReadImageAsset(this BlobStreamReader blob, DXGI_FORMAT format)
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

        public static void Import(TextureData data)
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
                var scratchFile = LoadFromFile(data, files[i]);
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

                for (int arrayIndex = 0; arrayIndex < metadata.ArraySize; arrayIndex++)
                {
                    for (int depthIndex = 0; depthIndex < metadata.Depth; depthIndex++)
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

            using var scratch = InitializeFromImages(data, [.. images]);
            for (int i = 0; i < scratchImages.Count; i++)
            {
                scratchImages[i].Dispose();
            }
            scratchImages.Clear();

            if (data.Info.ImportError != ImportErrors.Succeeded) return;

            if (settings.Compress && !(scratch.GetMetadata().IsCubemap() && settings.PrefilterCubemap))
            {
                using var bcScratch = CompressImage(data, scratch);
                if (data.Info.ImportError != 0) return;

                // Decompress the first image to be used for the icon.
#if DEBUG || DEBUG_EDITOR
                SaveDbgFile(bcScratch, data.ImportSettings.Sources, "texture");
#endif
                CopyIcon(bcScratch, data);
                CopySubresources(bcScratch, data);
                TextureInfoFromMetadata(bcScratch.GetMetadata(), ref data.Info);
            }
            else
            {
#if DEBUG || DEBUG_EDITOR
                SaveDbgFile(scratch, data.ImportSettings.Sources, "texture");
#endif
                CopySubresources(scratch, data);
                TextureInfoFromMetadata(scratch.GetMetadata(), ref data.Info);
            }
        }
        private static ScratchImage LoadFromWICFile(string szFile, WIC_FLAGS flags)
        {
            try
            {
                return Helper.LoadFromWICFile(szFile, flags);
            }
            catch
            {
                return null;
            }
        }
        private static ScratchImage LoadFromTGAFile(string szFile)
        {
            try
            {
                return Helper.LoadFromTGAFile(szFile);
            }
            catch
            {
                return null;
            }
        }
        private static ScratchImage LoadFromHDRFile(string szFile)
        {
            try
            {
                return Helper.LoadFromHDRFile(szFile);
            }
            catch
            {
                return null;
            }
        }
        private static ScratchImage LoadFromDDSFile(string szFile, DDS_FLAGS flags)
        {
            try
            {
                return Helper.LoadFromDDSFile(szFile, flags);
            }
            catch
            {
                return null;
            }
        }
        private static ScratchImage LoadFromFile(TextureData data, string fileName)
        {
            Debug.Assert(File.Exists(fileName));
            if (!File.Exists(fileName))
            {
                data.Info.ImportError = ImportErrors.FileNotFound;
                return null;
            }

            data.Info.ImportError = ImportErrors.Load;

            WIC_FLAGS wicFlags = WIC_FLAGS.NONE;

            if (data.ImportSettings.OutputFormat == BCFormats.BC4SingleChannelGray ||
                data.ImportSettings.OutputFormat == BCFormats.BC5DualChannelGray)
            {
                wicFlags |= WIC_FLAGS.IGNORE_SRGB;
            }

            string file = fileName;

            // Try one of WIC formats first (e.g. BMP, JPEG, PNG, etc.).
            wicFlags |= WIC_FLAGS.FORCE_RGB;

            var scratch = LoadFromWICFile(file, wicFlags);

            // It wasn't a WIC format. Try TGA.
            scratch ??= LoadFromTGAFile(file);

            if (scratch == null)
            {
                // It wasn't a TGA either. Try HDR.
                scratch = LoadFromHDRFile(file);
                if (scratch != null)
                {
                    data.Info.Flags |= TextureFlags.IsHdr;
                }
            }

            if (scratch == null)
            {
                // It wasn't HDR. Try DDS.
                scratch = LoadFromDDSFile(file, DDS_FLAGS.FORCE_RGB);
                if (scratch != null)
                {
                    data.Info.ImportError = ImportErrors.Decompress;
                    var mipScratch = scratch.Decompress(0, DXGI_FORMAT.UNKNOWN);
                    if (mipScratch != null)
                    {
                        scratch.Dispose();
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
        private static ScratchImage InitializeFromImages(TextureData data, Image[] images)
        {
            var settings = data.ImportSettings;

            // Scope for working scratch
            var metadata = MetadataFromTextureInfo(data.Info);
            using var workingScratch = Helper.InitializeTemporary(images, metadata);
            int arraySize = images.Length;

            ScratchImage scratch = null;
            if (settings.Dimension == TextureDimensions.Texture1D || settings.Dimension == TextureDimensions.Texture2D)
            {
                bool allow1d = settings.Dimension == TextureDimensions.Texture1D;
                Debug.Assert(arraySize >= 1 && images.Length >= 1);
                scratch = workingScratch.CreateArrayCopy(0, arraySize, allow1d, CP_FLAGS.NONE);
            }
            else if (settings.Dimension == TextureDimensions.TextureCube)
            {
                var image = images[0];

                if (Utils.Equal((float)image.Width / image.Height, 2f))
                {
                    if (!DeviceManager.RunOnGPU((device) =>
                    {
                        scratch = EnvMapProcessing.EquirectangularToCubemapGPU(device, images, settings.CubemapSize, settings.PrefilterCubemap, settings.MirrorCubemap);
                        return scratch != null;
                    }))
                    {
                        scratch = EnvMapProcessing.EquirectangularToCubemapCPU(images, settings.CubemapSize, settings.PrefilterCubemap, settings.MirrorCubemap);
                    }
                }
                else if (arraySize % 6 > 0 || image.Width != image.Height)
                {
                    data.Info.ImportError = ImportErrors.NeedSixImages;
                    return null;
                }
                else
                {
                    scratch = workingScratch.CreateCubeCopy(0, arraySize, CP_FLAGS.NONE);
                }
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

            bool generateFullMipchain = settings.PrefilterCubemap && settings.Dimension == TextureDimensions.TextureCube;
            if (settings.MipLevels != 1 || generateFullMipchain)
            {
                var mipMaps = GenerateMipmaps(scratch, data.Info, generateFullMipchain ? 0 : settings.MipLevels, settings.Dimension == TextureDimensions.Texture3D);
                scratch.Dispose();
                return mipMaps;
            }

            return scratch;
        }
        private static ScratchImage GenerateMipmaps(ScratchImage scratch, TextureInfo info, int mipLevels, bool is3d)
        {
            var metadata = scratch.GetMetadata();
            mipLevels = Math.Clamp(mipLevels, 0, GetMaxMipCount(metadata.Width, metadata.Height, metadata.Depth));

            ScratchImage mipScratch;

            if (!is3d)
            {
                mipScratch = scratch.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, mipLevels);
            }
            else
            {
                mipScratch = scratch.GenerateMipMaps3D(TEX_FILTER_FLAGS.DEFAULT, mipLevels);
            }

            if (mipScratch == null)
            {
                info.ImportError = ImportErrors.MipmapGeneration;
                return null;
            }

            return mipScratch;
        }
        private static void CopyIcon(ScratchImage bcScratch, TextureData data)
        {
            Debug.Assert(bcScratch.GetImageCount() > 0);
            using var scratch = bcScratch.Decompress(0, DXGI_FORMAT.UNKNOWN);

            Debug.Assert(scratch.GetImageCount() > 0);
            var image = scratch.GetImage(0);

            ulong size = GetImageAssetSize(image);
            Debug.Assert(size <= uint.MaxValue);
            data.IconSize = (uint)size;

            data.Icon = Marshal.AllocHGlobal((int)size);
            Debug.Assert(data.Icon != IntPtr.Zero);

            BlobStreamWriter blob = new(data.Icon, (int)size);
            blob.WriteImageAsset(image);
        }
        private static ScratchImage CompressImage(TextureData data, ScratchImage scratch)
        {
            Debug.Assert(data.ImportSettings.Compress && scratch.GetImageCount() > 0);

            var image = scratch.GetImage(0, 0, 0);
            if (image == null)
            {
                data.Info.ImportError = ImportErrors.Unknown;
                return null;
            }

            var outputFormat = DetermineOutputFormat(data, scratch, image);

            ScratchImage bcScratch = null;
            if (!(DeviceManager.CanUseGpu(outputFormat) && DeviceManager.RunOnGPU((device) =>
            {
                bcScratch = DeviceManager.CompressGpu(scratch, outputFormat);
                return bcScratch != null;
            })))
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
        private static DXGI_FORMAT DetermineOutputFormat(TextureData data, ScratchImage scratch, Image image)
        {
            Debug.Assert(data.ImportSettings.Compress);
            var imageFormat = image.Format;

            if (data.ImportSettings.OutputFormat == BCFormats.PickBestFit)
            {
                // Determine the best block compressed format if import settings
                // don't explicitly specify a format.

                if (data.Info.Flags.HasFlag(TextureFlags.IsHdr) || imageFormat == DXGI_FORMAT.BC6H_UF16 || imageFormat == DXGI_FORMAT.BC6H_SF16)
                {
                    data.ImportSettings.OutputFormat = BCFormats.BC6HDR;
                }
                else if (imageFormat == DXGI_FORMAT.R8_UNORM || imageFormat == DXGI_FORMAT.BC4_UNORM || imageFormat == DXGI_FORMAT.BC4_SNORM)
                {
                    // If the source image is gray scale or a single channel block compressed format (BC4),
                    // then output format will be BC4.
                    data.ImportSettings.OutputFormat = BCFormats.BC4SingleChannelGray;
                }
                else if (NormalMapIdentification.IsNormalMap(image) || imageFormat == DXGI_FORMAT.BC5_UNORM || imageFormat == DXGI_FORMAT.BC5_SNORM)
                {
                    // Test if the source image is a normal map and if so, use BC5 format for the output.
                    data.Info.Flags |= TextureFlags.IsImportedAsNormalMap;
                    data.ImportSettings.OutputFormat = BCFormats.BC5DualChannelGray;

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
                        data.ImportSettings.OutputFormat = BCFormats.BC7HighQuality;
                    }
                    else
                    {
                        if (scratch.IsAlphaAllOpaque())
                        {
                            data.ImportSettings.OutputFormat = BCFormats.BC1LowQualityAlpha;
                        }
                        else
                        {
                            data.ImportSettings.OutputFormat = BCFormats.BC3MediumQuality;
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
        private static void CopySubresources(ScratchImage scratch, TextureData data)
        {
            Debug.Assert(scratch.GetMetadata().MipLevels > 0 && scratch.GetMetadata().MipLevels <= TextureData.MaxMips);

            ulong subresourceSize = GetImageAssetSize(scratch);
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
            blob.WriteImageAsset(scratch);
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

        public static void Decompress(TextureData data)
        {
            Debug.Assert(Helper.IsCompressed((DXGI_FORMAT)data.Info.Format));

            Debug.Assert(data.ImportSettings.Compress);
            var images = SubresourceDataToImageAssets(data);

            var metadata = MetadataFromTextureInfo(data.Info);
            using var tmp = Helper.InitializeTemporary([.. images], metadata);
            if (tmp == null)
            {
                data.Info.ImportError = ImportErrors.Unknown;
                return;
            }

            using var scratch = tmp.Decompress(0, DXGI_FORMAT.UNKNOWN);
            if (scratch != null)
            {
                CopySubresources(scratch, data);
                TextureInfoFromMetadata(scratch.GetMetadata(), ref data.Info);
            }
            else
            {
                data.Info.ImportError = ImportErrors.Decompress;
            }
        }
        private static Image[] SubresourceDataToImageAssets(TextureData data)
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
                images[i] = blob.ReadImageAsset((DXGI_FORMAT)info.Format);
            }

            return images;
        }

        public static void PrefilterIbl(TextureData data, IblFilter filterType)
        {
            Debug.Assert(data.ImportSettings.PrefilterCubemap);
            ref var info = ref data.Info;
            DXGI_FORMAT format = (DXGI_FORMAT)info.Format;
            Debug.Assert(!Helper.IsCompressed(format));
            var images = SubresourceDataToImageAssets(data);
            Debug.Assert(images.Length > 0 && !Helper.IsCompressed(images[0].Format));
            Debug.Assert((info.Flags & TextureFlags.IsCubeMap) != 0);
            Debug.Assert(info.Width == info.Height);
            int cubemapCount = info.ArraySize / 6;
            Debug.Assert(info.MipLevels == MathF.Log2(info.Width) + 1);

            using var cubemaps = Helper.InitializeCube(format, info.Width, info.Height, cubemapCount, info.MipLevels, CP_FLAGS.NONE);
            if (cubemaps == null)
            {
                info.ImportError = ImportErrors.Unknown;
                return;
            }

            for (int imgIdx = 0; imgIdx < cubemaps.GetImageCount(); imgIdx++)
            {
                var image = cubemaps.GetImage(imgIdx);
                Debug.Assert(image.SlicePitch == images[imgIdx].SlicePitch);
                BlobStreamWriter writer = new(image.Pixels, (int)image.SlicePitch);
                writer.Write(images[imgIdx].Pixels, (int)image.SlicePitch);
            }

            ScratchImage filtered = null;
            if (!DeviceManager.RunOnGPU((device) =>
            {
                filtered = filterType == IblFilter.Diffuse ?
                    EnvMapProcessing.PrefilterDiffuse(device, cubemaps, SampleCount) :
                    EnvMapProcessing.PrefilterSpecular(device, cubemaps, SampleCount);
                return filtered != null;
            }))
            {
                info.ImportError = ImportErrors.Unknown;
                return;
            }

            using (filtered)
            {
                if (data.ImportSettings.Compress)
                {
                    using var compressed = CompressImage(data, filtered);
                    if (data.Info.ImportError != ImportErrors.Succeeded) return;

                    // Decompress the first image to be used for the icon.
                    Debug.Assert(compressed.GetImageCount() > 0);
                    CopyIcon(compressed, data);

#if DEBUG || DEBUG_EDITOR
                    SaveDbgFile(compressed, data.ImportSettings.Sources, filterType == IblFilter.Diffuse ? "diffuse" : "specular");
#endif
                    CopySubresources(compressed, data);
                    TextureInfoFromMetadata(compressed.GetMetadata(), ref data.Info);
                }
                else
                {
#if DEBUG || DEBUG_EDITOR
                    SaveDbgFile(filtered, data.ImportSettings.Sources, filterType == IblFilter.Diffuse ? "diffuse" : "specular");
#endif
                    CopySubresources(filtered, data);
                    TextureInfoFromMetadata(filtered.GetMetadata(), ref data.Info);
                }
            }
        }

        public static void ComputeBrdfIntegrationLut(TextureData data)
        {
            ScratchImage result = null;
            if (!DeviceManager.RunOnGPU((device) =>
            {
                result = EnvMapProcessing.BrdfIntegrationLut(device, SampleCount);
                return result != null;
            }))
            {
                data.Info.ImportError = ImportErrors.Unknown;
                return;
            }

            using (result)
            {
#if DEBUG || DEBUG_EDITOR
                SaveDbgFile(result, data.ImportSettings.Sources, "brdf");
#endif
                CopySubresources(result, data);
                TextureInfoFromMetadata(result.GetMetadata(), ref data.Info);
            }
        }

#if DEBUG || DEBUG_EDITOR
        const string TmpFolder = "./tex_importer_dbg/";
        static void SaveDbgFile(ScratchImage image, string fileName, string assetType)
        {
            if (!Directory.Exists(TmpFolder))
            {
                Directory.CreateDirectory(TmpFolder);
            }

            string path = Path.Combine(TmpFolder, $"tmp_{Path.GetFileName(fileName)}_{assetType}.dds");
            image.SaveToDDSFile(DDS_FLAGS.NONE, path);
        }
#endif
    }
}
