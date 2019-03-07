namespace NyuuGames.Mugen.ECS.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using FluentAssertions;
    using Xunit;

    public class EntityManagerTest
    {
        [Fact]
        public unsafe void CheckIfEntityExists()
        {
            using var entityDefinitionManager = new EntityManager();
            var definition = new BlueprintDefinition();
            var bp = new Blueprint((BlueprintDefinition*) Unsafe.AsPointer(ref definition));

            var entity = entityDefinitionManager.CreateEntity(in bp);

            entityDefinitionManager.EntityExists(entity).Should().BeTrue();

            entityDefinitionManager.EntityExists(new Entity(1, 0)).Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(GetMatrixData))]
        public unsafe void CreateEntities(int amount, int capacity)
        {
            using var entityDefinitionManager = new EntityManager(capacity);
            var definition = new BlueprintDefinition();
            var bp = new Blueprint((BlueprintDefinition*) Unsafe.AsPointer(ref definition));

            for (var i = 0; i < amount; ++i)
            {
                var entity = entityDefinitionManager.CreateEntity(in bp);

                entityDefinitionManager.EntityExists(entity).Should().BeTrue();
            }
            
        }

        [Theory]
        [MemberData(nameof(GetMatrixData))]
        public unsafe void CreateAndDeleteAndCreateEntities(int amount, int capacity)
        {
            using var entityDefinitionManager = new EntityManager(capacity);
            var entities = new List<Entity>(amount/2);

            var definition = new BlueprintDefinition();
            var bp = new Blueprint((BlueprintDefinition*) Unsafe.AsPointer(ref definition));

            for (var i = 0; i < amount; ++i)
            {
                var entity = entityDefinitionManager.CreateEntity(in bp);

                entityDefinitionManager.EntityExists(entity).Should().BeTrue();
                
                if(i%2==0) entities.Add(entity);
            }

            foreach (var entity in entities)
            {
                entityDefinitionManager.DeleteEntity(entity);

                entityDefinitionManager.EntityExists(entity).Should().BeFalse();
            }

            for (var i = amount/2; i < amount; ++i)
            {
                var entity = entityDefinitionManager.CreateEntity(in bp);

                entityDefinitionManager.EntityExists(entity).Should().BeTrue();
            }
        }

        public static IEnumerable<object[]> GetMatrixData()
        {
            var amount = Enumerable.Range(1, 12).Select(i => (int) Math.Pow(2, i));
            var capacity = Enumerable.Range(0, 12).Select(i => (int) Math.Pow(2, i)).ToList();

            var list = new List<object[]>(12*12);
            foreach (var a in amount)
            {
                foreach (var c in capacity)
                {
                    list.Add(new object[] { a, c});
                }
            }

            return list;
        }
    }
}