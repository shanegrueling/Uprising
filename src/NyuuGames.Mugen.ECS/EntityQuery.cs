namespace NyuuGames.Mugen.ECS
{
    using System;

    public class EntityQuery
    {
        private readonly ComponentType[] _required;
        private readonly ComponentType[] _any;
        private readonly ComponentType[] _optional;
        private readonly ComponentType[] _exclude;

        public EntityQuery(ComponentType[] required, ComponentType[] optional, ComponentType[] any, ComponentType[] exclude)
        {
            _required = required;
            _optional = optional;
            _any = any;
            _exclude = exclude;
        }

        public bool Check(ReadOnlySpan<ComponentType> componentTypes)
        {
            foreach (var requiredComponentType in _required)
            {
                var isFound = false;
                foreach (var componentType in componentTypes)
                {
                    if (componentType.Equals(requiredComponentType))
                    {
                        isFound = true;
                        break;
                    }
                }

                if (!isFound) return false;
            }

            foreach (var excludedComponentType in _exclude)
            {
                var isFound = false;
                foreach (var componentType in componentTypes)
                {
                    if (componentType.Equals(excludedComponentType))
                    {
                        isFound = true;
                        break;
                    }
                }

                if (isFound) return false;
            }

            if (_any.Length == 0) return true;

            var anyFound = 0;
            foreach (var anyComponentType in _any)
            {
                foreach (var componentType in componentTypes)
                {
                    if (componentType.Equals(anyComponentType))
                    {
                        ++anyFound;
                        break;
                    }
                }
            }

            return anyFound != 0;
        }
    }
}