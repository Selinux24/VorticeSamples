using ContentTools;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Content;
using PrimalLike.Graphics;
using ShaderCompiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace D3D12LibTests
{
    class RenderItem
    {
        private const string shadersSourcePath = "./Shaders/";
        private const string shadersIncludeDir = "../../../../../Libs/Direct3D12/Shaders/";

        private static uint modelId = IdDetail.InvalidId;
        private static uint vsId = IdDetail.InvalidId;
        private static uint psId = IdDetail.InvalidId;
        private static uint mtlId = IdDetail.InvalidId;

        private static readonly Dictionary<uint, uint> renderItemEntityMap = [];

        private static void LoadModel()
        {
            string modelPath = Path.GetFullPath("./Content/Model.model");
            using var file = new MemoryStream(File.ReadAllBytes(modelPath));

            modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Debug.Assert(IdDetail.IsValid(modelId));
        }
        private static void LoadShaders()
        {
            // Let's say our material uses a vertex shader and a pixel shader.
            ShaderFileInfo info = new(Path.Combine(shadersSourcePath, "TestShader.hlsl"), "TestShaderVS", ShaderStage.Vertex);

            string[] defines = ["ELEMENTS_TYPE=1", "ELEMENTS_TYPE=3"];
            uint[] keys =
            [
                (uint)ElementsType.StaticNormal,
                (uint)ElementsType.StaticNormalTexture,
            ];

            List<string> extraArgs = [];
            PrimalLike.Content.CompiledShader[] vertexShaders = new PrimalLike.Content.CompiledShader[2];
            for (uint i = 0; i < defines.Length; i++)
            {
                extraArgs.Clear();
                extraArgs.Add("-D");
                extraArgs.Add(defines[i]);
                bool compiledVs = Compiler.Compile(info, shadersIncludeDir, extraArgs, out var vertexShader);
                Debug.Assert(compiledVs);
                vertexShaders[i] = new()
                {
                    ByteCodeSize = (ulong)vertexShader.ByteCode.Length,
                    ByteCode = vertexShader.ByteCode,
                    Hash = vertexShader.Hash.HashDigest
                };
            }

            vsId = ContentToEngine.AddShaderGroup(vertexShaders, keys);

            info = new ShaderFileInfo(Path.Combine(shadersSourcePath, "TestShader.hlsl"), "TestShaderPS", ShaderStage.Pixel);
            bool compiledPs = Compiler.Compile(info, shadersIncludeDir, out var pixelShader);
            Debug.Assert(compiledPs);

            PrimalLike.Content.CompiledShader[] pixelShaders =
            [
                new PrimalLike.Content.CompiledShader()
                {
                    ByteCodeSize = (ulong)pixelShader.ByteCode.Length,
                    ByteCode = pixelShader.ByteCode,
                    Hash = pixelShader.Hash.HashDigest
                }
            ];

            psId = ContentToEngine.AddShaderGroup(pixelShaders, [uint.MaxValue]);
        }
        public static void CreateMaterial()
        {
            MaterialInitInfo info = new();
            info.ShaderIds[(uint)ShaderTypes.Vertex] = vsId;
            info.ShaderIds[(uint)ShaderTypes.Pixel] = psId;
            info.Type = MaterialTypes.Opaque;
            mtlId = ContentToEngine.CreateResource(info, AssetTypes.Material);
        }

        public static uint CreateRenderItem(uint entityId)
        {
            // load a model, pretend it belongs to entity_id
            var _1 = new Thread(LoadModel);
            _1.Start();

            // load a material:
            // 1) load textures, oh nooooo we don't have any, but that's ok.
            // 2) load shaders for that material
            var _2 = new Thread(LoadShaders);
            _2.Start();

            _1.Join();
            _2.Join();
            // add a render item using the model and its materials.
            CreateMaterial();
            uint[] materials = [mtlId, mtlId, mtlId, mtlId, mtlId];

            // TODO: add add_render_item in renderer.
            uint itemId = Direct3D12.Content.RenderItem.Add(entityId, modelId, materials);

            renderItemEntityMap[itemId] = entityId;
            return itemId;
        }
        public static void DestroyRenderItem(uint itemId)
        {
            // remove the render item from engine (also the game entity)
            if (IdDetail.IsValid(itemId))
            {
                Direct3D12.Content.RenderItem.Remove(itemId);
                var pair = renderItemEntityMap[itemId];
                Application.RemoveEntity(pair);
            }

            // remove material
            if (IdDetail.IsValid(mtlId))
            {
                ContentToEngine.DestroyResource(mtlId, AssetTypes.Material);
            }

            // remove shaders and textures
            if (IdDetail.IsValid(vsId))
            {
                ContentToEngine.RemoveShaderGroup(vsId);
            }

            if (IdDetail.IsValid(psId))
            {
                ContentToEngine.RemoveShaderGroup(psId);
            }

            // remove model
            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }
        }
    }
}
