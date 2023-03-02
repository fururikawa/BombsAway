using System.Collections;
using UnityEngine;

namespace BombsAway
{
    internal static class BombExplodesHelper
    {
        internal static void ExplodeTile(BombExplodes instance, int xPos, int yPos, int xDif, int yDif, int initialHeight)
        {
            int newX = xPos + xDif;
            int newY = yPos + yDif;

            if (WorldManager.manageWorld.isPositionOnMap(newX, newY) && (WorldManager.manageWorld.onTileMap[newX, newY] == -1 || ShouldDestroyOnTile(newX, newY)))
            {
                var bombModeActive = BombManager.Instance.GetActiveMode();

                int newHeight = bombModeActive.GetTileHeightDifference(initialHeight, newX, newY, xDif, yDif);
                NetworkMapSharer.share.RpcUpdateTileHeight(newHeight, newX, newY);
                int tileObjectId = bombModeActive.GetNextTileObjectID(xPos, yPos, newX, newY);
                NetworkMapSharer.share.RpcUpdateOnTileObject(tileObjectId, newX, newY);
                int tileTypeId = bombModeActive.GetTileType(xPos, yPos, newX, newY, tileObjectId);
                NetworkMapSharer.share.RpcUpdateTileType(tileTypeId, newX, newY);

                bombModeActive.AfterEffects(xPos, yPos, newX, newY, tileObjectId);

                WorldManager.manageWorld.heightChunkHasChanged(newX, newY);
            }
        }

        internal static IEnumerator StartExplosion(BombExplodes instance, int xPos, int yPos, int initialHeight)
        {
            int i = 0;
            int stages = (int)Mathf.Pow(BombManager.Instance.Radius, 3);
            foreach (var coords in BombManager.Instance.ExplosionCoordinates)
            {
                ExplodeTile(instance, xPos, yPos, coords.Item1, coords.Item2, initialHeight);
                if (++i % stages == 0)
                {
                    yield return null;
                }
            }
            yield break;
        }

        private static bool ShouldDestroyOnTile(int xPos, int yPos)
        {
            return WorldManager.manageWorld.onTileMap[xPos, yPos] > -1 &&
                (WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isWood ||
                    WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isHardWood ||
                    WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isSmallPlant ||
                    WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isStone ||
                    WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isHardStone ||
                    (WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isHardStone &&
                        WorldManager.manageWorld.allObjectSettings[WorldManager.manageWorld.onTileMap[xPos, yPos]].isMultiTileObject));
        }
    }
}
