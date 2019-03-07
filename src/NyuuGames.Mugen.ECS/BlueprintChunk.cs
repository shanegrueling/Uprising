namespace NyuuGames.Mugen.ECS
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BlueprintChunk
    {
        public BlueprintDefinition* Definition;

        public BlueprintChunk* NextChunk;
        public BlueprintChunk* NextChunkWithSpace;

        public int Capacity;
        public int EntityCount;
        
        public void* ChunkBody
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Unsafe.Add<BlueprintChunk>(Unsafe.AsPointer(ref this), 1);
        }

        public Span<T> GetSpan<T>(int offset)
        {
            return new Span<T>(Unsafe.Add<byte>(ChunkBody, offset * Capacity), EntityCount);
        }
    }
}