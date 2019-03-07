namespace NyuuGames.Mugen.ECS
{
    internal unsafe struct EntityDefinition
    {
        public BlueprintDefinition* BlueprintDefinition;
        public int Version;
        public BlueprintChunkReference BlueprintChunkReference;
    }
}