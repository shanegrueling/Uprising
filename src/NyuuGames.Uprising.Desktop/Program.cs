namespace NyuuGames.Uprising.Desktop
{
    using Mugen;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            GameBuilder.CreateDefaultBuilder().UseUprising().Build().Run();
        }
    }
}
