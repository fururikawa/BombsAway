using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BombsAway
{
    public sealed class FlowerMode : BaseObjectMode
    {
        public sealed override IEnumerable<int> PossibleTileObjects
        {
            get
            {
                if (_possibleTileObjects == null)
                {
                    _possibleTileObjects = WorldManager.manageWorld.allObjectSettings
                        .Where(t => t.beautyType == TownManager.TownBeautyType.Flowers &&
                            t.isSmallPlant &&
                            t.tileObjectId != 434)
                        .Select(t => t.tileObjectId);
                }

                return _possibleTileObjects;
            }
        }

        public sealed override string Name => "Flowery";
    }
}