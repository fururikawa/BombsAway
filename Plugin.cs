using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Mirror;
using UnityEngine;


namespace BombsAway;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private Harmony _harmony;
    private ConfigEntry<int> _explosionRadius;
    private ConfigEntry<KeyCode> _modeSwapKey;
    private ConfigEntry<KeyCode> _keyModifier;
    private ConfigEntry<bool> _isInifiniteMode;

    public Plugin()
    {
        _explosionRadius = Config.Bind<int>("Main", "Explosion Radius", 2, "How many blocks away from center are affected by the explosion. Min (Vanilla): 1, Max: 5.");
        _isInifiniteMode = Config.Bind<bool>("Main", "Infinite Bombs", false, "Remember Uncle Ben's words.");
        _modeSwapKey = Config.Bind<KeyCode>("Controls", "Switch Modes Key", KeyCode.B, "Key to switch between bomb modes.");
        _keyModifier = Config.Bind<KeyCode>("Controls", "Switch Modes Key Modifier", KeyCode.LeftShift, "Optional modifier for the key above.");
    }
    private void Awake()
    {
        BombExplodesHelper.ExplosionRadius = Mathf.Clamp(_explosionRadius.Value, 1, 5);
        BombExplodesHelper.IsInfiniteBombs = _isInifiniteMode.Value;
        _harmony = Harmony.CreateAndPatchAll(typeof(BombExplodesPatch), "fururikawa.BombsAway");

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo($"Switch Bomb key set to: {_keyModifier.Value} + {_modeSwapKey.Value.ToString()}");
    }

    private void Start()
    {
        BombExplodesHelper.ComputeExplosionGrid();
    }

    private void Update()
    {
        CharMovement localPlayer = NetworkMapSharer.share.localChar;
        if (localPlayer == null)
            return;

        if (!localPlayer.myEquip.isInVehicle() &&
            !StatusManager.manage.dead &&
            !localPlayer.myPickUp.isCarryingSomething())
        {
            InventorySlot slot = Inventory.inv.invSlots[Inventory.inv.selectedSlot];

            if (slot.itemInSlot == Inventory.inv.allItems[277])
            {
                if (InputMaster.input.Interact())
                {
                    if (!localPlayer.myPickUp.pickUp() &&
                        !localPlayer.myInteract.tileInteract((int)localPlayer.myInteract.selectedTile.x, (int)localPlayer.myInteract.selectedTile.y))
                    {
                        BombExplodesHelper.InvertBombState();
                        String notification = BombExplodesHelper.IsInverted ? "Bomb set to make hills!" : "Bomb set to make holes!";
                        NotificationManager.manage.createChatNotification(notification, false);
                        SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
                    }
                }

                if ((_keyModifier.Value == KeyCode.None || Input.GetKey(_keyModifier.Value)) && Input.GetKeyDown(_modeSwapKey.Value))
                {
                    BombExplodesHelper.CycleBombMode();
                    NotificationManager.manage.createChatNotification($"{BombExplodesHelper.GetBombModeActive()} Mode now in effect!", true);
                    SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
                }
            }
        }
    }

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }
}
