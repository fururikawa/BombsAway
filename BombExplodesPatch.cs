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
    internal static class BombExplodesPatch
    {
        [HarmonyPatch(typeof(CraftingManager), "Start")]
        [HarmonyPostfix]
        private static void SetRecipeAmounts()
        {
            Inventory.inv.allItems[277].craftable.recipeGiveThisAmount = 8;
        }

        [HarmonyPatch(typeof(WorldManager), "Start")]
        [HarmonyPostfix]
        private static void PopulatePossibleIDReferences()
        {
            foreach (var mode in BombManager.Instance.BombModes)
            {
                mode.Setup();
            }
        }

        [HarmonyPatch(typeof(Inventory), "consumeItemInHand")]
        [HarmonyPrefix]
        private static bool ConsumeItemInHandPrefix(Inventory __instance)
        {
            return !(BombManager.Instance.IsInfiniteBombs && __instance.invSlots[__instance.selectedSlot].itemNo == 277);
        }

        [HarmonyPatch(typeof(BombExplodes), "explodeTimer")]
        [HarmonyPrefix]
        private static bool ExplodeTimerPrefix(BombExplodes __instance, ref IEnumerator __result)
        {
            var activeMode = BombManager.Instance.GetActiveMode();
            baseStartCoroutine(__instance, activeMode.GetNextTilesToUse(BombManager.Instance.ExplosionCoordinates.Count()));

            __result = explodeTimer(__instance);

            return false;
        }

        private static void AttackAndDoDamage(Damageable instance, int damageToDeal, Transform attackedBy, float knockBackAmount = 2.5f)
        {
            bool canBeDamaged = (bool)typeof(Damageable).GetField("canBeDamaged", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
            AnimalAI myAnimalAi = (AnimalAI)typeof(Damageable).GetField("myAnimalAi", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
            if (canBeDamaged)
            {
                baseStartCoroutineStr(instance, "canBeDamagedDelay");
                if (knockBackAmount > 0f && instance.myChar)
                {
                    Vector3 knockBackDir = -(attackedBy.position - Transform(instance).position).normalized;
                    knockBackDir.y = 0.75f; // fly me to the moon!
                    instance.myChar.RpcTakeKnockback(knockBackDir, knockBackAmount * 3.5f);
                }
                if (myAnimalAi && attackedBy)
                {
                    myAnimalAi.takeHitAndKnockBack(attackedBy, knockBackAmount);
                }
                if (BombManager.Instance.GetActiveMode().Name == "Vanilla")
                    instance.changeHealth(-Mathf.RoundToInt((float)damageToDeal / instance.defence));
            }
        }

        private static IEnumerator explodeTimer(BombExplodes instance)
        {
            float timeBeforeExplode = 2.1f;
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
                var multiTileObjects = new List<int>();
                BombExplodesHelper.ExplodeTile(instance, xPos, yPos, 0, 0, initialHeight);
                Collider[] array = Physics.OverlapSphere(Transform(instance).position - Vector3.up * 1.5f, 4f, LayerMask.GetMask("Default") | instance.damageLayer);
                for (int i = 0; i < array.Length; i++)
                {
                    Damageable component = array[i].transform.root.GetComponent<Damageable>();
                    if (component)
                    {
                        AttackAndDoDamage(component, 25, Transform(instance), 20f);
                        if (BombManager.Instance.GetActiveMode().Name == "Vanilla")
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

            if (baseIsServer(instance))
            {
                baseStartCoroutine(instance, BombExplodesHelper.StartExplosion(instance, xPos, yPos, initialHeight));
            }
            yield return new WaitForSeconds(0.05f);
            instance.hideOnExplode.gameObject.SetActive(false);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.left * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.right * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.forward * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.back * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.left * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.right * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.forward * 2f, 10);
            ParticleManager.manage.emitParticleAtPosition(ParticleManager.manage.allParts[3], particlePos + Vector3.back * 2f, 10);
            if (baseIsServer(instance))
            {
                yield return new WaitForSeconds(0.5f);
                NetworkServer.Destroy(baseGameObject(instance));
            }
            yield break;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Component), "transform", MethodType.Getter)]
        private static Transform Transform(object instance)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Component), nameof(Component.gameObject), MethodType.Getter)]
        private static GameObject baseGameObject(object instance)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), typeof(IEnumerator))]
        private static Coroutine baseStartCoroutine(object instance, IEnumerator routine)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(MonoBehaviour), nameof(MonoBehaviour.StartCoroutine), typeof(String))]
        private static Coroutine baseStartCoroutineStr(object instance, String routine)
        {
            return null;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.isServer), MethodType.Getter)]
        private static bool baseIsServer(object instance)
        {
            return false;
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(BombExplodes), "explosionLightFlash")]
        private static IEnumerator explosionLightFlash(object instance)
        {
            return null;
        }
    }
}