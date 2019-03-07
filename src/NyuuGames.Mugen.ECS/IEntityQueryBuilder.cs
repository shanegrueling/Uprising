namespace NyuuGames.Mugen.ECS
{
    public interface IEntityQueryBuilder
    {
        IEntityQueryBuilder Require<T>();
        IEntityQueryBuilder Optional<T>();
        IEntityQueryBuilder Any<T>();
        IEntityQueryBuilder Exclude<T>();
    }
}