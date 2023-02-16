using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace BombsAway
{

    [HarmonyPatch]
    public static class BombExplodesPatch
    {
        public static int _explosionRadius = 3;
        public static bool _inverted = false;
        public static BombModes _bombModeActive = BombModes.Vanilla;

        [HarmonyPatch(typeof(CraftingManager), "Start")]
        [HarmonyPostfix]
        public static void SetRecipeAmounts()
        {
            Inventory.inv.allItems[277].craftable.recipeGiveThisAmount = 8;
        }

        [HarmonyPatch(typeof(Inventory), "consumeItemInHand")]
        [HarmonyPrefix]
        public static bool consumeItemInHandPrefix(Inventory __instance)
        {
            if (__instance.invSlots[__instance.selectedSlot].itemNo == 277)
                return false;
            return true;
        }

        [HarmonyPatch(typeof(BombExplodes), "explodeTimer")]
        [HarmonyPrefix]
        public static bool explodeTimerPrefix(BombExplodes __instance, ref IEnumerator __result)
        {
            if (_bombModeActive == BombModes.Silly)
            {
                baseStartCoroutine(__instance, SillyMode.GetNextTilesToUse((int)Mathf.Pow((_explosionRadius * 2 + 1), 2)));
            }
            else if (_bombModeActive == BombModes.Flowers)
            {
                baseStartCoroutine(__instance, FlowerMode.GetNextFlowerIDs((int)Mathf.Pow((_explosionRadius * 2 + 1), 2)));
            }

            __result = explodeTimer(__instance);

            return false;
        }

        public static void CycleBombMode()
        {
            switch (_bombModeActive)
            {
                case BombModes.Vanilla:
                    _bombModeActive = BombModes.Flowers;
                    break;
                case BombModes.Flowers:
                    _bombModeActive = BombModes.Silly;
                    break;
                case BombModes.Silly:
                    _bombModeActive = BombModes.Vanilla;
                    break;
            }
        }

        public static String GetBombModeActive()
        {
            switch (_bombModeActive)
            {
                case BombModes.Vanilla:
                    return "Vanilla";
                case BombModes.Flowers:
                    return "Flower";
                case BombModes.Silly:
                    return "Silly";
            }
            return "Vanilla";
        }

        private static bool shouldDestroyOnTile(int xPos, int yPos)
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

        private static void blowUpPos(BombExplodes instance, int xPos, int yPos, int xDif, int yDif, int initialHeight)
        {
            int newX = xPos + xDif;
            int newY = yPos + yDif;

            if (WorldManager.manageWorld.isPositionOnMap(newX, newY) && (shouldDestroyOnTile(newX, newY) || WorldManager.manageWorld.onTileMap[newX, newY] == -1))
            {
                if (WorldManager.manageWorld.onTileMap[newX, newY] != -1)
                {
                    NetworkMapSharer.share.RpcUpdateOnTileObject(-1, newX, newY);
                }

                if (!_inverted)
                {
                    updateTile(initialHeight, newX, newY, xDif, yDif);
                }
                else
                {
                    updateTileInverted(((byte)initialHeight), newX, newY, xDif, yDif);
                }

                if (_bombModeActive == BombModes.Silly)
                {
                    placeWorldObject(newX, newY, SillyMode.NextRandomTileObjectID());
                }
                else if (_bombModeActive == BombModes.Flowers)
                {
                    NetworkMapSharer.share.RpcUpdateOnTileObject(FlowerMode.NextFlowerId(), newX, newY);
                }
            }
        }

        static int[] fibonacci = new int[] { 1, 1, 2, 3, 5, 8, 13, 21 };

        private static void updateTile(int initialHeight, int newX, int newY, int xDif, int yDif)
        {
            int distance = (Mathf.Abs(xDif) + Mathf.Abs(yDif));
            int heightDif = WorldManager.manageWorld.heightMap[newX, newY] - initialHeight;
            if (heightDif >= 0 && heightDif <= 1 + _explosionRadius - distance)
            {
                NetworkMapSharer.share.RpcUpdateTileHeight(Mathf.RoundToInt(-3 / (float)fibonacci[distance]) - heightDif * Mathf.Clamp(distance, 0, 1), newX, newY);
            }
        }

        private static void updateTileInverted(int initialHeight, int newX, int newY, int xDif, int yDif)
        {
            int distance = (Mathf.Abs(xDif) + Mathf.Abs(yDif));

            NetworkMapSharer.share.RpcUpdateTileHeight(Mathf.Clamp((1 + _explosionRadius) - fibonacci[distance], 0, _explosionRadius + 1), newX, newY);
        }

        private static void attackAndDoDamage(Damageable instance, int damageToDeal, Transform attackedBy, float knockBackAmount = 2.5f)
        {
            bool canBeDamaged = (bool)typeof(Damageable).GetField("canBeDamaged", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
            AnimalAI myAnimalAi = (AnimalAI)typeof(Damageable).GetField("myAnimalAi", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
            if (canBeDamaged)
            {
                baseStartCoroutineStr(instance, "canBeDamagedDelay");
                if (knockBackAmount > 0f && instance.myChar)
                {
                    Vector3 knockBackDir = -(attackedBy.position - Transform(instance).position).normalized;
                    knockBackDir.y = 0.75f;
                    instance.myChar.RpcTakeKnockback(knockBackDir, knockBackAmount * 3.5f);
                }
                if (myAnimalAi && attackedBy)
                {
                    myAnimalAi.takeHitAndKnockBack(attackedBy, knockBackAmount);
                }
                if (_bombModeActive == BombModes.Vanilla)
                    instance.changeHealth(-Mathf.RoundToInt((float)damageToDeal / instance.defence));
            }
        }

        private static void placeWorldObject(int xPos, int yPos, int tileObjectId)
        {
            if (tileObjectId != -1 && WorldManager.manageWorld.onTileMap[xPos, yPos] == -1)
            {
                if (WorldManager.manageWorld.allObjects[tileObjectId].tileObjectGrowthStages)
                {
                    WorldManager.manageWorld.onTileStatusMap[xPos, yPos] = WorldManager.manageWorld.allObjects[tileObjectId].tileObjectGrowthStages.objectStages.Length - 1;
                }

                WorldManager.manageWorld.onTileMap[xPos, yPos] = tileObjectId;
            }
        }

        private static Tuple<int, int>[] nextCoordinates;

        public static void calcCoordinates(int radius)
        {
            var coordinates = new List<Tuple<int, int, int>>();
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    coordinates.Add(new Tuple<int, int, int>(i, j, Mathf.Abs(i) + Mathf.Abs(j)));
                }
            }
            nextCoordinates = coordinates.OrderBy(x => x.Item3).Select(x => new Tuple<int, int>(x.Item1, x.Item2)).ToArray();
        }

        private static IEnumerator blowUpPosCoroutine(BombExplodes instance, int radius, int xPos, int yPos, int initialHeight)
        {
            foreach (var coords in nextCoordinates)
            {
                blowUpPos(instance, xPos, yPos, coords.Item1, coords.Item2, initialHeight);
            }
            yield break;
        }

        private static IEnumerator explodeTimer(BombExplodes instance)
        {
            float timeBeforeExplode = 2.2f;
            int xPos = Mathf.RoundToInt(Transform(instance).position.x / 2f);
            int yPos = Mathf.RoundToInt(Transform(instance).position.z / 2f);
            int initialHeight = WorldManager.manageWorld.heightMap[xPos, yPos];

            int fallToHeight = -2;
            if (WorldManager.manageWorld.isPositionOnMap(xPos, yPos))
            {
                fallToHeight = WorldManager.manageWorld.heightMap[xPos, yPos];
            }
            new Vector3(Transform(instance).position.x, (float)fallToHeight, Transform(instance).position.z);
            float fallSpeed = 9f;

            while (timeBeforeExplode > 0f)
            {
                timeBeforeExplode -= Time.deltaTime;
                RaycastHit raycastHit;

                if (Transform(instance).position.y > (float)fallToHeight && !Physics.Raycast(Transform(instance).position - Vector3.up / 10f, Vector3.down, out raycastHit, 0.12f, instance.landOnMask))
                {
                    Transform(instance).position += Vector3.down * Time.deltaTime * fallSpeed;
                    fallSpeed = Mathf.Lerp(fallSpeed, 15f, Time.deltaTime * 2f);
                }
                yield return null;
            }
            Vector3 particlePos = Transform(instance).position;
            SoundManager.manage.playASoundAtPoint(instance.bombExplodesSound, particlePos, 1f, 1f);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.explosion, particlePos, 60);
            float num = Vector3.Distance(CameraController.control.transform.position, Transform(instance).position);
            float num2 = 0.75f - Mathf.Clamp(num / 25f, 0f, 0.75f);
            if (num2 > 0f)
            {
                CameraController.control.shakeScreen(num2);
            }
            baseStartCoroutine(instance, explosionLightFlash(instance));
            instance.hideOnExplode.SetActive(false);
            if (baseIsServer(instance))
            {
                List<int> multiTileObjects = new List<int>();
                blowUpPos(instance, xPos, yPos, 0, 0, initialHeight);
                Collider[] array = Physics.OverlapSphere(Transform(instance).position - Vector3.up * 1.5f, 4f, LayerMask.GetMask("Default") | instance.damageLayer);
                for (int i = 0; i < array.Length; i++)
                {
                    Damageable component = array[i].transform.root.GetComponent<Damageable>();
                    if (component)
                    {
                        attackAndDoDamage(component, 25, Transform(instance), 20f);
                        if (_bombModeActive == BombModes.Vanilla)
                            component.setOnFire();
                    }

                    TileObject tileObject = array[i].transform.root.GetComponent<TileObject>();
                    if (tileObject && !multiTileObjects.Contains(tileObject.GetInstanceID()) && tileObject.isMultiTileObject() && WorldManager.manageWorld.allObjectSettings[tileObject.tileObjectId].isHardStone)
                    {
                        multiTileObjects.Add(tileObject.GetInstanceID());
                        tileObject.damage(true, true);
                        NetworkMapSharer.share.RpcRemoveMultiTiledObject(tileObject.tileObjectId, tileObject.xPos, tileObject.yPos, WorldManager.manageWorld.rotationMap[tileObject.xPos, tileObject.yPos]);
                    }
                }
            }
            ParticleManager.manage.emitAttackParticle(particlePos, 50);
            ParticleManager.manage.emitRedAttackParticle(particlePos, 25);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos, 50);
            yield return new WaitForSeconds(0.05f);
            ParticleManager.manage.emitAttackParticle(particlePos + Vector3.left * 2f, 25);
            ParticleManager.manage.emitAttackParticle(particlePos + Vector3.right * 2f, 25);
            ParticleManager.manage.emitAttackParticle(particlePos + Vector3.forward * 2f, 25);
            ParticleManager.manage.emitAttackParticle(particlePos + Vector3.back * 2f, 25);
            ParticleManager.manage.emitRedAttackParticle(particlePos + Vector3.left * 2f, 25);
            ParticleManager.manage.emitRedAttackParticle(particlePos + Vector3.right * 2f, 25);
            ParticleManager.manage.emitRedAttackParticle(particlePos + Vector3.forward * 2f, 25);
            ParticleManager.manage.emitRedAttackParticle(particlePos + Vector3.back * 2f, 25);

            int radius = _explosionRadius;

            if (baseIsServer(instance))
            {
                baseStartCoroutine(instance, blowUpPosCoroutine(instance, radius, xPos, yPos, initialHeight));
                // for (int i = -radius; i <= radius; i++)
                // {
                //     for (int j = -radius; j <= radius; j++)
                //     {
                //         if (i == 0 && j == 0)
                //             continue;

                //         blowUpPos(instance, xPos, yPos, i, j, initialHeight);
                //     }
                // }
            }
            yield return new WaitForSeconds(0.05f);
            instance.hideOnExplode.gameObject.SetActive(false);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.left * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.right * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.forward * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.back * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.left * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.right * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.forward * 2f, 5);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.back * 2f, 5);
            if (baseIsServer(instance))
            {
                yield return new WaitForSeconds(0.5f);
                NetworkServer.Destroy(baseGameObject(instance));
            }
            yield break;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Component), "transform", MethodType.Getter)]
        public static Transform Transform(object instance)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Component), nameof(Component.gameObject), MethodType.Getter)]
        public static GameObject baseGameObject(object instance)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), typeof(IEnumerator))]
        public static Coroutine baseStartCoroutine(object instance, IEnumerator routine)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), typeof(String))]
        public static Coroutine baseStartCoroutineStr(object instance, String routine)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.isServer), MethodType.Getter)]
        public static bool baseIsServer(object instance)
        {
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BombExplodes), "explosionLightFlash")]
        public static IEnumerator explosionLightFlash(object instance)
        {
            return null;
        }
    }

    public enum BombModes
    {
        Vanilla,
        Flowers,
        Silly
    }
}