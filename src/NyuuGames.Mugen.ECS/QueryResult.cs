namespace NyuuGames.Mugen.ECS
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public sealed class QueryResult
    {
        private readonly List<Blueprint> _matchedBlueprints = new List<Blueprint>();

        public IReadOnlyList<Blueprint> MatchedBlueprints => _matchedBlueprints;

        public void AddBlueprint(in Blueprint blueprint)
        {
            _matchedBlueprints.Add(blueprint);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_matchedBlueprints.GetEnumerator());
        }

        public ComponentAccessArray<T> GetComponentArray<T>() where T : unmanaged, IComponent
        {
            return new ComponentAccessArray<T>(new BlueprintChunkManager());
        }

        public ref struct Enumerator
        {
            private List<Blueprint>.Enumerator _blueprintEnumerator;
            private BlueprintChunkEnumerator _currentChunkEnumerator;
            private int _currentIndexInChunk;
            private BlueprintChunkReference _current;
            private bool _isStarted;

            internal Enumerator(List<Blueprint>.Enumerator enumerator)
            {
                _blueprintEnumerator = enumerator;
                _current = default;
                _isStarted = false;
                _currentChunkEnumerator = default;
                _currentIndexInChunk = -1;
            }

            private unsafe bool MoveBlueprint()
            {
                int entityCount;
                do
                {
                    if (!_blueprintEnumerator.MoveNext())
                    {
                        return false;
                    }

                    entityCount = _blueprintEnumerator.Current._definition->EntityCount;
                } while(entityCount == 0);
                
                _currentChunkEnumerator = new BlueprintChunkEnumerator(_blueprintEnumerator.Current._definition);
                return true;
            }

            private unsafe bool MoveChunk()
            {
                _currentIndexInChunk = -1;
                do
                {
                    if(!_currentChunkEnumerator.MoveNext()) return false;
                } while(_currentChunkEnumerator._currentChunk->EntityCount == 0);

                return true;
            }

            private unsafe bool MoveIndex()
            {
                return ++_currentIndexInChunk < _currentChunkEnumerator._currentChunk->EntityCount;
            }
            
            public bool MoveNext()
            {
                if(!_isStarted)
                {
                    if(!MoveBlueprint()) return false;
                    MoveChunk();

                    _isStarted = true;
                }

                if(!MoveIndex())
                {
                    if(!MoveChunk())
                    {
                        if(!MoveBlueprint()) return false;

                        MoveChunk();
                    } 
                    MoveIndex();
                }

                unsafe
                {
                    _current = new BlueprintChunkReference(_currentChunkEnumerator._currentChunk, _currentIndexInChunk);
                }

                return true;
            }

            public unsafe ref readonly BlueprintChunkReference Current => ref Unsafe.AsRef<BlueprintChunkReference>(Unsafe.AsPointer(ref _current));
        }
    }

    public unsafe ref struct BlueprintChunkEnumerator
    {
        private readonly BlueprintDefinition* _definition;
        internal BlueprintChunk* _currentChunk;
        public bool IsValid;

        internal BlueprintChunkEnumerator(BlueprintDefinition* definition)
        {
            _definition = definition;
            IsValid = false;
            _currentChunk = null;
        }

        internal BlueprintChunkEnumerator(ref BlueprintDefinition definition)
        {
            _definition = (BlueprintDefinition*)Unsafe.AsPointer(ref definition);
            IsValid = false;
            _currentChunk = null;
        }

        internal BlueprintChunkEnumerator(in Blueprint blueprint)
        {
            _definition = blueprint._definition;
            IsValid = false;
            _currentChunk = null;
        }

        public bool MoveNext()
        {
            if(_definition->EntityCount == 0) return false;
            
            _currentChunk = IsValid ? _currentChunk->NextChunk : _definition->FirstChunk;
            IsValid = _currentChunk != null;
            
            return IsValid;
        }

        public ref BlueprintChunk Current => ref Unsafe.AsRef<BlueprintChunk>(_currentChunk);
    }
}