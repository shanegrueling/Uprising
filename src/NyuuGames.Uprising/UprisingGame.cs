namespace NyuuGames.Uprising
{
    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Game.Components;
    using Game.Systems;
    using Microsoft.Extensions.DependencyInjection;
    using Mugen;
    using Mugen.ECS;
    using Mugen.Veldrid;
    using Veldrid;
    using Veldrid.Sdl2;

    public static class GameBuilderExtension
    {
        public static GameBuilder UseUprising(this GameBuilder builder)
        {
            return builder.UseVeldrid()
                .ConfigureServices(collection => collection.AddSingleton<IGame, UprisingGame>());
        }
    }

    public class UprisingGame : IGame
    {
        private readonly Sdl2Window _window;
        private readonly GraphicsDevice _graphicsDevice;

        public UprisingGame(Sdl2Window window, GraphicsDevice graphicsDevice)
        {
            _window = window;
            _graphicsDevice = graphicsDevice;
        }

        public void Run()
        {
            var entityManager = new EntityManager();

            CreateMap(entityManager, new Vector2(50, 50), new Vector2(16, 16));

            CreateCursor(entityManager, new Vector2(25, 25), new Vector2(16, 16));

            var calculateLocalToWorldMatrix = new CalculateLocalToWorldMatrix(entityManager);
            var drawRectangle = new DrawSprites(entityManager, _graphicsDevice, _window);
            var setPlayerInput = new SetPlayerInput(entityManager);
            var moveCursour = new MoveCursorToCorrectPosition(entityManager);

            long previousFrameTicks = 0;
            
            var sw = new Stopwatch();
            sw.Start();
            var frames = 0;
            while (_window.Exists)
            {
                ++frames;
                var currentFrameTicks = sw.ElapsedTicks;
                var deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double)Stopwatch.Frequency;

                previousFrameTicks = currentFrameTicks;

                Input.UpdateFrameInput(_window.PumpEvents());
                setPlayerInput.Update();
                moveCursour.Update();
                calculateLocalToWorldMatrix.Update();
                drawRectangle.Update();
            }
            sw.Stop();
            drawRectangle.Dispose();

            Console.WriteLine(frames);
            Console.WriteLine(sw.ElapsedMilliseconds/1000f);
            Console.WriteLine(frames/(sw.ElapsedMilliseconds/1000f));
        }

        private static void CreateCursor(EntityManager entityManager, Vector2 position, Vector2 tileSize)
        {
            var ent = entityManager.CreateEntity(typeof(Position), typeof(LocalToWorldMatrix), typeof(Scale), typeof(SpriteRenderer), typeof(CursourLayer), typeof(PlayerInput));
            entityManager.GetComponent<Position>(ent) = new Position { Value = new Vector3(tileSize.X * position.X, tileSize.Y * position.Y, 0) };
            entityManager.GetComponent<Scale>(ent) = new Scale { Value = new Vector3(tileSize, 1) };
            entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(3, 42) };
        }

        private static void CreateMap(EntityManager entityManager, Vector2 mapSize, Vector2 tileSize)
        {
            for(var x = 0; x < mapSize.X; ++x)
            {
                for(var y = 0; y < mapSize.Y; ++y)
                {
                    CreateLandLayer(entityManager, mapSize, tileSize, x, y);
                    CreatePropLayer(entityManager, mapSize, tileSize, x, y);
                }
            }
        }

        private static void CreatePropLayer(EntityManager entityManager, Vector2 mapSize, Vector2 tileSize, int x, int y)
        {
            if(x == 0 || x == mapSize.X -1 || y == 0 || y == mapSize.Y - 1) return;

            if(x % 2 == 0 && y % 2 == 1) return;

            var ent = entityManager.CreateEntity(typeof(Position), typeof(LocalToWorldMatrix), typeof(Scale), typeof(SpriteRenderer), typeof(PropLayer));
            entityManager.GetComponent<Position>(ent) = new Position { Value = new Vector3(tileSize.X * x, tileSize.Y * y, 0) };
            entityManager.GetComponent<Scale>(ent) = new Scale { Value = new Vector3(tileSize, 1) };
            entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(6, 6) };
        }

        private static void CreateLandLayer(EntityManager entityManager, Vector2 mapSize, Vector2 tileSize, int x, int y)
        {
            var ent = entityManager.CreateEntity(typeof(Position), typeof(LocalToWorldMatrix), typeof(Scale), typeof(SpriteRenderer), typeof(LandLayer));
            entityManager.GetComponent<Position>(ent) = new Position { Value = new Vector3(tileSize.X * x, tileSize.Y * y, 0) };
            entityManager.GetComponent<Scale>(ent) = new Scale { Value = new Vector3(tileSize, 1) };

            if (x == 0 && y == 0)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(0, 30) };
            }
            else if (x == 0 && y == mapSize.Y - 1)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(0, 28) };
            }
            else if (x == mapSize.X - 1 && y == 0)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(2, 30) };
            }
            else if (x == mapSize.X - 1 && y == mapSize.Y - 1)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(2, 28) };
            }
            else if (x == 0)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(0, 29) };
            }
            else if (y == 0)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(1, 30) };
            }
            else if (x == mapSize.X - 1)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(2, 29) };
            }
            else if (y == mapSize.Y - 1)
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(1, 27) };
            }
            else
            {
                entityManager.GetComponent<SpriteRenderer>(ent) = new SpriteRenderer { Value = new Vector2(0, 0) };
            }
        }
    }
}
