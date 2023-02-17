using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombsAway
{
    internal sealed class SillyMode : BaseObjectMode
    {
        public override void Setup()
        {
            _possibleTileObjects = WorldManager.manageWorld.allObjectSettings
                .Where(t => !t.isMultiTileObject &&
                    !t.isFence &&
                    !t.isFlowerBed &&
                    !t.GetComponent<TileObjectConnect>() &&
                    t.canBePickedUp &&
                    (t.tileObjectId < 309 || t.tileObjectId > 313) &&
                    t.tileObjectId != 343)
                .Select(x => x.tileObjectId);
        }

        public override int GetTileType(int xPos, int yPos, int newX, int newY, int tileObjectId)
        {
            return Random.Range(0, (int)TileTypes.tiles.BasicRockPath);
        }

        public sealed override string Name => "Silly";
    }
}