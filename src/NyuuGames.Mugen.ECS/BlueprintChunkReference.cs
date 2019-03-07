namespace NyuuGames.Mugen.ECS
{
    public readonly unsafe struct BlueprintChunkReference
    {
        internal readonly BlueprintChunk* Chunk;
        internal readonly int Index;

        public BlueprintChunkReference(BlueprintChunk* chunk, int index)
        {
            Chunk = chunk;
            Index = index;
        }
    }
}