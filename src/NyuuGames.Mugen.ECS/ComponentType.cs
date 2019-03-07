namespace NyuuGames.Mugen.ECS
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public readonly struct ComponentType : IEquatable<ComponentType>
    {
        public readonly int Hash;
        public readonly int Size;

        internal ComponentType(int hash, int size)
        {
            Hash = hash;
            Size = size;
        }

        public static implicit operator ComponentType(Type type)
        {
            return GetComponentType(type);
        }

        public static ComponentType GetComponentType(Type type)
        {
            return new ComponentType(TypeManager.GetIndex(type), Marshal.SizeOf(type));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ComponentType other)
        {
            return Hash == other.Hash;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash;
        }
    }
}