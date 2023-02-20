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
    private ConfigEntry<KeyCode> _modeSwapKey;
    private ConfigEntry<KeyCode> _stateSwapKey;
    private ConfigEntry<KeyCode> _radiusUpKey;
    private ConfigEntry<KeyCode> _radiusDownKey;
    private ConfigEntry<KeyCode> _keyModifier;
    private ConfigEntry<bool> _isInifiniteMode;
    private static int NexusID = 184;

    public Plugin()
    {
        _isInifiniteMode = Config.Bind<bool>("Main", "Infinite Bombs", false, "Remember Uncle Ben's words.");
        _modeSwapKey = Config.Bind<KeyCode>("Controls", "Switch Modes Key", KeyCode.B, "Key to switch between bomb modes.");
        _stateSwapKey = Config.Bind<KeyCode>("Controls", "Switch States Key", KeyCode.Mouse2, "Key to switch between bomb states.");
        _radiusUpKey = Config.Bind<KeyCode>("Controls", "Increase Radius Key", KeyCode.KeypadPlus, "Key to increase the radius of bombs.");
        _radiusDownKey = Config.Bind<KeyCode>("Controls", "Decrease Radius Key", KeyCode.KeypadMinus, "Key to decrease the radius of bombs.");
        _keyModifier = Config.Bind<KeyCode>("Controls", "Bombs Away Key Modifier", KeyCode.LeftShift, "Optional modifier for all the keys above.");
        Config.Bind<int>("Other", "NexusID", NexusID);
    }

    private void Awake()
    {
        _harmony = Harmony.CreateAndPatchAll(typeof(BombExplodesPatch), "fururikawa.BombsAway");

        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Logger.LogInfo($"Switch Bomb key set to: {_keyModifier.Value} + {_modeSwapKey.Value.ToString()}");
    }

    private void Start()
    {
        RegisterAllModes();
        BombManager.Instance.IsInfiniteBombs = _isInifiniteMode.Value;
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
                if ((_keyModifier.Value == KeyCode.None || Input.GetKey(_keyModifier.Value)) && Input.GetKeyDown(_radiusUpKey.Value))
                {
                    if (BombManager.Instance.Radius < 5)
                        BombManager.Instance.SetRadius((uint)BombManager.Instance.Radius + 1);

                    NotificationManager.manage.createChatNotification("Bomb radius set to " + BombManager.Instance.Radius + "!");
                    SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
                }
                else if ((_keyModifier.Value == KeyCode.None || Input.GetKey(_keyModifier.Value)) && Input.GetKeyDown(_radiusDownKey.Value))
                {
                    if (BombManager.Instance.Radius > 1)
                        BombManager.Instance.SetRadius((uint)BombManager.Instance.Radius - 1);

                    NotificationManager.manage.createChatNotification("Bomb radius set to " + BombManager.Instance.Radius + "!");
                    SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
                }

                if ((_keyModifier.Value == KeyCode.None || Input.GetKey(_keyModifier.Value)) && Input.GetKeyDown(_stateSwapKey.Value))
                {
                    BombManager.Instance.ToggleBombState();

                    string notification = "";
                    switch (BombManager.Instance.BombState)
                    {
                        case 0:
                            notification = "Bomb set to make holes!";
                            break;
                        case 1:
                            notification = "Bomb set to make hills!";
                            break;
                        case 2:
                            notification = "Bomb set to level ground!";
                            break;
                        case 3:
                            notification = "Bomb set to safe mode!";
                            break;
                    }

                    NotificationManager.manage.createChatNotification(notification, false);
                    SoundManager.manage.play2DSound(SoundManager.manage.signTalk);
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
