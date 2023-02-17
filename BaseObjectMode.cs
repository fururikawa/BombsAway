using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BombsAway
{
    public abstract class BaseObjectMode
    {
        protected Queue<int> _nextTilesToUse;
        protected MapRand _random;
        protected IEnumerable<int> _possibleTileObjects;

        protected BaseObjectMode()
        {
            _nextTilesToUse = new Queue<int>();
        }

        public abstract IEnumerable<int> PossibleTileObjects { get; }
        public abstract String Name { get; }

        public virtual IEnumerator GetNextTilesToUse(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                _nextTilesToUse.Enqueue(GetRandomTileObjectID());
            }
            yield break;
        }
        public int NextRandomTileObjectID()
        {
            if (_nextTilesToUse.Count > 0)
                return _nextTilesToUse.Dequeue();

            return GetRandomTileObjectID();
        }

        protected virtual int GetRandomTileObjectID()
        {
            return PossibleTileObjects.ElementAt(Rng().Range(0, PossibleTileObjects.Count()));
        }

        protected virtual MapRand Rng()
        {
            if (_random == null)
            {
                _random = new MapRand(new Random().Next(-16545, 16545));
            }
            return _random;
        }
    }
}