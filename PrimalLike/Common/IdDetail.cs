﻿global using GenerationType = ushort; //Based on GenerationBits: ushort when <=8, uint when <=16, ulong when <=32, else not possible
global using IdType = uint;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace PrimalLike.Common
{
    public static class IdDetail
    {
        public const IdType One = 1u;
        public const uint GenerationBits = 8;
        public const uint IndexBits = sizeof(IdType) * 8 - GenerationBits;
        public const IdType IndexMask = (One << (int)IndexBits) - 1;
        public const IdType GenerationMask = (One << (int)GenerationBits) - 1;
        public const uint MinDeletedElements = 1024;

        public static bool IsValid(IdType id)
        {
            return id != IdType.MaxValue;
        }
        public static IdType Index(IdType id)
        {
            return id & IndexMask;
        }
        public static IdType Generation(IdType id)
        {
            return id >> (int)IndexBits & GenerationMask;
        }
        public static IdType NewGeneration(IdType id)
        {
            IdType generation = Generation(id) + One;
            Debug.Assert(generation < (One << (int)GenerationBits) - 1);
            return Index(id) | generation << (int)IndexBits;
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
