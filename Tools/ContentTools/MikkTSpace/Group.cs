
namespace ContentTools.MikkTSpace
{
    record Group(int[] buffer)
    {
        private readonly int[] buffer = buffer;

        public int VertexRepresentitive;
        public bool OrientPreservering;
        public int FaceIndicesOffset;
        public int NFaces { get; private set; } = 0;

        public int GetFaceIndex(int index)
        {
            return buffer[index + FaceIndicesOffset];
        }

        public void AddTriToGroup(int triIndex)
        {
            buffer[NFaces + FaceIndicesOffset] = triIndex;
            NFaces++;
        }
    }
}
