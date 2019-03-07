namespace NyuuGames.Uprising.Game.Systems
{
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using Components;
    using Mugen.ECS;

    public sealed class CalculateLocalToWorldMatrix
    {
        private readonly EntityManager _entityManager;
        private readonly QueryResult _result;

        public CalculateLocalToWorldMatrix(EntityManager entityManager)
        {
            _entityManager = entityManager;

            _result = entityManager.Find(builder => builder.Any<Position>().Any<Scale>().Require<LocalToWorldMatrix>());
        }

        public void Update()
        {
            int GetOffset(ComponentType type, Blueprint blueprint)
            {
                var componentTypes = blueprint.BlueprintDefinition.ComponentTypes;

                var offset = Unsafe.SizeOf<Entity>();
                for (var i = 0; i < componentTypes.Length; ++i)
                {
                    ref var componentType = ref componentTypes[i];
                    if (componentType.Equals(type))
                    {
                        break;
                    }

                    offset += componentType.Size;
                }

                return offset;
            }

            var localToWorldMatrixes = _result.GetComponentArray<LocalToWorldMatrix>();
            var positions = _result.GetComponentArray<Position>();
            var scales = _result.GetComponentArray<Scale>();

            var matchedBlueprints = _result.MatchedBlueprints;
            foreach (var blueprint in matchedBlueprints)
            {
                var offsetPosition = GetOffset(ComponentType.GetComponentType(typeof(Position)), blueprint);
                var offsetScale = GetOffset(ComponentType.GetComponentType(typeof(Scale)), blueprint);
                var offsetMatrix = GetOffset(ComponentType.GetComponentType(typeof(LocalToWorldMatrix)), blueprint);

                foreach (ref var chunk in blueprint)
                {
                    for (var i = 0; i < chunk.EntityCount; ++i)
                    {
                        ref var position = ref positions.Get(ref chunk, i, offsetPosition);
                        ref var scale = ref scales.Get(ref chunk, i, offsetScale);

                        localToWorldMatrixes.Get(ref chunk, i, offsetMatrix) = new LocalToWorldMatrix
                        {
                            Value = Matrix4x4.CreateScale(scale.Value) * Matrix4x4.CreateTranslation(position.Value)
                        };
                    }
                }
            }
        }
    }
}