using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombsAway
{
    internal sealed class SillyMode : BaseObjectMode
    {
        public override void Setup()
        {
            var selectedTileObjects = WorldManager.manageWorld.allObjectSettings
                .Where(t => !t.isMultiTileObject &&
                    !t.isFence &&
                    !t.isFlowerBed &&
                    !t.GetComponent<TileObjectConnect>() &&
                    t.canBePickedUp &&
                    (t.tileObjectId < 309 || t.tileObjectId > 313) &&
                    t.tileObjectId != 343);

                if (!BombManager.Instance.AllowBerleyBoxes)
                {
                    selectedTileObjects = selectedTileObjects.Where(t => t.tileObjectId < 351 || t.tileObjectId > 353);
                }

                _possibleTileObjects = selectedTileObjects.Select(t => t.tileObjectId);
        }

        public override int GetTileType(int xPos, int yPos, int newX, int newY, int tileObjectId)
        {
            return Random.Range(0, 34);
        }

        public sealed override string Name => "Silly";
    }
}