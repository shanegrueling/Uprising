namespace NyuuGames.Mugen.ECS
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal unsafe class BlueprintManager : IDisposable
    {
        private readonly BlueprintChunkManager _blueprintChunkManager = new BlueprintChunkManager();
        private BlueprintDefinition* _lastBlueprintDefinitionCreated = null;

        /// <summary>
        ///     Returns a Blueprint. Expects the componentTypes to be sorted from lowest hash value to highest.
        /// </summary>
        /// <param name="componentTypes"></param>
        /// <returns></returns>
        public Blueprint GetBlueprint(in ReadOnlySpan<ComponentType> componentTypes)
        {
            var (blueprint, wasFound) = FindExistingBlueprint(componentTypes);

            return wasFound ? blueprint : CreateBlueprint(componentTypes);
        }

        private Blueprint CreateBlueprint(in ReadOnlySpan<ComponentType> componentTypes)
        {
            var blueprintDefintion = (BlueprintDefinition*) Marshal.AllocHGlobal(
                sizeof(BlueprintDefinition) + sizeof(ComponentType) * componentTypes.Length
            );

            blueprintDefintion->ComponentTypesLength = componentTypes.Length;
            blueprintDefintion->PreviousBlueprintDefinition = _lastBlueprintDefinitionCreated;
            blueprintDefintion->EntityCount = 0;
            blueprintDefintion->LastChunk = null;
            blueprintDefintion->ChunkWithSpace = null;
            componentTypes.CopyTo(blueprintDefintion->ComponentTypes);

            blueprintDefintion->FirstChunk = (BlueprintChunk*) Unsafe.AsPointer(
                ref _blueprintChunkManager.CreateBlueprintChunk(
                    ref Unsafe.AsRef<BlueprintDefinition>(blueprintDefintion)
                )
            );

            _lastBlueprintDefinitionCreated = blueprintDefintion;

            return new Blueprint(blueprintDefintion);
        }

        private (Blueprint blueprint, bool wasFound) FindExistingBlueprint(
            in ReadOnlySpan<ComponentType> componentTypes)
        {
            var blueprintDefinition = _lastBlueprintDefinitionCreated;
            var length = componentTypes.Length;
            while (blueprintDefinition != null)
            {
                if (length != blueprintDefinition->ComponentTypesLength)
                {
                    blueprintDefinition = blueprintDefinition->PreviousBlueprintDefinition;
                    continue;
                }

                var currentBlueprintComponentTypes = blueprintDefinition->ComponentTypes;
                var isSame = true;
                for (var i = 0; i < length; ++i)
                {
                    if (componentTypes[i].Equals(currentBlueprintComponentTypes[i]))
                    {
                        continue;
                    }

                    isSame = false;
                }

                if (isSame)
                {
                    return (new Blueprint(blueprintDefinition), true);
                }

                blueprintDefinition = blueprintDefinition->PreviousBlueprintDefinition;
            }

            return (new Blueprint(), false);
        }

        public ref BlueprintChunk GetChunkWithSpace(in Blueprint blueprint)
        {
            ref var definition = ref blueprint.BlueprintDefinition;

            if (definition.ChunkWithSpace == null)
            {
                definition.ChunkWithSpace = (BlueprintChunk*) Unsafe.AsPointer(ref _blueprintChunkManager.CreateBlueprintChunk(ref definition));
            }

            return ref Unsafe.AsRef<BlueprintChunk>(definition.ChunkWithSpace);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_lastBlueprintDefinitionCreated);
        }

        public void Dispose()
        {
            foreach(var blueprint in this)
            {
                var blueprintChunkEnumerator = new BlueprintChunkEnumerator(blueprint);
                BlueprintChunk* blueprintChunk = null;
                while(blueprintChunkEnumerator.MoveNext())
                {
                    if(blueprintChunk != null)
                    {
                        Marshal.FreeHGlobal(new IntPtr(blueprintChunk));
                    }
                    blueprintChunk = blueprintChunkEnumerator._currentChunk;
                }

                Marshal.FreeHGlobal((IntPtr)blueprint._definition);
            }
        }

        public struct Enumerator
        {
            public Blueprint Current { get; private set; }
            private bool _isStarted;

            internal Enumerator(BlueprintDefinition* startBlueprintDefinition)
            {
                _isStarted = false;
                Current = new Blueprint(startBlueprintDefinition);
            }

            public bool MoveNext()
            {
                if(Current._definition == null) return false;
                if(!_isStarted) 
                {
                    _isStarted = true;
                    return true;
                }

                if(Current._definition->PreviousBlueprintDefinition == null) return false;

                Current = new Blueprint(Current._definition->PreviousBlueprintDefinition);
                return true;
            }
        }
    }
}