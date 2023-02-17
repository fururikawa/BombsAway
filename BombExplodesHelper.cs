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

            if (WorldManager.manageWorld.isPositionOnMap(newX, newY) && (ShouldDestroyOnTile(newX, newY) || WorldManager.manageWorld.onTileMap[newX, newY] == -1))
            {
                if (WorldManager.manageWorld.onTileMap[newX, newY] != -1)
                {
                    NetworkMapSharer.share.RpcUpdateOnTileObject(-1, newX, newY);
                }

                updateTileHeight(initialHeight, newX, newY, xDif, yDif);

                var bombModeActive = BombManager.Instance.GetActiveMode();
                var randomTileObjectId = bombModeActive.NextRandomTileObjectID();
                NetworkMapSharer.share.RpcUpdateOnTileObject(randomTileObjectId, newX, newY);
                NetworkMapSharer.share.RpcUpdateTileType(bombModeActive.GetTileType(xPos, yPos, newX, newY, randomTileObjectId), newX, newY);
            }
        }

        internal static IEnumerator StartExplosion(BombExplodes instance, int xPos, int yPos, int initialHeight)
        {
            foreach (var coords in BombManager.Instance.ExplosionCoordinates)
            {
                ExplodeTile(instance, xPos, yPos, coords.Item1, coords.Item2, initialHeight);
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

        private static void placeWorldObject(int xPos, int yPos, int tileObjectId)
        {
            if (tileObjectId != -1)
            {
                if (WorldManager.manageWorld.allObjects[tileObjectId].tileObjectGrowthStages)
                {
                    var growthStages = WorldManager.manageWorld.allObjects[tileObjectId].tileObjectGrowthStages.objectStages;
                    WorldManager.manageWorld.onTileStatusMap[xPos, yPos] = UnityEngine.Random.Range(0, growthStages.Length);
                }

                WorldManager.manageWorld.onTileMap[xPos, yPos] = tileObjectId;
            }
        }

        private static void updateTileHeight(int initialHeight, int newX, int newY, int xDif, int yDif)
        {
            int distance = Mathf.Abs(xDif) + Mathf.Abs(yDif);
            int heightDif = WorldManager.manageWorld.heightMap[newX, newY] - initialHeight;
            int newHeight = 0;
            int radius = BombManager.Instance.Radius;
            int distanceFactor = BombManager.Instance.GetActiveMode().DistanceFactorFunction(distance);
            float modifier = BombManager.Instance.GetActiveMode().ExplosionModifier;

            if (BombManager.Instance.IsInverted)
            {
                if (heightDif <= radius && heightDif >= -1 - radius + distance)
                    newHeight = Mathf.RoundToInt(modifier * Mathf.Clamp(radius * 2 - distanceFactor, 0, radius + 1));
            }
            else
            {
                if (heightDif >= 0 && heightDif <= 1 + radius - distance)
                    newHeight = -Mathf.RoundToInt(modifier * Mathf.Clamp((radius * 2 - distanceFactor), 0, radius + 1));
            }
            NetworkMapSharer.share.RpcUpdateTileHeight(newHeight, newX, newY);
        }
    }
}