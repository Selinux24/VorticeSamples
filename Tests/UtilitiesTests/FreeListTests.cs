using NUnit.Framework;
using Utilities;

namespace UtilitiesTests
{
    public class FreeListTests
    {
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
