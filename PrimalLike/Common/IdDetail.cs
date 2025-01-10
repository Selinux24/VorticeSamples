global using GenerationType = uint; //Based on GenerationBits: ushort when <=8, uint when <=16, ulong when <=32, else not possible
global using IdType = uint;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("PrimalLikeTests")]
namespace PrimalLike.Common
{
    public static class IdDetail
    {
        public const int GenerationBits = 10;
        public const int IndexBits = sizeof(IdType) * 8 - GenerationBits;
        public const IdType IndexMask = ((IdType)1ul << IndexBits) - 1u;
        public const IdType GenerationMask = ((IdType)1ul << GenerationBits) - 1u;
        public const IdType InvalidId = IdType.MaxValue;
        public const uint MinDeletedElements = 1024;

        public static bool IsValid(IdType id)
        {
            return id != InvalidId;
        }
        public static IdType Index(IdType id)
        {
            IdType index = id & IndexMask;
            Debug.Assert(index != IndexMask);
            return index;
        }
        public static IdType Generation(IdType id)
        {
            return id >> IndexBits & GenerationMask;
        }
        public static IdType NewGeneration(IdType id)
        {
            IdType generation = Generation(id) + 1;
            Debug.Assert(generation < GenerationMask);
            return Index(id) | generation << IndexBits;
        }

        public static string StringHash<T>()
        {
            string typeName = typeof(T).Name;
            byte[] byteArray = SHA256.HashData(Encoding.UTF8.GetBytes(typeName));
            string hash = Convert.ToHexString(byteArray);

            return hash;
        }
    }
}
