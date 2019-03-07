namespace NyuuGames.Mugen.ECS
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal sealed unsafe class BlueprintChunkManager
    {
        public ref BlueprintChunk CreateBlueprintChunk(ref BlueprintDefinition blueprintDefinition)
        {
            var componentTypes = blueprintDefinition.ComponentTypes;

            var sizeOfInstance = sizeof(Entity);
            for (var i = 0; i < componentTypes.Length; ++i)
            {
                sizeOfInstance += componentTypes[i].Size;
            }

            ref var chunk = ref Unsafe.AsRef<BlueprintChunk>(
                (void*) Marshal.AllocHGlobal(sizeof(BlueprintChunk) + sizeOfInstance * 1024)
            );

            chunk.Definition = (BlueprintDefinition*) Unsafe.AsPointer(ref blueprintDefinition);
            chunk.Capacity = 1024;
            chunk.EntityCount = 0;
            chunk.NextChunk = null;
            chunk.NextChunkWithSpace = blueprintDefinition.ChunkWithSpace;

            if (blueprintDefinition.LastChunk != null)
            {
                blueprintDefinition.LastChunk->NextChunk = (BlueprintChunk*) Unsafe.AsPointer(ref chunk);
            }

            blueprintDefinition.ChunkWithSpace =
                blueprintDefinition.LastChunk = (BlueprintChunk*) Unsafe.AsPointer(ref chunk);

            return ref chunk;
        }

        public BlueprintChunkReference AllocateEntityInChunk(ref BlueprintChunk chunk, in Entity entity)
        {
            var indexInChunk = chunk.EntityCount++;
            chunk.Definition->EntityCount++;

            if (chunk.Capacity == chunk.EntityCount)
            {
                chunk.Definition->ChunkWithSpace = chunk.NextChunkWithSpace;
            }

            Unsafe.Write(Unsafe.Add<Entity>(chunk.ChunkBody, indexInChunk), entity);

            return new BlueprintChunkReference((BlueprintChunk*) Unsafe.AsPointer(ref chunk), indexInChunk);
        }

        public void RemoveEntityFromChunk(ref BlueprintChunkReference reference, EntityDefinition[] entityDefinitions)
        {
            var bodyStart = reference.Chunk->ChunkBody;

            if (reference.Chunk->EntityCount > reference.Index + 1)
            {
                var sizeOfInstanceSoFar = Unsafe.SizeOf<Entity>();

                Unsafe.CopyBlock(
                    Unsafe.Add<Entity>(bodyStart, reference.Index),
                    Unsafe.Add<Entity>(bodyStart, reference.Chunk->EntityCount-1),
                    (uint)sizeOfInstanceSoFar
                );

                var componentTypes = reference.Chunk->Definition->ComponentTypes;
                for (var i = 0; i < componentTypes.Length; ++i)
                {
                    var size = componentTypes[i].Size;

                    Unsafe.CopyBlock(
                        Unsafe.Add<byte>(bodyStart, sizeOfInstanceSoFar * reference.Chunk->Capacity + size * reference.Index),
                        Unsafe.Add<byte>(bodyStart, (sizeOfInstanceSoFar + size) * reference.Chunk->Capacity - size),
                        (uint) size
                    );
                    sizeOfInstanceSoFar += size;
                }

                ref var ent = ref Unsafe.AsRef<Entity>(Unsafe.Add<Entity>(bodyStart, reference.Index));
                entityDefinitions[ent.Index].BlueprintChunkReference = new BlueprintChunkReference(reference.Chunk, reference.Index);
            }
            else
            {
                var sizeOfInstanceSoFar = Unsafe.SizeOf<Entity>();

                Unsafe.InitBlock(
                    Unsafe.Add<Entity>(bodyStart, reference.Index),
                    0,
                    (uint)sizeOfInstanceSoFar
                );

                var componentTypes = reference.Chunk->Definition->ComponentTypes;
                for (var i = 0; i < componentTypes.Length; ++i)
                {
                    var size = componentTypes[i].Size;

                    Unsafe.InitBlock(
                        Unsafe.Add<byte>(bodyStart, sizeOfInstanceSoFar * reference.Chunk->Capacity + size * reference.Index),
                        0,
                        (uint) size
                    );
                    sizeOfInstanceSoFar += size;
                }
            }

            reference.Chunk->EntityCount--;
            reference.Chunk->Definition->EntityCount--;
        }

        public void* GetComponent(in ComponentType type, in BlueprintChunkReference reference)
        {
            var componentTypes = reference.Chunk->Definition->ComponentTypes;

            var offset = sizeof(Entity);
            for (var i = 0; i < componentTypes.Length; ++i)
            {
                ref var componentType = ref componentTypes[i];
                if (componentType.Equals(type))
                {
                    break;
                }
                offset += componentType.Size;
            }

            return GetComponent(type.Size, offset, reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetComponent(int size, int offset, in BlueprintChunkReference reference)
        {
            return Unsafe.Add<byte>(reference.Chunk->ChunkBody, offset * reference.Chunk->Capacity + size * reference.Index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetComponent(int size, int offset, ref BlueprintChunk chunk, int indexInChunk)
        {
            return Unsafe.Add<byte>(chunk.ChunkBody, offset * chunk.Capacity + size * indexInChunk);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* GetComponent(int size, int offset, BlueprintChunk* chunk, int indexInChunk)
        {
            return Unsafe.Add<byte>(chunk->ChunkBody, offset * chunk->Capacity + size * indexInChunk);
        }

        public BlueprintChunkReference CopyFromChunkToAnotherChunk(in BlueprintChunkReference sourceBlueprintChunkReference, ref BlueprintChunk targetBlueprintChunk)
        {
            var sourceCapacity = sourceBlueprintChunkReference.Chunk->Capacity;
            var sourceChunkBody = sourceBlueprintChunkReference.Chunk->ChunkBody;
            var targetCapacity = targetBlueprintChunk.Capacity;
            var targetChunkBody = targetBlueprintChunk.ChunkBody;

            var targetBlueprintChunkReference = AllocateEntityInChunk(
                ref targetBlueprintChunk,
                Unsafe.AsRef<Entity>(Unsafe.Add<Entity>(sourceChunkBody, sourceBlueprintChunkReference.Index))
            );
            
            var sourceComponentTypes = sourceBlueprintChunkReference.Chunk->Definition->ComponentTypes;
            var sourceOffset = sizeof(Entity);
            var targetComponentTypes = targetBlueprintChunkReference.Chunk->Definition->ComponentTypes;
            var targetOffset = sizeof(Entity);

            var sourceIndex = 0;
            var targetIndex = 0;
            while (sourceIndex < sourceComponentTypes.Length)
            {
                ref var sourceComponentType = ref sourceComponentTypes[sourceIndex];
                var currentTargetIndex = targetIndex;
                var currentTargetOffset = targetOffset;
                var isFound = false;

                while (currentTargetIndex < targetComponentTypes.Length)
                {
                    ref var targetComponentType = ref targetComponentTypes[currentTargetIndex];

                    if (!sourceComponentType.Equals(targetComponentType))
                    {
                        ++currentTargetIndex;
                        currentTargetOffset += targetComponentType.Size;
                        continue;
                    }

                    Unsafe.CopyBlock(
                        Unsafe.Add<byte>(targetChunkBody, targetOffset * targetCapacity + targetComponentType.Size * targetBlueprintChunkReference.Index),
                        Unsafe.Add<byte>(sourceChunkBody, sourceOffset * sourceCapacity + targetComponentType.Size * sourceBlueprintChunkReference.Index),
                        (uint) targetComponentType.Size
                    );

                    isFound = true;
                    ++currentTargetIndex;
                    currentTargetOffset += targetComponentType.Size;
                    break;
                }

                if (isFound)
                {
                    targetIndex = currentTargetIndex;
                    targetOffset = currentTargetOffset;
                }

                ++sourceIndex;
                sourceOffset += sourceComponentType.Size;
            }

            return targetBlueprintChunkReference;
        }
    }
}