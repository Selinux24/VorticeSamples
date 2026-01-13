using System;
using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    /// <summary>
    /// Resource barrier helper class.
    /// </summary>
    class D3D12ResourceBarrier : IDisposable
    {
        const int MaxResourceBarriers = 64;

        readonly ResourceBarrier[] barriers;
        uint offset;

        public D3D12ResourceBarrier()
        {
            barriers = new ResourceBarrier[MaxResourceBarriers];
            offset = 0;
        }
        ~D3D12ResourceBarrier()
        {
            Dispose(false);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            if (!disposing) return;

            offset = 0;
            for (int i = 0; i < barriers.Length; i++)
            {
                barriers[i] = default;
            }
        }

        /// <summary>
        /// Adds a transition barrier to the list of barriers.
        /// </summary>
        public void AddTransitionBarrier(
            ID3D12Resource resource,
            ResourceStates before,
            ResourceStates after,
            ResourceBarrierFlags flags = ResourceBarrierFlags.None,
            uint subresource = D3D12.ResourceBarrierAllSubResources)
        {
            Debug.Assert(resource != null);
            Debug.Assert(offset < MaxResourceBarriers);

            ResourceTransitionBarrier transition = new(resource, before, after, subresource);

            barriers[offset++] = new(transition, flags);
        }

        /// <summary>
        /// Adds a UAV barrier to the list of barriers.
        /// </summary>
        public void AddUAVBarrier(ID3D12Resource resource)
        {
            Debug.Assert(resource != null);
            Debug.Assert(offset < MaxResourceBarriers);

            ResourceUnorderedAccessViewBarrier uav = new(resource);

            barriers[offset++] = new(uav);
        }

        public void AddAliasingBarrier(ID3D12Resource resourceBefore, ID3D12Resource resourceAfter)
        {
            Debug.Assert(resourceBefore != null && resourceAfter != null);
            Debug.Assert(offset < MaxResourceBarriers);

            ResourceAliasingBarrier aliasing = new(resourceBefore, resourceAfter);

            barriers[offset++] = new(aliasing);
        }

        public void Apply(ID3D12GraphicsCommandList cmdList)
        {
            Debug.Assert(offset > 0);
            cmdList.ResourceBarrier(offset, barriers);
            offset = 0;
        }
    }
}
