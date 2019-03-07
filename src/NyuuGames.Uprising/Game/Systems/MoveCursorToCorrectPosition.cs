namespace NyuuGames.Uprising.Game.Systems
{
    using System.Numerics;
    using Components;
    using Mugen.ECS;

    public sealed class MoveCursorToCorrectPosition
    {
        private readonly EntityManager _entityManager;
        private readonly QueryResult _result;

        public MoveCursorToCorrectPosition(EntityManager entityManager)
        {
            _entityManager = entityManager;

            _result = _entityManager.Find(builder => builder.Require<Position>().Require<PlayerInput>());
        }

        public void Update()
        {
            var positions = _result.GetComponentArray<Position>();
            var playerInputs = _result.GetComponentArray<PlayerInput>();

            (int Width, int Height) window = (1280, 800);

            var projection = Matrix4x4.CreateOrthographicOffCenter(0, window.Width, 0, window.Height, 0, 1);
            var view = Matrix4x4.CreateTranslation(new Vector3(window.Width / 2f - 25 * 16f, window.Height / 2f - 25 * 16f, 0));
            var invert = view * projection;
            Matrix4x4.Invert(invert, out invert);


            foreach(ref readonly var idx in _result)
            {
                ref var position = ref positions[idx];
                ref var input = ref playerInputs[idx];

                var ndc = new Vector3(input.TargetTile.X * 2.0f / window.Width - 1, input.TargetTile.Y * 2.0f / window.Height - 1, 0);

                position.Value = Vector3.Transform(ndc, invert);
            }
        }
    }
}