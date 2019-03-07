namespace NyuuGames.Mugen
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class GameBuilder
    {
        private Action<IServiceCollection> _configureServices;

        private GameBuilder()
        {

        }

        public GameBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            _configureServices += configureDelegate;
            return this;
        }

        public IGame Build()
        {
            var collection = new ServiceCollection();

            _configureServices?.Invoke(collection);
            var provider = collection.BuildServiceProvider();

            return provider.GetRequiredService<IGame>();
        }

        public static GameBuilder CreateDefaultBuilder()
        {
            return new GameBuilder();
        }
    }
}
