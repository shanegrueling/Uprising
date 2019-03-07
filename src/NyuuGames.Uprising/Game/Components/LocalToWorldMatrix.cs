namespace NyuuGames.Uprising.Game.Components
{
    using System.Numerics;
    using Mugen.ECS;

    public struct LocalToWorldMatrix : IComponent
    {
        public Matrix4x4 Value;
    }
}