using System;
using System.Collections.Generic;
using System.Linq;

namespace BombsAway
{
    public static class BombExplodesHelper
    {
        public static int ExplosionRadius = 3;
        public static bool IsInfiniteBombs = false;

        public static bool IsInverted
        {
            get
            {
                return _isInverted;
            }
        }
        public static BombModes BombModeActive
        {
            get
            {
                return _bombModeActive;
            }
        }

        public static IEnumerable<Tuple<int, int>> ExplosionCoordinates
        {
            get
            {
                return _explosionCoordinates;
            }
        }

        public static void ComputeExplosionGrid()
        {
            var coordinates = new List<Tuple<int, int, int>>();
            for (int i = -ExplosionRadius; i <= ExplosionRadius; i++)
            {
                for (int j = -ExplosionRadius; j <= ExplosionRadius; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    coordinates.Add(new Tuple<int, int, int>(i, j, Math.Abs(i) + Math.Abs(j))); // Store x, y, and distance from center
                }
            }
            _explosionCoordinates = coordinates.OrderBy(x => x.Item3)
                .Select(x => new Tuple<int, int>(x.Item1, x.Item2))
                .ToArray();
        }

        public static void InvertBombState()
        {
            _isInverted = !_isInverted;
        }

        public static void CycleBombMode()
        {
            switch (_bombModeActive)
            {
                case BombModes.Vanilla:
                    _bombModeActive = BombModes.Flowers;
                    break;
                case BombModes.Flowers:
                    _bombModeActive = BombModes.Silly;
                    break;
                case BombModes.Silly:
                    _bombModeActive = BombModes.Vanilla;
                    break;
            }
        }

        public static String GetBombModeActive()
        {
            switch (_bombModeActive)
            {
                case BombModes.Vanilla:
                    return "Vanilla";
                case BombModes.Flowers:
                    return "Flower";
                case BombModes.Silly:
                    return "Silly";
            }
            return "Vanilla";
        }

        public static int Fibo(int index)
        {
            return _fibonacciNumbers[index];
        }

        private static bool _isInverted = false;
        private static BombModes _bombModeActive = BombModes.Vanilla;
        private static int[] _fibonacciNumbers = new int[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };
        private static Tuple<int, int>[] _explosionCoordinates;
    }

    public enum BombModes
    {
        Vanilla,
        Flowers,
        Silly
    }
}