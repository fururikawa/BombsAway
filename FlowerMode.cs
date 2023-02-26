using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombsAway
{
    internal sealed class FlowerMode : BaseObjectMode
    {
        public override void Setup()
        {
            _possibleTileObjects = WorldManager.manageWorld.allObjectSettings
                .Where(t => t.beautyType == TownManager.TownBeautyType.Flowers &&
                    t.isSmallPlant &&
                    t.tileObjectId != 434)
                .Select(t => t.tileObjectId);
        }

        public override int GetTileType(int xPos, int yPos, int newX, int newY, int tileObjectId)
        {
            if (GenerateMap.generate.coldLandGrowBack.objectsInBiom.FirstOrDefault(x => x?.tileObjectId == tileObjectId))
            {
                return (int)TileTypes.tiles.PineGrass;
            }
            else if (GenerateMap.generate.tropicalGrowBack.objectsInBiom.FirstOrDefault(x => x?.tileObjectId == tileObjectId))
            {
                return (int)TileTypes.tiles.TropicalGrass;
            }
            else if (GenerateMap.generate.desertRainGrowBack.objectsInBiom.FirstOrDefault(x => x?.tileObjectId == tileObjectId))
            {
                return (int)TileTypes.tiles.Dirt;
            }

            return (int)TileTypes.tiles.Grass;
        }

        public override void AfterEffects(int xPos, int xyos, int newX, int newY, int tileObjectId)
        {
            var particleSystem = ParticleManager.manage.explosion;
            var main = particleSystem.main;
            var color = new Color();
            var position = new Vector3(newX * 2f, 0, newY * 2f) + Vector3.up * WorldManager.manageWorld.heightMap[newX, newY];

            switch (tileObjectId)
            {
                case 201:
                    color = new Color(0.58f, 0f, 0.82f, 0.85f);
                    break;
                case 202:
                    color = new Color(1.0f, 1.0f, 0.0f, 0.85f);
                    break;
                case 203:
                    color = new Color(0.98f, 0.42f, 0.62f, 0.85f);
                    break;
                case 204:
                    color = new Color(0.39f, 0.32f, 0.67f, 0.85f);
                    break;
                case 205:
                    color = new Color(1.0f, 0.26f, 0.26f, 0.85f);
                    break;
            }
            main.startColor = color;
            ParticleManager.manage.emitParticleAtPosition(particleSystem, position, 5);
        }

        public sealed override string Name => "Flowery";
    }
}