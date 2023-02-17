using System.Collections.Generic;
using System.Linq;

namespace BombsAway
{
    public sealed class SillyMode : BaseObjectMode
    {
        public sealed override IEnumerable<int> PossibleTileObjects
        {
            get
            {
                if (_possibleTileObjects == null)
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

                return _possibleTileObjects;
            }
        }

        public sealed override string Name => "Silly";
    }
}