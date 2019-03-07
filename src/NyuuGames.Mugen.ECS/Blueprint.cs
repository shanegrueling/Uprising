namespace NyuuGames.Mugen.ECS
{
    using System.Runtime.CompilerServices;

    public readonly struct Blueprint
    {
        internal readonly unsafe BlueprintDefinition* _definition;

        public ref BlueprintDefinition BlueprintDefinition
        {
            get
            {
                unsafe {
                    return ref Unsafe.AsRef<BlueprintDefinition>(_definition);
                }
            }
        }

        internal unsafe Blueprint(BlueprintDefinition* definition)
        {
            _definition = definition;
        }

        public unsafe BlueprintChunkEnumerator GetEnumerator()
        {
            return new BlueprintChunkEnumerator(_definition);
        }
    }
}