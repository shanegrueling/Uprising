namespace NyuuGames.Uprising.Game.Systems
{
    using System;
    using System.Numerics;
    using Components;
    using Mugen.ECS;

    public sealed class SetPlayerInput : IComponent
    {
        private readonly EntityManager _entityManager;
        private readonly QueryResult _result;

        public SetPlayerInput(EntityManager entityManager)
        {
            _entityManager = entityManager;

            _result = _entityManager.Find(builder => builder.Require<PlayerInput>());
        }

        public void Update()
        {
            var playerInputs = _result.GetComponentArray<PlayerInput>();

            foreach (ref readonly var idx in _result)
            {
                ref var input = ref playerInputs[idx];

                input.TargetTile = new Vector2(
                    MathF.Round(Input.MousePosition.X / 16) * 16,
                    MathF.Round((800 - Input.MousePosition.Y) / 16) * 16
                );
            }
        }
    }
}