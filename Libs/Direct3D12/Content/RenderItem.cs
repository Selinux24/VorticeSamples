using PrimalLike.Common;
using PrimalLike.Content;
using PrimalLike.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Utilities;
using Vortice.Direct3D12;

namespace Direct3D12.Content
{
    public static class RenderItem
    {
        static readonly FreeList<D3D12RenderItem> renderItems = new();
        static readonly FreeList<uint[]> renderItemIds = new();
        static readonly object renderItemMutex = new();

        static readonly List<ID3D12PipelineState> pipelineStates = [];
        static readonly Dictionary<ulong, uint> psoMap = [];
        static readonly object psoMutex = new();

        static readonly FrameCache frameCache = new();

        public static bool Initialize()
        {
            return true;
        }
        public static void Shutdown()
        {
            foreach (var item in pipelineStates)
            {
                item.Dispose();
            }

            psoMap.Clear();
            pipelineStates.Clear();
        }

        /// <summary>
        /// Creates a buffer that's basically an array of IdType
        /// </summary>
        /// <remarks>
        /// buffer[0] = geometry_content_id
        /// buffer[1 .. n] = d3d12_render_item_ids (n is the number of low-level render item ids which must also equal the number of submeshes/material ids).
        /// buffer[n + 1] = id::invalid_id (this marks the end of submesh_gpu_id array).
        /// </remarks>
        public static uint Add(uint entityId, uint geometryContentId, uint[] materialIds)
        {
            Debug.Assert(IdDetail.IsValid(entityId) && IdDetail.IsValid(geometryContentId));
            uint materialCount = (uint)(materialIds?.Length ?? 0);
            Debug.Assert(materialCount > 0);
            ContentToEngine.GetSubmeshGpuIds(geometryContentId, materialCount, out uint[] gpuIds);

            ViewsCache viewsCache = new();
            Submesh.GetViews(gpuIds, ref viewsCache);

            // NOTE: the list of ids starts with geomtery id and ends with an invalid id to mark the end of the list.
            uint[] items = new uint[1 + materialCount + 1];

            items[0] = geometryContentId;

            lock (renderItemMutex)
            {
                for (uint i = 0; i < materialCount; i++)
                {
                    PsoId idPair = Material.CreatePso(materialIds[i], viewsCache.PrimitiveTopologies[i], viewsCache.ElementsTypes[i]);

                    D3D12RenderItem item = new()
                    {
                        EntityId = entityId,
                        SubmeshGpuId = gpuIds[i],
                        MaterialId = materialIds[i],
                        PsoId = idPair.GpassPsoId,
                        DepthPsoId = idPair.DepthPsoId
                    };

                    Debug.Assert(IdDetail.IsValid(item.SubmeshGpuId) && IdDetail.IsValid(item.MaterialId));
                    items[i + 1] = renderItems.Add(item);
                }

                // mark the end of ids list.
                items[materialCount + 1] = IdDetail.InvalidId;

                return renderItemIds.Add(items);
            }
        }
        public static void Remove(uint id)
        {
            lock (renderItemMutex)
            {
                var items = renderItemIds[id];

                // NOTE: the last element in the list of ids is always an invalid id.
                for (uint i = 1; items[i] != IdDetail.InvalidId; i++)
                {
                    renderItems.Remove(items[i]);
                }

                renderItemIds.Remove(id);
            }
        }
        public static void GetD3D12RenderItemIds(ref FrameInfo info, ref uint[] d3d12RenderItemIds)
        {
            Debug.Assert(info.RenderItemIds != null && info.Thresholds != null && info.RenderItemCount != 0);
            Debug.Assert(d3d12RenderItemIds.Length == 0);

            frameCache.LodOffsets.Clear();
            frameCache.GeometryIds.Clear();
            uint count = info.RenderItemCount;

            lock (renderItemMutex)
            {
                for (uint i = 0; i < count; i++)
                {
                    var buffer = renderItemIds[info.RenderItemIds[i]];
                    frameCache.GeometryIds.Add(buffer[0]);
                }

                ContentToEngine.GetLodOffsets([.. frameCache.GeometryIds], info.Thresholds, count, frameCache.LodOffsets);
                Debug.Assert(frameCache.LodOffsets.Count == count);

                uint d3d12RenderItemCount = 0;
                for (uint i = 0; i < count; i++)
                {
                    d3d12RenderItemCount += frameCache.LodOffsets[(int)i].Count;
                }

                Debug.Assert(d3d12RenderItemCount > 0);
                Array.Resize(ref d3d12RenderItemIds, (int)d3d12RenderItemCount);

                uint itemIndex = 0;
                for (uint i = 0; i < count; i++)
                {
                    var items = renderItemIds[info.RenderItemIds[i]];
                    var lodOffset = frameCache.LodOffsets[(int)i];

                    Array.Copy(items, lodOffset.Offset + 1, d3d12RenderItemIds, itemIndex, lodOffset.Count);
                    itemIndex += lodOffset.Count;
                    Debug.Assert(itemIndex <= d3d12RenderItemCount);
                }

                Debug.Assert(itemIndex <= d3d12RenderItemCount);
            }
        }
        internal static void GetItems(uint[] d3d12RenderItemIds, ref ItemsCache cache)
        {
            Debug.Assert(d3d12RenderItemIds != null && d3d12RenderItemIds.Length > 0);
            Debug.Assert(
                cache.EntityIds != null &&
                cache.SubmeshGpuIds != null &&
                cache.MaterialIds != null &&
                cache.GPassPsos != null &&
                cache.DepthPsos != null);

            lock (renderItemMutex)
            {
                lock (psoMutex)
                {
                    for (uint i = 0; i < d3d12RenderItemIds.Length; i++)
                    {
                        var item = renderItems[d3d12RenderItemIds[i]];

                        cache.EntityIds[i] = item.EntityId;
                        cache.SubmeshGpuIds[i] = item.SubmeshGpuId;
                        cache.MaterialIds[i] = item.MaterialId;
                        cache.GPassPsos[i] = pipelineStates[(int)item.PsoId];
                        cache.DepthPsos[i] = pipelineStates[(int)item.DepthPsoId];
                    }
                }
            }
        }

        public static uint CreatePsoIfNeeded<T>(T data, bool isDepth) where T : unmanaged
        {
            // calculate Crc32 hash of the data.
            ulong key = (ulong)D3D12Helpers.GetStableHashCode(data);
            lock (psoMutex)
            {
                if (psoMap.TryGetValue(key, out uint pair))
                {
                    return pair;
                }
            }

            var pso = D3D12Graphics.Device.CreatePipelineState(data);

            lock (psoMutex)
            {
                uint id = (uint)pipelineStates.Count;
                pipelineStates.Add(pso);

                D3D12Helpers.NameD3D12Object(pso, key, isDepth ? "Depth-only Pipeline State Object - key" : "GPass Pipeline State Object - key");

                Debug.Assert(IdDetail.IsValid(id));
                psoMap[key] = id;
                return id;
            }
        }
    }
}
