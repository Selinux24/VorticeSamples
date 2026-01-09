using PrimalLike.Content;
using PrimalLike.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using Utilities;
using Vortice.Direct3D12;
using D3DPrimitiveTopology = Vortice.Direct3D.PrimitiveTopology;
using D3DPrimitiveTopologyType = Vortice.Direct3D12.PrimitiveTopologyType;

namespace Direct3D12.Content
{
    static class Material
    {
        static readonly List<ID3D12RootSignature> rootSignatures = [];
        static readonly Dictionary<ulong, uint> mtlRsMap = []; // maps a material's type and shader flags to an index in the array of root signatures.
        static readonly FreeList<IntPtr> materials = new();
        static readonly Lock materialMutex = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {
            foreach (var item in rootSignatures)
            {
                item.Dispose();
            }

            mtlRsMap.Clear();
            rootSignatures.Clear();
        }

        public static uint Add(MaterialInitInfo info)
        {
            lock (materialMutex)
            {
                D3D12MaterialStream stream = new(info);
                Debug.Assert(stream.Buffer != IntPtr.Zero);
                return materials.Add(stream.Buffer);
            }
        }
        public static void Remove(uint id)
        {
            lock (materialMutex)
            {
                materials.Remove(id);
            }
        }

        public static void GetMaterials(uint[] materialIds, ref MaterialsCache cache, ref uint descriptorIndexCount)
        {
            Debug.Assert(materialIds != null && materialIds.Length > 0);
            Debug.Assert(cache.RootSignatures != null && cache.MaterialTypes != null);

            lock (materialMutex)
            {
                uint totalIndexCount = 0;

                for (uint i = 0; i < materialIds.Length; i++)
                {
                    var stream = new D3D12MaterialStream(materials[materialIds[i]]);

                    cache.RootSignatures[i] = rootSignatures[(int)stream.RootSignatureId];
                    cache.MaterialTypes[i] = stream.MaterialType;
                    cache.DescriptorIndices[i] = stream.DescriptorIndices;
                    cache.TextureCounts[i] = stream.TextureCount;
                    cache.MaterialSurfaces[i] = stream.Surface;
                    totalIndexCount += stream.TextureCount;
                }

                descriptorIndexCount = totalIndexCount;
            }
        }

        public static uint CreateRootSignature(MaterialTypes type, ShaderFlags flags)
        {
            Debug.Assert(type < MaterialTypes.Count);
            Debug.Assert(sizeof(MaterialTypes) == sizeof(uint) && sizeof(ShaderFlags) == sizeof(uint));
            ulong key = ((uint)type << 32) | (uint)flags;
            if (mtlRsMap.TryGetValue(key, out uint value))
            {
                return value;
            }

            ID3D12RootSignature rootSignature = null;

            switch (type)
            {
                case MaterialTypes.Opaque:
                {
                    RootParameter1[] parameters = new RootParameter1[(uint)OpaqueRootParameter.Count];

                    ShaderVisibility bufferVisibility = new();
                    ShaderVisibility dataVisibility = new();

                    if (flags.HasFlag(ShaderFlags.Vertex))
                    {
                        bufferVisibility = ShaderVisibility.Vertex;
                        dataVisibility = ShaderVisibility.Vertex;
                    }
                    else if (flags.HasFlag(ShaderFlags.Mesh))
                    {
                        bufferVisibility = ShaderVisibility.Mesh;
                        dataVisibility = ShaderVisibility.Mesh;
                    }

                    if (flags.HasFlag(ShaderFlags.Hull) ||
                        flags.HasFlag(ShaderFlags.Geometry) ||
                        flags.HasFlag(ShaderFlags.Amplification))
                    {
                        bufferVisibility = ShaderVisibility.All;
                        dataVisibility = ShaderVisibility.All;
                    }

                    if (flags.HasFlag(ShaderFlags.Pixel) ||
                        flags.HasFlag(ShaderFlags.Compute))
                    {
                        dataVisibility = ShaderVisibility.All;
                    }

                    parameters[(uint)OpaqueRootParameter.GlobalShaderData] = D3D12Helpers.AsCbv(ShaderVisibility.All, 0);
                    parameters[(uint)OpaqueRootParameter.PerObjectData] = D3D12Helpers.AsCbv(dataVisibility, 1);
                    parameters[(uint)OpaqueRootParameter.PositionBuffer] = D3D12Helpers.AsSrv(bufferVisibility, 0);
                    parameters[(uint)OpaqueRootParameter.ElementBuffer] = D3D12Helpers.AsSrv(bufferVisibility, 1);
                    parameters[(uint)OpaqueRootParameter.SrvIndices] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 2); // TODO: needs to be visible to any stages that need to sample textures.
                    parameters[(uint)OpaqueRootParameter.DirectionalLights] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 3);
                    parameters[(uint)OpaqueRootParameter.CullableLights] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 4);
                    parameters[(uint)OpaqueRootParameter.LightGrid] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 5);
                    parameters[(uint)OpaqueRootParameter.LightIndexList] = D3D12Helpers.AsSrv(ShaderVisibility.Pixel, 6);

                    StaticSamplerDescription[] samplers =
                    [
                        D3D12Helpers.StaticSampler(D3D12Helpers.SampleStatesCollection.StaticPoint, 0,0, ShaderVisibility.Pixel),
                        D3D12Helpers.StaticSampler(D3D12Helpers.SampleStatesCollection.StaticLinear, 1,0, ShaderVisibility.Pixel),
                        D3D12Helpers.StaticSampler(D3D12Helpers.SampleStatesCollection.StaticPoint, 2,0, ShaderVisibility.Pixel),
                    ];

                    var rootSignatureDesc = new D3D12RootSignatureDesc(parameters, GetRootSignatureFlags(flags), samplers);
                    rootSignature = rootSignatureDesc.Create();
                    Debug.Assert(rootSignature != null);
                }
                break;
            }

            Debug.Assert(rootSignature != null);
            uint id = (uint)rootSignatures.Count;
            rootSignatures.Add(rootSignature);
            mtlRsMap[key] = id;
            D3D12Helpers.NameD3D12Object(rootSignature, key, "GPass Root Signature - key");

            return id;
        }
        static RootSignatureFlags GetRootSignatureFlags(ShaderFlags flags)
        {
            RootSignatureFlags defaultFlags = D3D12RootSignatureDesc.DefaultFlags;
            if (flags.HasFlag(ShaderFlags.Vertex)) defaultFlags &= ~RootSignatureFlags.DenyVertexShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Hull)) defaultFlags &= ~RootSignatureFlags.DenyHullShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Domain)) defaultFlags &= ~RootSignatureFlags.DenyDomainShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Geometry)) defaultFlags &= ~RootSignatureFlags.DenyGeometryShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Pixel)) defaultFlags &= ~RootSignatureFlags.DenyPixelShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Amplification)) defaultFlags &= ~RootSignatureFlags.DenyAmplificationShaderRootAccess;
            if (flags.HasFlag(ShaderFlags.Mesh)) defaultFlags &= ~RootSignatureFlags.DenyMeshShaderRootAccess;
            return defaultFlags;
        }

        public static PsoId CreatePso(uint materialId, D3DPrimitiveTopology primitiveTopology, uint elementsType)
        {
            D3D12PipelineStateSubobjectStream stream = new();

            lock (materialMutex)
            {
                D3D12MaterialStream material = new(materials[materialId]);
                var shaders = GetMaterialShaders(material, elementsType);

                stream.RenderTargetFormats = new([D3D12GPass.MainBufferFormat]);
                stream.RootSignature = rootSignatures[(int)material.RootSignatureId];
                stream.PrimitiveTopology = GetD3DPrimitiveTopologyType(primitiveTopology);
                stream.DepthStencilFormat = D3D12GPass.DepthBufferFormat;
                stream.Rasterizer = D3D12Helpers.RasterizerStatesCollection.BackFaceCull;
                stream.DepthStencil1 = D3D12Helpers.DepthStatesCollection.ReversedReadonly;
                stream.Blend = D3D12Helpers.BlendStatesCollection.Disabled;

                stream.Vs = new(shaders[(uint)ShaderTypes.Vertex].ByteCode.Span);
                stream.Ps = new(shaders[(uint)ShaderTypes.Pixel].ByteCode.Span);
                stream.Ds = new(shaders[(uint)ShaderTypes.Domain].ByteCode.Span);
                stream.Hs = new(shaders[(uint)ShaderTypes.Hull].ByteCode.Span);
                stream.Gs = new(shaders[(uint)ShaderTypes.Geometry].ByteCode.Span);
                stream.Cs = new(shaders[(uint)ShaderTypes.Compute].ByteCode.Span);
                stream.As = new(shaders[(uint)ShaderTypes.Amplification].ByteCode.Span);
                stream.Ms = new(shaders[(uint)ShaderTypes.Mesh].ByteCode.Span);
            }

            uint gPassPsoId = RenderItem.CreatePsoIfNeeded(stream, false);

            lock (materialMutex)
            {
                stream.Ps = new([]);
                stream.DepthStencil1 = D3D12Helpers.DepthStatesCollection.Reversed;
            }

            uint depthPsoId = RenderItem.CreatePsoIfNeeded(stream, true);

            return new()
            {
                GpassPsoId = gPassPsoId,
                DepthPsoId = depthPsoId
            };
        }
        static D3DPrimitiveTopologyType GetD3DPrimitiveTopologyType(D3DPrimitiveTopology topology)
        {
            return topology switch
            {
                D3DPrimitiveTopology.PointList => D3DPrimitiveTopologyType.Point,
                D3DPrimitiveTopology.LineList or D3DPrimitiveTopology.LineStrip => D3DPrimitiveTopologyType.Line,
                D3DPrimitiveTopology.TriangleList or D3DPrimitiveTopology.TriangleStrip => D3DPrimitiveTopologyType.Triangle,
                _ => D3DPrimitiveTopologyType.Undefined,
            };
        }
        static CompiledShader[] GetMaterialShaders(D3D12MaterialStream material, uint elementsType)
        {
            var flags = material.ShaderFlags;
            var shaders = new CompiledShader[(uint)ShaderTypes.Count];

            uint shaderIndex = 0;
            for (uint i = 0; i < (uint)ShaderTypes.Count; i++)
            {
                ShaderFlags shaderFlags = (ShaderFlags)(1u << (int)i);
                if (!flags.HasFlag(shaderFlags)) continue;

                ShaderTypes shaderType = GetShaderType(shaderFlags);
                uint key = shaderType == ShaderTypes.Vertex ? elementsType : uint.MaxValue;
                uint shaderId = material.ShaderIds[shaderIndex];
                shaders[i] = ContentToEngine.GetShader(shaderId, key);
                Debug.Assert(shaders[i].ByteCodeSize > 0);
                shaderIndex++;
            }

            return shaders;
        }
        static ShaderTypes GetShaderType(ShaderFlags flag)
        {
            return (ShaderTypes)BitOperations.TrailingZeroCount((uint)flag);
        }
    }
}
