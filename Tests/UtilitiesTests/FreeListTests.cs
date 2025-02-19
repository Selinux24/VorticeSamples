using NUnit.Framework;
using System;
using System.Collections.Generic;
using Utilities;

namespace UtilitiesTests
{
    public class FreeListTests
    {
        private const int randMax = 0x7fff;
        private const float invRandMax = 1f / randMax;
        private static readonly Random rand = new(37);
        private static float Random(float min = 0f)
        {
            float v = rand.Next(0, randMax) * invRandMax;

            return MathF.Max(min, v);
        }

        private readonly List<TestStruct> lights = [];
        private readonly List<TestStruct> disabledLights = [];
        private readonly FreeList<TestStruct> freeList = new();

        [Test()]
        public void AddRemoveClassTest()
        {
            FreeList<TestClass> list = new(10);

            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.True);

            uint id0 = list.Add(new TestClass() { InternalId = 0 });
            Assert.That(list.Size, Is.EqualTo(1));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            uint id1 = list.Add(new TestClass() { InternalId = 1 });
            Assert.That(list.Size, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            uint id2 = list.Add(new TestClass() { InternalId = 2 });
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id1);
            uint id3 = list.Add(new TestClass() { InternalId = 3 });
            Assert.That(id1, Is.EqualTo(id3));
            Assert.That(list[0].InternalId, Is.EqualTo(0));
            Assert.That(list[1].InternalId, Is.EqualTo(3));
            Assert.That(list[2].InternalId, Is.EqualTo(2));
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id0);
            uint id4 = list.Add(new TestClass() { InternalId = 4 });
            Assert.That(id0, Is.EqualTo(id4));
            Assert.That(list[0].InternalId, Is.EqualTo(4));
            Assert.That(list[1].InternalId, Is.EqualTo(3));
            Assert.That(list[2].InternalId, Is.EqualTo(2));
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id2);
            uint id5 = list.Add(new TestClass() { InternalId = 5 });
            Assert.That(id2, Is.EqualTo(id5));
            Assert.That(list[0].InternalId, Is.EqualTo(4));
            Assert.That(list[1].InternalId, Is.EqualTo(3));
            Assert.That(list[2].InternalId, Is.EqualTo(5));
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id4);
            Assert.That(list.Size, Is.EqualTo(2));
            Assert.That(list.Empty, Is.False);
            list.Remove(id3);
            Assert.That(list.Size, Is.EqualTo(1));
            Assert.That(list.Empty, Is.False);
            list.Remove(id5);
            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Empty, Is.True);
        }
        [Test()]
        public void AddRemoveStructTest()
        {
            FreeList<TestStruct> list = new(10);

            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.True);

            uint id0 = list.Add(new TestStruct() { InternalId = 0 });
            Assert.That(list.Size, Is.EqualTo(1));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            uint id1 = list.Add(new TestStruct() { InternalId = 1 });
            Assert.That(list.Size, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            uint id2 = list.Add(new TestStruct() { InternalId = 2 });
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id1);
            uint id3 = list.Add(new TestStruct() { InternalId = 3 });
            Assert.That(id1, Is.EqualTo(id3));
            Assert.That(list[0].InternalId, Is.EqualTo(0));
            Assert.That(list[1].InternalId, Is.EqualTo(3));
            Assert.That(list[2].InternalId, Is.EqualTo(2));
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id0);
            uint id4 = list.Add(new TestStruct() { InternalId = 4 });
            Assert.That(id0, Is.EqualTo(id4));
            Assert.That(list[0].InternalId, Is.EqualTo(4));
            Assert.That(list[1].InternalId, Is.EqualTo(3));
            Assert.That(list[2].InternalId, Is.EqualTo(2));
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id2);
            uint id5 = list.Add(new TestStruct() { InternalId = 5 });
            Assert.That(id2, Is.EqualTo(id5));
            Assert.That(list[0].InternalId, Is.EqualTo(4));
            Assert.That(list[1].InternalId, Is.EqualTo(3));
            Assert.That(list[2].InternalId, Is.EqualTo(5));
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Remove(id4);
            Assert.That(list.Size, Is.EqualTo(2));
            Assert.That(list.Empty, Is.False);
            list.Remove(id3);
            Assert.That(list.Size, Is.EqualTo(1));
            Assert.That(list.Empty, Is.False);
            list.Remove(id5);
            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Empty, Is.True);
        }

        [Test()]
        public void ClearClassTest()
        {
            FreeList<TestClass> list = new(10);

            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.True);

            list.Add(new TestClass() { InternalId = 0 });
            Assert.That(list.Size, Is.EqualTo(1));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Add(new TestClass() { InternalId = 1 });
            Assert.That(list.Size, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Add(new TestClass() { InternalId = 2 });
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Clear();
            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Empty, Is.True);
        }
        [Test()]
        public void ClearStructTest()
        {
            FreeList<TestStruct> list = new(10);

            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.True);

            list.Add(new TestStruct() { InternalId = 0 });
            Assert.That(list.Size, Is.EqualTo(1));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Add(new TestStruct() { InternalId = 1 });
            Assert.That(list.Size, Is.EqualTo(2));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Add(new TestStruct() { InternalId = 2 });
            Assert.That(list.Size, Is.EqualTo(3));
            Assert.That(list.Capacity, Is.EqualTo(10));
            Assert.That(list.Empty, Is.False);

            list.Clear();
            Assert.That(list.Size, Is.EqualTo(0));
            Assert.That(list.Empty, Is.True);
        }

        [Test()]
        public void RandomTest()
        {
            for (int i = 0; i < 1000; i++)
            {
                Assert.DoesNotThrow(RandomCreate);
            }
        }
        private void RandomCreate()
        {
            uint count = (uint)(Random(0.1f) * 100);
            for (int i = 0; i < count; i++)
            {
                if (lights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (lights.Count - 1));
                var light = lights[index];
                lights.RemoveAt(index);
                disabledLights.Add(light);
            }

            count = (uint)(Random(0.1f) * 50);
            for (int i = 0; i < count; i++)
            {
                if (lights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (lights.Count - 1));
                var light = lights[index];
                freeList.Remove((uint)light.InternalId);
                lights.RemoveAt(index);
            }

            count = (uint)(Random(0.1f) * 50);
            for (int i = 0; i < count; i++)
            {
                if (disabledLights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (disabledLights.Count - 1));
                var light = disabledLights[index];
                freeList.Remove((uint)light.InternalId);
                disabledLights.RemoveAt(index);
            }

            count = (uint)(Random(0.1f) * 100);
            for (int i = 0; i < count; i++)
            {
                if (disabledLights.Count == 0)
                {
                    break;
                }
                int index = (int)(Random() * (disabledLights.Count - 1));
                var light = disabledLights[index];
                disabledLights.RemoveAt(index);
                lights.Add(light);
            }

            count = (uint)(Random(0.1f) * 50);
            for (uint i = 0; i < count; i++)
            {
                TestStruct t1 = new() { Value = Random() };
                t1.InternalId = (int)freeList.Add(t1);
                lights.Add(t1);

                TestStruct t2 = new() { Value = Random() };
                t2.InternalId = (int)freeList.Add(t2);
                lights.Add(t2);
            }
        }
    }

    class TestClass
    {
        public int InternalId { get; set; }
        public float Value { get; set; }
    }

    struct TestStruct
    {
        public int InternalId { get; set; }
        public float Value { get; set; }
    }
}
