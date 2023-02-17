using System;
using System.Collections.Generic;
using System.Linq;

namespace BombsAway
{
    public class BombManager
    {
        private static BombManager _instance;

        public static BombManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BombManager();
                return _instance;
            }
        }

        public static int ExplosionRadius = 3;
        public static bool IsInfiniteBombs = false;

        private BombManager()
        {
            bombModes = new List<BaseObjectMode>();
            currentMode = 0;
        }

        public void Register(BaseObjectMode newMode)
        {
            bombModes.Add(newMode);
        }

        public BaseObjectMode GetActiveMode()
        {
            return bombModes.ElementAt(currentMode);
        }

        public IEnumerable<BaseObjectMode> BombModes
        {
            get
            {
                return bombModes;
            }
        }

        public bool IsInverted
        {
            get
            {
                return _isInverted;
            }
        }

        public IEnumerable<Tuple<int, int>> ExplosionCoordinates
        {
            get
            {
                return _explosionCoordinates;
            }
        }

        public void ComputeExplosionGrid()
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

        public void InvertBombState()
        {
            _isInverted = !_isInverted;
        }

        public void CycleBombMode()
        {
            if (currentMode == bombModes.Count - 1)
            {
                currentMode = 0;
                return;
            }
            currentMode++;
        }
        public int Fibo(int index)
        {
            return _fibonacciNumbers[index];
        }

        private bool _isInverted = false;
        private int[] _fibonacciNumbers = new int[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };
        private Tuple<int, int>[] _explosionCoordinates;
        private IList<BaseObjectMode> bombModes;
        private int currentMode;
    }
}