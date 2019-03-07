namespace NyuuGames.Mugen.ECS
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public sealed class EntityManager : IDisposable
    {
        private readonly BlueprintManager _blueprintManager;
        private readonly QueryManager _queryManager;
        private readonly BlueprintChunkManager _blueprintChunkManager = new BlueprintChunkManager();
        private EntityDefinition[] _entities;
        private readonly EntityIndexPool _entityIndexPool = new EntityIndexPool();

        public EntityManager(int startCapacity = 1024)
        {
            _blueprintManager = new BlueprintManager();
            _queryManager = new QueryManager(_blueprintManager);

            _entities = ArrayPool<EntityDefinition>.Shared.Rent(Math.Max(1, startCapacity));
        }

        public bool EntityExists(Entity entity)
        {
            if ((uint) entity.Index >= (uint) _entities.Length)
            {
                return false;
            }

            return _entities[entity.Index].Version == entity.Version;
        }

        public Blueprint GetBlueprint(params ComponentType[] componentTypes)
        {
            Array.Sort(componentTypes, (ct1, ct2) => ct1.Hash - ct2.Hash);
            return _blueprintManager.GetBlueprint(componentTypes);
        }

        public Entity CreateEntity(params ComponentType[] componentTypes)
        {
            Array.Sort(componentTypes, (ct1, ct2) => ct1.Hash - ct2.Hash);
            return CreateEntity(_blueprintManager.GetBlueprint(componentTypes));
        }

        public Entity CreateEntity(in Blueprint blueprint)
        {
            var index = _entityIndexPool.GetNext();

            if (index >= _entities.Length)
            {
                Resize(_entities.Length * 2);
            }

            ref var entityDefinition = ref _entities[index];
            unsafe
            {
                entityDefinition.BlueprintDefinition = blueprint._definition;
                if (entityDefinition.Version == 0)
                {
                    entityDefinition.Version = 1;
                }

                var ent = new Entity(index, entityDefinition.Version);
                entityDefinition.BlueprintChunkReference = _blueprintChunkManager.AllocateEntityInChunk(
                    ref _blueprintManager.GetChunkWithSpace(blueprint),
                    ent
                );

                return ent;
            }
        }

        private void Resize(int targetCapacity)
        {
            var newArray = ArrayPool<EntityDefinition>.Shared.Rent(targetCapacity);
            _entities.CopyTo(newArray, 0);

            ArrayPool<EntityDefinition>.Shared.Return(_entities);

            _entities = newArray;
        }
        
        public void DeleteEntity(in Entity entity)
        {
            ref var definition = ref _entities[entity.Index];

            ++definition.Version;
            unsafe
            {
                _blueprintChunkManager.RemoveEntityFromChunk(ref definition.BlueprintChunkReference, _entities);

                definition.BlueprintDefinition = null;
            }

            _entityIndexPool.Return(entity.Index);
        }

        public ref T GetComponent<T>(in Entity entity) where T : unmanaged, IComponent
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(GetComponent(typeof(T), in entity));
            }
        }

        private unsafe void* GetComponent(in ComponentType type, in Entity entity)
        {
            ref var definition = ref _entities[entity.Index];

            return _blueprintChunkManager.GetComponent(type, in definition.BlueprintChunkReference);
        }

        public void AddComponent<T>(in Entity entity, ref T component) where T : unmanaged, IComponent
        {
            void AddToComponentType(
                ref Span<ComponentType> newComponentTypes,
                ReadOnlySpan<ComponentType> currentComponentTypes,
                ComponentType ct)
            {
                if (currentComponentTypes.Length == 0)
                {
                    newComponentTypes[0] = ct;
                    return;
                }

                if (currentComponentTypes[0].Hash > ct.Hash)
                {
                    newComponentTypes[0] = ct;
                    currentComponentTypes.CopyTo(newComponentTypes.Slice(1));
                    return;
                }

                if (currentComponentTypes[currentComponentTypes.Length - 1].Hash < ct.Hash)
                {
                    currentComponentTypes.CopyTo(newComponentTypes);
                    newComponentTypes[newComponentTypes.Length - 1] = ct;
                    return;
                }

                for (int i = 0, j = 0; i < currentComponentTypes.Length; ++i, ++j)
                {
                    var oldCT = currentComponentTypes[j];

                    if (oldCT.Hash > ct.Hash)
                    {
                        newComponentTypes[j++] = ct;
                    }

                    newComponentTypes[j] = oldCT;
                }
            }

            ref var definition = ref _entities[entity.Index];

            var componentType = ComponentType.GetComponentType(typeof(T));

            unsafe
            {
                var currentComponentTypes = definition.BlueprintDefinition->ComponentTypes;
                Span<ComponentType> newComponentTypes = stackalloc ComponentType
                    [definition.BlueprintDefinition->ComponentTypesLength+1];

                AddToComponentType(ref newComponentTypes, currentComponentTypes, componentType);

                var newBlueprint = _blueprintManager.GetBlueprint(newComponentTypes);
                var oldReference = definition.BlueprintChunkReference;
                definition.BlueprintChunkReference = _blueprintChunkManager.CopyFromChunkToAnotherChunk(
                    in oldReference,
                    ref _blueprintManager.GetChunkWithSpace(newBlueprint)
                );

                _blueprintChunkManager.RemoveEntityFromChunk(ref oldReference, _entities);
            }

            ref var comp = ref GetComponent<T>(entity);
            comp = component;
        }

        public void Dispose()
        {
            ArrayPool<EntityDefinition>.Shared.Return(_entities);
            _blueprintManager.Dispose();
        }

        public QueryResult Find(Action<IEntityQueryBuilder> configureQuery)
        {
            var builder = new EntityQueryBuilder();
            configureQuery?.Invoke(builder);

            return _queryManager.AddQuery(builder.Build());
        }
    }

    internal class QueryManager
    {
        private readonly List<(EntityQuery query, QueryResult result)> _queryResultSet = new List<(EntityQuery query, QueryResult result)>();
        private readonly BlueprintManager _blueprintManager;

        public QueryManager(BlueprintManager blueprintManager)
        {
            _blueprintManager = blueprintManager;
        }

        public QueryResult AddQuery(EntityQuery query)
        {
            var result = new QueryResult();

            _queryResultSet.Add((query, result));

            foreach (Blueprint blueprint in _blueprintManager)
            {
                if(query.Check(blueprint.BlueprintDefinition.ComponentTypes))
                {
                    result.AddBlueprint(blueprint);
                }
            }

            return result;
        }
    }
}