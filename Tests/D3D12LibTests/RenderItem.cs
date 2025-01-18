using Direct3D12;
using PrimalLike.Common;
using PrimalLike.Components;
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
            ShaderFileInfo info = new("TestShader.hlsl", "TestShaderVS", ShaderStage.Vertex);
            bool compiledVs = Compiler.Compile(shadersSourcePath, info, out var vertexShader);
            Debug.Assert(compiledVs);

            info = new ShaderFileInfo("TestShader.hlsl", "TestShaderPS", ShaderStage.Pixel);
            bool compiledPs = Compiler.Compile(shadersSourcePath, info, out var pixelShader);
            Debug.Assert(compiledPs);

            vsId = ContentToEngine.AddShader(new PrimalLike.Content.CompiledShader()
            {
                ByteCodeSize = (ulong)vertexShader.ByteCode.Length,
                ByteCode = vertexShader.ByteCode,
                Hash = vertexShader.Hash.HashDigest
            });

            psId = ContentToEngine.AddShader(new PrimalLike.Content.CompiledShader()
            {
                ByteCodeSize = (ulong)pixelShader.ByteCode.Length,
                ByteCode = pixelShader.ByteCode,
                Hash = pixelShader.Hash.HashDigest
            });
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
            uint itemId = D3D12Content.AddRenderItem(0, modelId, materials);

            renderItemEntityMap[itemId] = entityId;
            return itemId;
        }
        public static void DestroyRenderItem(uint itemId)
        {
            // remove the render item from engine (also the game entity)
            if (IdDetail.IsValid(itemId))
            {
                D3D12Content.RemoveRenderItem(itemId);
                var pair = renderItemEntityMap[itemId];
                GameEntity.Remove(pair);
            }

            // remove material
            if (IdDetail.IsValid(mtlId))
            {
                ContentToEngine.DestroyResource(mtlId, AssetTypes.Material);
            }

            // remove shaders and textures
            if (IdDetail.IsValid(vsId))
            {
                ContentToEngine.RemoveShader(vsId);
            }

            if (IdDetail.IsValid(psId))
            {
                ContentToEngine.RemoveShader(psId);
            }

            // remove model
            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }
        }
    }
}
