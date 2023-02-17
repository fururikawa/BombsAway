using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
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
    private static int NexusID = 184;

    public Plugin()
    {
        _explosionRadius = Config.Bind<int>("Main", "Explosion Radius", 2, "How many blocks away from center are affected by the explosion. Min (Vanilla): 1, Max: 5.");
        _isInifiniteMode = Config.Bind<bool>("Main", "Infinite Bombs", false, "Remember Uncle Ben's words.");
        _modeSwapKey = Config.Bind<KeyCode>("Controls", "Switch Modes Key", KeyCode.B, "Key to switch between bomb modes.");
        _keyModifier = Config.Bind<KeyCode>("Controls", "Switch Modes Key Modifier", KeyCode.LeftShift, "Optional modifier for the key above.");
        Config.Bind<int>("Other", "NexusID", NexusID);
    }
    private void Awake()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(BombExplodesPatch), "fururikawa.BombsAway");

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo($"Switch Bomb key set to: {_keyModifier.Value} + {_modeSwapKey.Value.ToString()}");
    }

    private void Start()
    {
        RegisterAllModes();
        BombManager.Instance.IsInfiniteBombs = _isInifiniteMode.Value;
        BombManager.Instance.SetRadius((uint)Mathf.Clamp(_explosionRadius.Value, 1, 5));
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
                        BombManager.Instance.ToggleBombState();
                        String notification = BombManager.Instance.IsInverted ? 
                            "Bomb set to make hills!" : 
                            "Bomb set to make holes!";
                        NotificationManager.manage.createChatNotification(notification, false);
                        SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
                    }
                }

                if ((_keyModifier.Value == KeyCode.None || Input.GetKey(_keyModifier.Value)) && Input.GetKeyDown(_modeSwapKey.Value))
                {
                    BombManager.Instance.CycleBombModes();
                    NotificationManager.manage.createChatNotification($"{BombManager.Instance.GetActiveMode().Name} Mode now in effect!", true);
                    SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
                }
            }
        }
    }

    private void OnDestroy()
    {
        _harmony.UnpatchSelf();
    }

    private void RegisterAllModes()
    {
        BombManager.Instance.Register(new VanillaMode());

        IEnumerable<Type> objectModes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany<Assembly, Type>(x => x.GetTypes())
                    .Where(x => x.IsSubclassOf(typeof(BaseObjectMode)));

        foreach (Type modeType in objectModes)
        {
            if (modeType == typeof(VanillaMode))
                continue;

            BaseObjectMode mode = (BaseObjectMode)Activator.CreateInstance(modeType);
            BombManager.Instance.Register(mode);
            Debug.Log($"Registered {mode.Name}!");
        }
    }
}
