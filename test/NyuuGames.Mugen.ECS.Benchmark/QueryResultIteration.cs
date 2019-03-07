namespace NyuuGames.Mugen.ECS.Benchmark
{
    using System;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using BenchmarkDotNet.Attributes;

    public class QueryResultIteration
    {
        private QueryResult _result;
        private EntityManager _em;
        private Blueprint _blueprint;

        [Params(100000)]
        public int EntityAmountBlueprintA { get; set; }

        //[Params(1000, 10000, 100000)]
        public int EntityAmountBlueprintB => EntityAmountBlueprintA;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _em = new EntityManager();
            _blueprint = _em.GetBlueprint(typeof(Position), typeof(Scale), typeof(LocalToWorldMatrix));
            var blueprintB = _em.GetBlueprint(typeof(Position), typeof(Scale), typeof(LocalToWorldMatrix), typeof(Unused));

            _result = _em.Find(builder => builder.Any<Position>().Any<Scale>().Require<LocalToWorldMatrix>());

            var r = new Random();

            for(var i = 0; i < EntityAmountBlueprintA;++i)
            {
                var e = _em.CreateEntity(_blueprint);

                _em.GetComponent<Position>(e) = new Position { Value = new Vector3(100 * (float)r.NextDouble(), 100 * (float)r.NextDouble(), 0)};
                _em.GetComponent<Scale>(e) = new Scale { Value = Vector3.One };
            }

            for(var i = 0; i < EntityAmountBlueprintB;++i)
            {
                var e = _em.CreateEntity(blueprintB);

                _em.GetComponent<Position>(e) = new Position { Value = new Vector3(100 * (float)r.NextDouble(), 100 * (float)r.NextDouble(), 0)};
                _em.GetComponent<Scale>(e) = new Scale { Value = Vector3.One };
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _em.Dispose();
            _result = null;
            _em = null;
            _blueprint = default;
        }

        [Benchmark(Baseline =true)]
        public void UpdateCurrentWay()
        {
            var localToWorldMatrixes = _result.GetComponentArray<LocalToWorldMatrix>();
            var positions = _result.GetComponentArray<Position>();
            var scales  = _result.GetComponentArray<Scale>();

            foreach(ref readonly var idx in _result)
            {
                ref var position = ref positions[idx];
                ref var scale = ref scales[idx];

                localToWorldMatrixes[idx] = new LocalToWorldMatrix { Value = Matrix4x4.CreateScale(scale.Value) * Matrix4x4.CreateTranslation(position.Value)};
            }
        }

        [Benchmark]
        public void UpdateAlternative2Way()
        {
            int GetOffset<T>(Blueprint blueprint)
            {
                var componentTypes = blueprint.BlueprintDefinition.ComponentTypes;
                var type = ComponentType.GetComponentType(typeof(T));

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
            var scales  = _result.GetComponentArray<Scale>();

            var matchedBlueprints = _result.MatchedBlueprints;
            foreach(var blueprint in matchedBlueprints)
            {
                var offsetPosition = GetOffset<Position>(blueprint);
                var offsetScale = GetOffset<Scale>(blueprint);
                var offsetMatrix = GetOffset<LocalToWorldMatrix>(blueprint);

                foreach(ref var chunk in blueprint)
                {
                    for(var i = 0 ; i < chunk.EntityCount; ++i)
                    {
                        ref var position = ref positions.Get(ref chunk, i, offsetPosition);
                        ref var scale = ref scales.Get(ref chunk, i, offsetScale);

                        localToWorldMatrixes.Get(ref chunk, i, offsetMatrix) = new LocalToWorldMatrix { Value = Matrix4x4.CreateScale(scale.Value) * Matrix4x4.CreateTranslation(position.Value)};
                    }
                }
            }
        }

        [Benchmark]
        public void UpdateAlternative2_1Way()
        {
            int GetOffset<T>(Blueprint blueprint)
            {
                var componentTypes = blueprint.BlueprintDefinition.ComponentTypes;
                var type = ComponentType.GetComponentType(typeof(T));

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
            var scales  = _result.GetComponentArray<Scale>();

            var matchedBlueprints = _result.MatchedBlueprints;
            foreach(var blueprint in matchedBlueprints)
            {
                var offsetPosition = GetOffset<Position>(blueprint);
                var offsetScale = GetOffset<Scale>(blueprint);
                var offsetMatrix = GetOffset<LocalToWorldMatrix>(blueprint);

                foreach(ref var chunk in blueprint)
                {
                    var positionsC = chunk.GetSpan<Position>(offsetPosition);
                    var scalesC = chunk.GetSpan<Scale>(offsetScale);
                    var localToWorldMatrixesC = chunk.GetSpan<LocalToWorldMatrix>(offsetMatrix);

                    for(var i = 0 ; i < chunk.EntityCount; ++i)
                    {
                        ref var position = ref positionsC[i];
                        ref var scale = ref scalesC[i];

                        localToWorldMatrixesC[i] = new LocalToWorldMatrix { Value = Matrix4x4.CreateScale(scale.Value) * Matrix4x4.CreateTranslation(position.Value)};
                    }
                }
            }
        }

        [Benchmark]
        public void UpdateAlternativeWay()
        {
            int GetOffset(ComponentType type)
            {
                var componentTypes = _blueprint.BlueprintDefinition.ComponentTypes;

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
            var scales  = _result.GetComponentArray<Scale>();
            
            var offsetPosition = GetOffset(ComponentType.GetComponentType(typeof(Position)));
            var offsetScale = GetOffset(ComponentType.GetComponentType(typeof(Scale)));
            var offsetMatrix = GetOffset(ComponentType.GetComponentType(typeof(LocalToWorldMatrix)));

            foreach(ref readonly var idx in _result)
            {
                ref var position = ref positions.Get(in idx, offsetPosition);
                ref var scale = ref scales.Get(in idx, offsetScale);

                localToWorldMatrixes.Get(in idx, offsetMatrix) = new LocalToWorldMatrix { Value = Matrix4x4.CreateScale(scale.Value) * Matrix4x4.CreateTranslation(position.Value)};
            }
        }

        private struct Position : IComponent
        {
            public Vector3 Value;
        }

        private struct Scale : IComponent
        {
            public Vector3 Value;
        }

        private struct LocalToWorldMatrix : IComponent
        {
            public Matrix4x4 Value;
        }

        private struct Unused : IComponent
        {
            public Vector3 Value;
        }
    }
}