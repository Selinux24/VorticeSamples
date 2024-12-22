using System.Diagnostics;
using Vortice.Direct3D12;

namespace Direct3D12
{
    /// <summary>
    /// Resource barrier helper class.
    /// </summary>
    public class D3D12ResourceBarrier
    {
        const int MaxResourceBarriers = 32;

        private readonly ResourceBarrier[] barriers = new ResourceBarrier[MaxResourceBarriers];
        private int offset = 0;

        /// <summary>
        /// Adds a transition barrier to the list of barriers.
        /// </summary>
        public void Add(
            ID3D12Resource resource,
            ResourceStates before,
            ResourceStates after,
            ResourceBarrierFlags flags = ResourceBarrierFlags.None,
            int subresource = D3D12.ResourceBarrierAllSubResources)
        {
            Debug.Assert(resource != null);
            Debug.Assert(offset < MaxResourceBarriers);

            ResourceTransitionBarrier transition = new(resource, before, after, subresource);

            barriers[offset++] = new(transition, flags);
        }

        /// <summary>
        /// Adds a UAV barrier to the list of barriers.
        /// </summary>
        public void Add(
            ID3D12Resource resource,
            ResourceBarrierFlags flags = ResourceBarrierFlags.None)
        {
            Debug.Assert(resource != null);
            Debug.Assert(offset < MaxResourceBarriers);

            ResourceUnorderedAccessViewBarrier uav = new(resource);

            barriers[offset++] = new(uav);
        }

        public void Add(
            ID3D12Resource resourceBefore,
            ID3D12Resource resourceAfter,
            ResourceBarrierFlags flags = ResourceBarrierFlags.None)
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
