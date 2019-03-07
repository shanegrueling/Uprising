namespace NyuuGames.Mugen.ECS
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BlueprintDefinition
    {
        internal BlueprintDefinition* PreviousBlueprintDefinition;
        
        internal BlueprintChunk* FirstChunk;
        internal BlueprintChunk* LastChunk;
        internal BlueprintChunk* ChunkWithSpace;
        public int EntityCount;

        public int ComponentTypesLength;

        public Span<ComponentType> ComponentTypes => new Span<ComponentType>(Unsafe.Add<int>(Unsafe.AsPointer(ref ComponentTypesLength), 1), ComponentTypesLength);
    }
}