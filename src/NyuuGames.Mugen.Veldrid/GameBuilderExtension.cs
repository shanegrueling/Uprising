namespace NyuuGames.Mugen.Veldrid
{
    using global::Veldrid;
    using global::Veldrid.Sdl2;
    using global::Veldrid.StartupUtilities;
    using Microsoft.Extensions.DependencyInjection;

    public static class GameBuilderExtension
    {
        public static GameBuilder UseVeldrid(this GameBuilder builder)
        {
            return builder.ConfigureServices(UseVeldrid);
        }

        private static void UseVeldrid(IServiceCollection collection)
        {
            var windowCI = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 1280,
                WindowHeight = 800,
                WindowInitialState = WindowState.BorderlessFullScreen,
                WindowTitle = "NyuuGames Uprising"
            };

            collection.AddSingleton(provider => VeldridStartup.CreateWindow(ref windowCI));
            collection.AddSingleton(
                provider => VeldridStartup.CreateGraphicsDevice(provider.GetRequiredService<Sdl2Window>())
            );
        }
    }
}