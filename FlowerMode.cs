using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BombsAway
{
    public static class FlowerMode
    {
        private static MapRand _random;
        private static int[] FlowerIDs = new int[] { 201, 202, 203, 204, 205 };
        private static Queue<int> _nextFlowerIDs = new Queue<int>();

        public static IEnumerator GetNextFlowerIDs(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                _nextFlowerIDs.Enqueue(GetRandomFlowerID());
            }
            yield break;
        }

        public static int NextFlowerId()
        {
            if (_nextFlowerIDs.Count > 0)
                return _nextFlowerIDs.Dequeue();
            return GetRandomFlowerID();
        }

        private static int GetRandomFlowerID()
        {
            return FlowerIDs[Rng().Range(0, FlowerIDs.Length)];
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