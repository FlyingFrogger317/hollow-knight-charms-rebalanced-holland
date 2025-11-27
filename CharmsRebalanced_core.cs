using Modding;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
namespace CharmsRebalanced
{
    public class CharmsRebalanced : Mod, ITogglableMod, ICustomMenuMod, IGlobalSettings<CharmsRebalanced.Config._Config.GlobalSettings>, ILocalSettings<CharmsRebalanced.SaveModSettings>
    {
        internal static CharmsRebalanced Instance;
        internal static string ModDisplayName = "Charms Rebalanced";
        internal static string version = "1.0.0.8";
        public CharmsRebalanced() : base(ModDisplayName) { }
        public override string GetVersion()
        {
            return version;
        }
        private static class UsedHooks
        {
            private static Dictionary<string, IList> hooks = new();
            public static T RegisterHook<T>(string hook, T del)
            {
                if (!hooks.ContainsKey(hook))
                {
                    hooks[hook] = new List<T>();
                }
                hooks[hook].Add(del);
                return del;
            }
            public static void UnregisterHooks(string hook)
            {
                if (hooks.ContainsKey(hook))
                {
                    foreach (var del in hooks[hook])
                    {
                        switch (hook)
                        {
                            case "GetPlayerInt":
                                ModHooks.GetPlayerIntHook -= (Modding.Delegates.GetIntProxy)del;
                                break;
                            case "CharmUpdate":
                                ModHooks.CharmUpdateHook -= (Modding.Delegates.CharmUpdateHandler)del;
                                break;
                            case "SoulGain":
                                ModHooks.SoulGainHook -= (Func<int, int>)del;
                                break;
                        }
                    }
                    hooks[hook].Clear();
                }
            }
            public static void UnregisterAllHooks()
            {
                foreach (var hook in hooks.Keys)
                {
                    UnregisterHooks(hook);
                }
            }
        }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;
            CharmMods.Init();
            ModHooks.GetPlayerIntHook += UsedHooks.RegisterHook<Modding.Delegates.GetIntProxy>("GetPlayerInt", OnPlayerDataGetInt);
            ModHooks.CharmUpdateHook += UsedHooks.RegisterHook<Modding.Delegates.CharmUpdateHandler>("CharmUpdate", (PlayerData data, HeroController controller) =>
            {
                RunHandlers(UsableHook.CharmUpdate, data, controller);
            });
            ModHooks.SoulGainHook += UsedHooks.RegisterHook<Func<int, int>>("SoulGain", soul =>
            {
                int? retVal = RunHandlers<int?>(UsableHook.SoulGain, soul);
                return retVal ?? soul;
            });
            ModHooks.OnReceiveDeathEventHook += (EnemyDeathEffects enemyDeathEffects, bool eventAlreadyReceived, ref float? attackDirection, ref bool resetDeathEvent, ref bool spellBurn, ref bool isWatery) =>
            {
                Log(enemyDeathEffects.gameObject.name);
            };
            ILHooks.EnableAll();
            Log("Loaded");
        }
        public void Unload()
        {
            UsedHooks.UnregisterAllHooks();
            ILHooks.DisableAll();
            Log("Unloaded");
        }
        public class SaveModSettings
        {
            public bool radDead = false;
        }
        SaveModSettings saveSettings = new SaveModSettings();
        public void OnLoadLocal(SaveModSettings s)
        {
            saveSettings = s;
        }
        public SaveModSettings OnSaveLocal()
        {
            return saveSettings;
        }
        public static class Config
        {
            public static bool ExampleOption
            {
                get
                {
                    return _Config.settingsInstance.ExampleOption == 0;
                }
            }
            public static class _Config
            {
                public class GlobalSettings
                {
                    public int ExampleOption { get; set; } = 0;
                    public Dictionary<string, bool> patchesEnabled = new()
                    {

                    };
                }
                static public GlobalSettings settingsInstance = new GlobalSettings();
                static List<(string, string, string[], string)> options = new()
                {
                    ("Example Option", "An example configuration option.", new string[] { "True", "False" }, "ExampleOption")
                };
                static public void CreateEntries(Modmenus.ModMenuScreenBuilder builder)
                {
                    var settings = typeof(GlobalSettings);
                    foreach (var (name, description, values, id) in options)
                    {
                        builder.AddHorizontalOption(new IMenuMod.MenuEntry
                        {
                            Name = name,
                            Description = description,
                            Values = values,
                            Saver = (int val) =>
                            {
                                Instance.Log("Setting " + id + " to " + val);
                                settings.GetProperty(id).SetValue(settingsInstance, val);
                            },
                            Loader = () =>
                            {
                                return (int)settings.GetProperty(id).GetValue(settingsInstance);
                            }
                        });
                    }
                }
            }
        }
        public void OnLoadGlobal(CharmsRebalanced.Config._Config.GlobalSettings s)
        {
            CharmsRebalanced.Config._Config.settingsInstance = s;
        }
        public CharmsRebalanced.Config._Config.GlobalSettings OnSaveGlobal()
        {
            return CharmsRebalanced.Config._Config.settingsInstance;
        }
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
        {
            Modmenus.ModMenuScreenBuilder builder = new Modmenus.ModMenuScreenBuilder(ModDisplayName, modListMenu);
            builder.AddHorizontalOption(new IMenuMod.MenuEntry
            {
                Name = "Enabled",
                Description = "Whether the mod is enabled or not.",
                Values = new string[] { "True", "False" },
                Saver = (int val) => { toggleDelegates?.SetModEnabled(val == 0); },
                Loader = () => (bool)(toggleDelegates?.GetModEnabled()) ? 0 : 1
            });
            Config._Config.CreateEntries(builder);
            return builder.CreateMenuScreen();
        }
        public bool ToggleButtonInsideMenu => true;
        private int OnPlayerDataGetInt(string field, int orig)
        {
            if (field.StartsWith("charmCost_"))
            {
                int.TryParse(field.Substring(10), out int charmId);
                CharmUtils.CharmData charmData = CharmUtils.GetCharm(charmId);

                if (charmData.costChange.Equals(0)) return orig;
                int newCharmCost = orig + charmData.costChange;
                return newCharmCost;
            }
            return orig;
        }
        //handle registerable callbacks
#nullable enable
        public delegate object? CharmHandler(object[] args);
        private void RunHandlers(UsableHook hook, params object[] args)
        {
            RunHandlers<object?>(hook, args);
        }
        private T? RunHandlers<T>(UsableHook hook, params object[] args)
        {
            HandlerList handlable = RegisteredHandlers[hook];
            T? returnValue = default;
            foreach (var (charms, fn) in handlable)
            {
                int charmsNeeded = charms.Length;
                CharmUtils.CharmData[] equippedCharms = CharmUtils.GetCharmsIfEquippedOrNot(charms);
                if (equippedCharms.Length == charmsNeeded)
                {
                    var output = fn(args);
                    if (output is T t)
                    {
                        returnValue = t;
                    }
                }
            }
            return returnValue;
        }
        public enum UsableHook
        {
            CharmUpdate,
            SoulGain
        }
        private class HandlerList : List<(string[], CharmHandler)> { };
        private Dictionary<UsableHook, HandlerList> RegisteredHandlers = Enum.GetValues(typeof(UsableHook)).Cast<UsableHook>().ToDictionary(hook => hook, hook => new HandlerList());
        public void RegisterCharmHandler(UsableHook hook, string charm, CharmHandler handler)
        {
            RegisterCharmHandler(hook, [charm], handler);
        }
        public void RegisterCharmHandler(UsableHook hook, string[] charms, CharmHandler handler)
        {
            RegisteredHandlers[hook].Add((charms, handler));
        }
#nullable disable
    }
}