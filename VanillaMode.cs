using System.Collections;
using System.Collections.Generic;

namespace BombsAway
{
    internal sealed class VanillaMode : BaseObjectMode
    {
        public sealed override string Name => "Vanilla";
        public sealed override IEnumerator GetNextTilesToUse(int quantity)
        {
            yield break;
        }
        public override void Setup()
        {
            _possibleTileObjects = new int[] { -1 };
        }
    }
}