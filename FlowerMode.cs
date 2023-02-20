using System.Linq;

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

        public sealed override string Name => "Flowery";
    }
}