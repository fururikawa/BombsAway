using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombsAway
{
    public abstract class BaseObjectMode
    {
        protected Queue<int> _nextTilesToUse;
        protected MapRand _random;
        /// <summary>A collection containing all possible tile object IDs to spawn.</summary>
        protected IEnumerable<int> _possibleTileObjects;
        protected bool _enabled;
        protected float _explosionModifier;
        protected Func<int, int> _distanceFactorFunction;
        protected Color[] rainbowColors = new Color[] { new Color(0.58f, 0f, 0.82f), new Color(0.29f, 0, 0.5f), new Color(0, 0.31f, 1f), new Color(0, 1f, 0), new Color(1f, 1f, 0), new Color(1f, 0.64f, 0), new Color(1f, 0, 0.31f) };

        protected BaseObjectMode()
        {
            _random = new MapRand(UnityEngine.Random.Range(-16545, 16545));
            _possibleTileObjects = new int[] { -1 };
            _nextTilesToUse = new Queue<int>();
            _distanceFactorFunction = BombManager.Instance.Fibonacci;
            _explosionModifier = 1.0f;
            _enabled = true;
        }

        public IEnumerable<int> PossibleTileObjects { get => _possibleTileObjects; }
        /// <summary>Affects the resulting height of the tiles destroyed/created.
        /// Must be between 0 and 1, inclusive. Default is 1.</summary>
        public float ExplosionModifier
        {
            get => _explosionModifier;
            set => _explosionModifier = Mathf.Clamp01(value);
        }
        /// <summary>Only change this if you know what you're doing.
        /// Affects the resulting height of the tiles destroyed/created based on their
        /// distance from the center of the explosion.</summary>
        public Func<int, int> DistanceFactorFunction
        {
            get => _distanceFactorFunction;
            set => _distanceFactorFunction = value;
        }
        public abstract String Name { get; }
        /// <summary>
        /// This is the entry method for your mode. You can do whatever you want here to make ready
        /// for the sparks show, but you absolutely must set the value for <c>_possibleTileObjects</c>
        /// or your bomb will work just like vanilla.
        /// </summary>
        public abstract void Setup();
        /// <summary>The tile type to change to after exploding it. Refer to <c>TileTypes.tiles</c> for acceptable values.
        /// The parameters are informational, they're there in case you want to make the return value conditional on any of them.</summary>
        /// <param name="xPos">The X position of the explosion origin.</param>
        /// <param name="yPos">The Y position of the explosion origin.</param>
        /// <param name="newX">The X position of the tile being updated.</param>
        /// <param name="newY">The Y position of the tile being updated.</param>
        /// <param name="tileObjectId">The <c>tileObjectId</c> that was selected in <c>NextRandomTileObjectID()</c>.</param>
        public virtual int GetTileType(int xPos, int yPos, int newX, int newY, int tileObjectId)
        {
            return 0;
        }
        /// <summary>Control whether this mode will be included in the cycling of modes.</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public virtual void GetNextTilesToUse(int quantity)
        {
            for (int i = 0; i < quantity; i++)
            {
                _nextTilesToUse.Enqueue(GetRandomTileObjectID());
            }
        }

        public virtual int GetNextTileObjectID(int xPos, int yPos, int newX, int newY)
        {
            if (_nextTilesToUse.Count > 0)
                return _nextTilesToUse.Dequeue();

            return GetRandomTileObjectID();
        }

        public virtual IEnumerator Prepare()
        {
            GetNextTilesToUse(BombManager.Instance.ExplosionCoordinates.Count());

            var colorKeys = new GradientColorKey[7];
            var alphaKeys = new GradientAlphaKey[1] { new GradientAlphaKey(1.0f, 0.0f) };

            for (int i = 0; i < rainbowColors.Length; i++)
            {
                colorKeys[i] = new GradientColorKey(rainbowColors[i], i * 0.14f);
            }

            var rainbow = new Gradient();
            rainbow.mode = GradientMode.Blend;
            rainbow.SetKeys(colorKeys, alphaKeys);

            ParticleSystem particleSystem = ParticleManager.manage.explosion;
            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(rainbow);
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false;

            yield break;
        }

        protected virtual int GetRandomTileObjectID()
        {
            return PossibleTileObjects.ElementAt(Rng().Range(0, PossibleTileObjects.Count()));
        }

        public virtual int GetTileHeightDifference(int initialHeight, int newX, int newY, int xDif, int yDif)
        {
            int distance = Mathf.Abs(xDif) + Mathf.Abs(yDif);
            int heightDif = WorldManager.manageWorld.heightMap[newX, newY] - initialHeight;
            int newHeight = 0;
            int radius = BombManager.Instance.Radius;
            int distanceFactor = BombManager.Instance.GetActiveMode().DistanceFactorFunction(distance);
            float modifier = BombManager.Instance.GetActiveMode().ExplosionModifier;

            switch (BombManager.Instance.BombState)
            {
                case 0:
                    if (heightDif >= 0 && heightDif <= 1 + radius - distance)
                        newHeight = -Mathf.RoundToInt(modifier * Mathf.Clamp((radius * 2 - distanceFactor), 0, radius + 1));
                    break;
                case 1:
                    if (heightDif <= radius && heightDif >= -1 - radius + distance)
                        newHeight = Mathf.RoundToInt(modifier * Mathf.Clamp(radius * 2 - distanceFactor, 0, radius + 1));
                    break;
                case 2:
                    newHeight = -heightDif;
                    break;
            }

            return newHeight;
        }

        public virtual void AfterEffects(int xPos, int yPos, int newX, int newY, int tileObjectId) { }

        public virtual void CleanUp() { }

        protected virtual MapRand Rng()
        {
            return _random;
        }
    }
}