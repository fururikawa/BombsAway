using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        private BombManager()
        {
            _bombModes = new List<BaseObjectMode>();
            _explosionCoordinates = new Tuple<int, int>[] { };
            _fibonacci = new int[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89 };
            _currentMode = 0;
            _isInverted = false;
            _radius = 2;
            _isInfiniteBombs = false;
        }

        public int Radius
        {
            get => _radius;
        }

        public bool IsInverted
        {
            get => _isInverted;
        }
        public bool IsInfiniteBombs
        {
            get => _isInfiniteBombs;
            set => _isInfiniteBombs = value;
        }

        public BaseObjectMode GetActiveMode()
        {
            return _bombModes.ElementAt(_currentMode);
        }

        public BaseObjectMode FindModeByName(String name)
        {
            return _bombModes.FirstOrDefault(x => x.Name == name);
        }

        public IEnumerable<BaseObjectMode> BombModes
        {
            get => _bombModes;
        }

        public IEnumerable<Tuple<int, int>> ExplosionCoordinates
        {
            get => _explosionCoordinates;
        }

        public void ToggleBombState()
        {
            _isInverted = !_isInverted;
        }

        public void CycleBombModes()
        {
            do
            {
                if (_currentMode == _bombModes.Count - 1)
                {
                    _currentMode = 0;
                }
                else
                {
                    _currentMode++;
                }
            }
            while (!GetActiveMode().Enabled);
        }

        public void SetRadius(uint newRadius)
        {
            _radius = (int)newRadius;
            GenerateExplosionGrid();
        }

        public int Fibonacci(int distance)
        {
            return _fibonacci[distance];
        }

        internal void Register(BaseObjectMode newMode)
        {
            _bombModes.Add(newMode);
        }

        internal void GenerateExplosionGrid()
        {
            var coordinates = new List<Tuple<int, int, int>>();

            for (int i = -_radius; i <= _radius; i++)
            {
                for (int j = -_radius; j <= _radius; j++)
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

        private bool _isInverted;
        private Tuple<int, int>[] _explosionCoordinates;
        private IList<BaseObjectMode> _bombModes;
        private int[] _fibonacci;
        private int _currentMode;
        private int _radius;
        private bool _isInfiniteBombs;
    }
}