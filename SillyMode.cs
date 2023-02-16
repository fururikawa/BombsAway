using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombsAway
{
    public static class SillyMode
    {
        private static Queue<int> _nextTilesToUse = new Queue<int>();
        private static MapRand _random;
        private static IEnumerable<TileObjectSettings> _possibleTileObjects;
        private static int _possibleTileObjectsCount;
        public static IEnumerable<TileObjectSettings> PossibleTileObjects
        {
            get
            {
                if (_possibleTileObjects == null)
                {
                    _possibleTileObjects = WorldManager.manageWorld.allObjectSettings
                        .Where(t => !t.isMultiTileObject && !t.isFence && !t.isFlowerBed && t.canBePickedUp && (t.tileObjectId < 309 || t.tileObjectId > 312));
                    _possibleTileObjectsCount = _possibleTileObjects.Count();
                }

                return _possibleTileObjects;
            }
        }

        public static IEnumerator GetNextTilesToUse(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                _nextTilesToUse.Enqueue(GetRandomTileObjectID());
            }
            yield break;
        }

        public static int NextRandomTileObjectID()
        {
            if (_nextTilesToUse.Count > 0)
                return _nextTilesToUse.Dequeue();

            return GetRandomTileObjectID();
        }
        private static int GetRandomTileObjectID()
        {
            return PossibleTileObjects.ElementAt(Rng().Range(0, _possibleTileObjectsCount)).tileObjectId;
        }

        private static MapRand Rng()
        {
            if (_random == null)
            {
                _random = new MapRand(Random.RandomRangeInt(-16545, 16545));
            }
            return _random;
        }
    }
}