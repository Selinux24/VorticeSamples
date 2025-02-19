using NUnit.Framework;
using NUnit.Framework.Internal;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;

namespace PrimalLikeTests.Components
{
    public class EntityComponentTests
    {
        private readonly Randomizer rand = Randomizer.CreateRandomizer();
        private readonly List<Entity> entities = [];
        private int added = 0;
        private int removed = 0;
        private int numEntities = 0;

        [Test()]
        public void CreateTest()
        {
            for (int r = 0; r < 100; ++r)
            {
                for (int i = 0; i < 1000; ++i)
                {
                    CreateRandom();
                    RemoveRandom();
                    numEntities = entities.Count;
                }

                Assert.That(numEntities, Is.EqualTo(added - removed));
                Console.WriteLine($"Iteration:{r} --> Entities: {numEntities}, Added: {added}, Removed: {removed}");
            }
        }
        private void CreateRandom()
        {
            int count = rand.Next() % 20;
            if (entities.Count == 0)
            {
                count = 1000;
            }
            TransformInfo transformInfo = new();
            EntityInfo entityInfo = new()
            {
                Transform = transformInfo,
            };

            while (count > 0)
            {
                ++added;
                Entity entity = GameEntity.Create(entityInfo);
                Assert.That(entity.IsValid, Is.EqualTo(IdDetail.IsValid(entity.Id)), "The entity id is not valid");
                entities.Add(entity);
                Assert.That(GameEntity.IsAlive(entity.Id), Is.True, "The entity is not alive");
                --count;
            }
        }
        private void RemoveRandom()
        {
            int count = rand.Next() % 20;
            if (entities.Count < 1000)
            {
                return;
            }

            while (count > 0)
            {
                int index = rand.Next() % entities.Count;
                Entity entity = entities[index];
                Assert.That(entity.IsValid, Is.EqualTo(IdDetail.IsValid(entity.Id)), "The entity id is not valid");
                if (entity.IsValid)
                {
                    GameEntity.Remove(entity.Id);
                    entities.RemoveAt(index);
                    Assert.That(!GameEntity.IsAlive(entity.Id), Is.True, "The entity is alive");
                    ++removed;
                }
                --count;
            }
        }
    }
}