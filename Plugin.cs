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
    private bool _isInstanced = false;
    private ConfigEntry<int> _explosionRadius;
    private ConfigEntry<KeyCode> _modeSwapKey;
    private ConfigEntry<KeyCode> _keyModifier;

    public Plugin()
    {
        _explosionRadius = Config.Bind<int>("Main", "Intensity", 3, "How many blocks away from the bomb will be affected. Vanilla is 1, Max is 3 due to lag issues.");
        _modeSwapKey = Config.Bind<KeyCode>("Main", "Swap Modes Key", KeyCode.B, "");
        _keyModifier = Config.Bind<KeyCode>("Main", "Swap Modes Key Modifier", KeyCode.LeftShift, "");
    }
    private void Awake()
    {
        BombExplodesPatch._explosionRadius = _explosionRadius.Value;
        _harmony = Harmony.CreateAndPatchAll(typeof(BombExplodesPatch), "fururikawa.BombsAway");

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo($"Sawp Bomb key set to: {_modeSwapKey.Value.ToString()}");
    }

    private void Start()
    {
        BombExplodesPatch.calcCoordinates(_explosionRadius.Value);
    }

    private void Update()
    {
        CharMovement localPlayer = NetworkMapSharer.share.localChar;
        if (localPlayer == null)
            return;

        if (!localPlayer.myEquip.isInVehicle() && !localPlayer.myPickUp.isLayingDown() && !localPlayer.myPickUp.sitting)
        {
            InventorySlot slot = Inventory.inv.invSlots[Inventory.inv.selectedSlot];

            if (slot.itemInSlot == Inventory.inv.allItems[277])
            {
                if (InputMaster.input.Interact())
                {
                    BombExplodesPatch._inverted = !BombExplodesPatch._inverted;
                    String notification = BombExplodesPatch._inverted ? "Bomb set to make hills!" : "Bomb set to make holes!";
                    NotificationManager.manage.createChatNotification(notification, false);
                }

                if (Input.GetKey(_keyModifier.Value) && Input.GetKeyDown(_modeSwapKey.Value))
                {
                    BombExplodesPatch.CycleBombMode();
                    NotificationManager.manage.createChatNotification($"{BombExplodesPatch.GetBombModeActive()} Mode now in effect!", true);
                }
            }
        }
    }

    private void OnDestroy()
    {
    }
}
