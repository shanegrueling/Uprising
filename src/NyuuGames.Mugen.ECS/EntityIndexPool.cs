namespace NyuuGames.Mugen.ECS
{
    using System.Collections.Generic;

    internal sealed class EntityIndexPool
    {
        private readonly Queue<int> _freeIndices = new Queue<int>();
        private int _highestUsedIndex;

        public int GetNext()
        {
            if (_freeIndices.Count > 0) return _freeIndices.Dequeue();

            var idx = _highestUsedIndex;
            ++_highestUsedIndex;
            return idx;
        }

        public void Return(int idx)
        {
            _freeIndices.Enqueue(idx);
        }
    }
}