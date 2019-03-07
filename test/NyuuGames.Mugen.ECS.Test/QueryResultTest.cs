namespace NyuuGames.Mugen.ECS.Test
{
    using System.Numerics;
    using FluentAssertions;
    using Xunit;

    public class QueryResultTest
    {
        [Fact]
        public void IterateOver_ThreeEntitiesTwoBlueprints()
        {
            var entityManager = new EntityManager();

            var entity = entityManager.CreateEntity(typeof(Position), typeof(Velocity));
            var entity2 = entityManager.CreateEntity(typeof(Position));
            var entity3 = entityManager.CreateEntity(typeof(Position), typeof(Velocity));

            var blueprint = entityManager.GetBlueprint(typeof(Position), typeof(Velocity));
            var blueprint2 = entityManager.GetBlueprint(typeof(Position));

            entityManager.GetComponent<Position>(entity).Value = Vector3.UnitX;
            entityManager.GetComponent<Position>(entity2).Value = Vector3.UnitY;
            entityManager.GetComponent<Position>(entity3).Value = Vector3.UnitZ;

            var queryResult = new QueryResult();

            queryResult.AddBlueprint(blueprint2);
            queryResult.AddBlueprint(blueprint);

            var i = 0;
            foreach (ref readonly var idx in queryResult)
            {
                ++i;
            }

            i.Should().Be(3);
        }

        [Fact]
        public void IterateOver_OneEntityOneBlueprint()
        {
            var entityManager = new EntityManager();

            var entity = entityManager.CreateEntity(typeof(Position), typeof(Velocity));

            var blueprint = entityManager.GetBlueprint(typeof(Position), typeof(Velocity));
            
            ref var entityPosition = ref entityManager.GetComponent<Position>(entity);

            entityPosition.Value = Vector3.UnitX;

            var queryResult = new QueryResult();
            queryResult.AddBlueprint(blueprint);

            var postions = queryResult.GetComponentArray<Position>();
            var i = 0;
            foreach (ref readonly var idx in queryResult)
            {
                ++i;
                ref var position = ref postions[idx];
                (position.Value == Vector3.UnitX).Should().BeTrue();
                position.Value = Vector3.UnitY;
            }
            (entityPosition.Value == Vector3.UnitY).Should().BeTrue();
            i.Should().Be(1);
        }

        [Fact]
        public void IterateOver_ThreeEntitiesThreeBlueprintsOneEmtpy()
        {
            var entityManager = new EntityManager();

            var entity = entityManager.CreateEntity(typeof(Position), typeof(Velocity));
            var entity2 = entityManager.CreateEntity(typeof(Position));
            var entity3 = entityManager.CreateEntity(typeof(Position), typeof(Velocity));

            var blueprint = entityManager.GetBlueprint(typeof(Position), typeof(Velocity));
            var blueprint3 = entityManager.GetBlueprint(typeof(Position), typeof(Velocity), typeof(Test));
            var blueprint2 = entityManager.GetBlueprint(typeof(Position));

            entityManager.GetComponent<Position>(entity).Value = Vector3.UnitX;
            entityManager.GetComponent<Position>(entity2).Value = Vector3.UnitY;
            entityManager.GetComponent<Position>(entity3).Value = Vector3.UnitZ;

            var queryResult = new QueryResult();

            queryResult.AddBlueprint(blueprint2);
            queryResult.AddBlueprint(blueprint3);
            queryResult.AddBlueprint(blueprint);

            var i = 0;
            foreach (ref readonly var idx in queryResult)
            {
                ++i;
            }

            i.Should().Be(3);
        }

        private struct Position : IComponent
        {
            public Vector3 Value;
        }

        private struct Velocity : IComponent
        {
            public Vector3 Value;
        }

        private struct Test : IComponent
        {
            public Vector3 Value;
        }
    }
}