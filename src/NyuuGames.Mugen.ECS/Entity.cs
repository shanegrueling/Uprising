using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NyuuGames.Mugen.ECS.Test")]

namespace NyuuGames.Mugen.ECS
{
    using System;

    public readonly struct Entity : IEquatable<Entity>
    {
        public static readonly Entity Invalid = new Entity(0, 0);

        internal readonly int Index;
        internal readonly int Version;

        public Entity(int index, int version)
        {
            Index = index;
            Version = version;
        }

        public bool Equals(Entity other)
        {
            return Index == other.Index && Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            return obj is Entity other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Index, Version).GetHashCode();
        }
    }

    /*public class T
    {
        public void TT()
        {
            var result = new QueryResult();

            var positions = result.GetComponentArray<Position>();
            var velocities = result.GetReadOnlyComponentArray<Velocity>();
            foreach (ref readonly var idx in result)
            {
                ref var position = ref positions[idx];
                ref readonly var velocity = ref velocities[idx];

                TTT(ref positions[idx], in velocities[idx]);
            }
        }
        
        public void TTT(ref Position pos, in Velocity vel)
        {
            
        }

        public readonly struct Position : IComponent
        {

        }

        public readonly struct Velocity
        {

        }
    }*/
}