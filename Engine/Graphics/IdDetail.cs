//Based on GenerationBits: ushort when <=8, uint when <=16, ulong when <=32, else not possible
global using GenerationType = ushort;
using System.Diagnostics;

namespace Engine.Graphics
{
    static class IdDetail
    {
        const IdType One = 1u;
        public const uint GenerationBits = 8;
        public const uint IndexBits = sizeof(IdType) * 8 - GenerationBits;
        public const IdType IndexMask = (One << (int)IndexBits) - 1;
        public const IdType GenerationMask = (One << (int)GenerationBits) - 1;
        public const IdType InvalidId = One - 1;

        public static bool IsValid(IdType id)
        {
            return id != InvalidId;
        }
        public static IdType Index(IdType id)
        {
            return id & IndexMask;
        }
        public static IdType Generation(IdType id)
        {
            return (id >> (int)IndexBits) & GenerationMask;
        }
        public static IdType NewGeneration(IdType id)
        {
            IdType generation = Generation(id) + One;
            Debug.Assert(generation < ((One << (int)GenerationBits) - 1));
            return Index(id) | (generation << (int)IndexBits);
        }
    }
}
