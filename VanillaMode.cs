using System.Collections;
using System.Collections.Generic;

namespace BombsAway
{
    internal sealed class VanillaMode : BaseObjectMode
    {
        public sealed override IEnumerable<int> PossibleTileObjects
        {
            get
            {
                return new int[] { -1 };
            }
        }

        public sealed override string Name => "Vanilla";
        public sealed override IEnumerator GetNextTilesToUse(int quantity)
        {
            yield break;
        }
    }
}