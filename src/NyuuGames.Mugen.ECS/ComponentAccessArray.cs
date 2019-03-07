namespace NyuuGames.Mugen.ECS
{
    using System.Runtime.CompilerServices;

    public readonly ref struct ComponentAccessArray<T> where T : unmanaged, IComponent
    {
        private readonly BlueprintChunkManager _chunkManager;
        private readonly ComponentType _componentType;

        internal ComponentAccessArray(BlueprintChunkManager chunkManager)
        {
            _chunkManager = chunkManager;
            _componentType = ComponentType.GetComponentType(typeof(T));
        }

        public ref T this[in BlueprintChunkReference idx]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return ref Unsafe.AsRef<T>(
                        _chunkManager.GetComponent(_componentType, idx)
                    );
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(in BlueprintChunkReference idx, int offset)
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(
                    _chunkManager.GetComponent(_componentType.Size, offset, idx.Chunk, idx.Index)
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(ref BlueprintChunk chunk, int idxInChunk, int offset)
        {
            unsafe
            {
                return ref Unsafe.AsRef<T>(
                    _chunkManager.GetComponent(_componentType.Size, offset, ref chunk, idxInChunk)
                );
            }
        }
    }
}