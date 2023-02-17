using System.Collections;
using UnityEngine;

namespace BombsAway
{
    public class BombExplodesHelper
    {
        private static BombExplodesHelper _instance;

        public static BombExplodesHelper Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new BombExplodesHelper();
                return _instance;
            }
        }

        public void blowUpPos(BombExplodes instance, int xPos, int yPos, int xDif, int yDif, int initialHeight)
        {
            int newX = xPos + xDif;
            int newY = yPos + yDif;

            if (WorldManager.manageWorld.isPositionOnMap(newX, newY) && (shouldDestroyOnTile(newX, newY) || WorldManager.manageWorld.onTileMap[newX, newY] == -1))
            {
                if (WorldManager.manageWorld.onTileMap[newX, newY] != -1)
                {
                    NetworkMapSharer.share.RpcUpdateOnTileObject(-1, newX, newY);
                }

                updateTileHeight(initialHeight, newX, newY, xDif, yDif);

                placeWorldObject(newX, newY, BombManager.Instance.GetActiveMode().NextRandomTileObjectID());
            }
        }

        private void placeWorldObject(int xPos, int yPos, int tileObjectId)
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

        public IEnumerator blowUpPosCoroutine(BombExplodes instance, int xPos, int yPos, int initialHeight)
        {
            foreach (var coords in BombManager.Instance.ExplosionCoordinates)
            {
                blowUpPos(instance, xPos, yPos, coords.Item1, coords.Item2, initialHeight);
            }
            yield break;
        }

        private void updateTileHeight(int initialHeight, int newX, int newY, int xDif, int yDif)
        {
            int distance = Mathf.Abs(xDif) + Mathf.Abs(yDif);
            int heightDif = WorldManager.manageWorld.heightMap[newX, newY] - initialHeight;
            int newHeight = 0;
            if (BombManager.Instance.IsInverted)
            {
                if (heightDif <= BombManager.ExplosionRadius && heightDif >= -1 - BombManager.ExplosionRadius + distance)
                    newHeight = Mathf.Clamp(BombManager.ExplosionRadius * 2 - BombManager.Instance.Fibo(distance), 0, BombManager.ExplosionRadius + 1);
            }
            else
            {
                if (heightDif >= 0 && heightDif <= 1 + BombManager.ExplosionRadius - distance)
                    newHeight = -Mathf.Clamp((BombManager.ExplosionRadius * 2 - BombManager.Instance.Fibo(distance)), 0, BombManager.ExplosionRadius + 1);
            }
            NetworkMapSharer.share.RpcUpdateTileHeight(newHeight, newX, newY);
        }

        private bool shouldDestroyOnTile(int xPos, int yPos)
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