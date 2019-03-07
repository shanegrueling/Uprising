namespace NyuuGames.Mugen.ECS
{
    public class EntityQueryBuilder : IEntityQueryBuilder
    {
        private delegate void ModifyArray(ComponentType[] array, ref int idx);

        private ModifyArray? _requiredAction;
        private int _requiredCount;
        private ModifyArray? _optionalAction;
        private int _optionalCount;
        private ModifyArray? _anyAction;
        private int _anyCount;
        private ModifyArray? _excludedAction;
        private int _excludeCount;

        private ModifyArray GetModifyArray<T>() => (ComponentType[] array, ref int idx) =>
            array[idx++] = ComponentType.GetComponentType(typeof(T));

        public IEntityQueryBuilder Require<T>()
        {
            _requiredAction += GetModifyArray<T>();
            ++_requiredCount;
            return this;
        }

        public IEntityQueryBuilder Optional<T>()
        {
            _optionalAction += GetModifyArray<T>();
            ++_optionalCount;
            return this;
        }

        public IEntityQueryBuilder Any<T>()
        {
            _anyAction += GetModifyArray<T>();
            ++_anyCount;
            return this;
        }

        public IEntityQueryBuilder Exclude<T>()
        {
            _excludedAction += GetModifyArray<T>();
            ++_excludeCount;
            return this;
        }

        public EntityQuery Build()
        {
            var required = GetArray(_requiredAction, _requiredCount);
            var optional = GetArray(_optionalAction, _optionalCount);
            var any = GetArray(_anyAction, _anyCount);
            var exclude = GetArray(_excludedAction, _excludeCount);

            return new EntityQuery(required, optional, any, exclude);
        }

        private static ComponentType[] GetArray(ModifyArray? modifyArrayAction, int count)
        {
            var r = new ComponentType[count];
            var idx = 0;
            modifyArrayAction?.Invoke(r, ref idx);
            return r;
        }
    }
}