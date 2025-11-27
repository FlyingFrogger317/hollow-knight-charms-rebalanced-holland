using Modding;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using HutongGames.PlayMaker.Actions;
namespace CharmsRebalanced
{
    public class CharmsRebalanced : Mod, ITogglableMod, ICustomMenuMod, IGlobalSettings<CharmsRebalanced.Config._Config.GlobalSettings>, ILocalSettings<CharmsRebalanced.SaveModSettings>
    {
        internal static CharmsRebalanced Instance;
        internal static string ModDisplayName = "Charms Rebalanced";
        internal static string version = "1.0.0.9";
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
                if (enemyDeathEffects.gameObject.name.Contains("Radiance") && saveSettings.radDead == false)
                {
                    saveSettings.radDead = true;
                }
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
        public SaveModSettings saveSettings = new SaveModSettings();
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
                    public Dictionary<string, Int32> patchesEnabled = new()
                    {
                        { "swarm", 1 },
                        { "compass", 1 },
                        { "grubsong", 1 },
                        { "stalwart", 1 },
                        { "baldur", 1 },
                        { "fury", 1 },
                        { "quick_focus", 1 },
                        { "lifeblood_heart", 1 },
                        { "lifeblood_core", 1 },
                        { "crest", 1 },
                        { "flukenest", 1 },
                        { "thorns", 1 },
                        { "mark_of_pride", 1 },
                        { "steady_body", 1 },
                        { "heavy_blow", 1 },
                        { "sharp_shadow", 1 },
                        { "spore_shroom", 1 },
                        { "longnail", 1 },
                        { "shaman_stone", 1 },
                        { "soul_catcher", 1 },
                        { "soul_eater", 1 },
                        { "glowing_womb", 1 },
                        { "fragile_heart", 1 },
                        { "fragile_greed", 1 },
                        { "fragile_strength", 1 },
                        { "nailmasters_glory", 1 },
                        { "jonis_blessing", 1 },
                        { "shape_of_unn", 1 },
                        { "hiveblood", 1 },
                        { "dream_wielder", 1 },
                        { "dashmaster", 1 },
                        { "quick_slash", 1 },
                        { "spell_twister", 1 },
                        { "deep_focus", 1 },
                        { "grubberflys_elegy", 1 },
                        { "kingsoul", 1 },
                        { "sprintmaster", 1 },
                        { "dreamshield", 1 },
                        { "weaversong", 1 },
                        { "grimmchild", 1 },
                        { "voidsoul", 1 }
                    };
                }
                static public GlobalSettings settingsInstance = new GlobalSettings();
                // static List<(string, string, string[], string)> options = new()
                // {
                //     ("Example Option", "An example configuration option.", new string[] { "True", "False" }, "ExampleOption")
                // };
                public abstract class ScreenItem { }
                public class Submenu : ScreenItem
                {
                    public string title;
                    public string description;
                    public List<ScreenItem> items = new();
                }
                public class Option : ScreenItem
                {
                    public string title;
                    public string description;
                    public string[] values;
                    public string[] id;
                }
                public static List<ScreenItem> screenItems = new()
                {
                    new Option {
                        title = "Example Option",
                        description = "An example configuration option.",
                        values = new string[] { "True", "False" },
                        id = ["ExampleOption"]
                    },
                    new Submenu {
                        title = "Patches Enabled",
                        description = "Enable or disable individual charm patches.",
                        items = new List<ScreenItem> {
                            new Submenu {
                                title = "Charm Row 1",
                                description = "Patches for the first row of charms.",
                                items = new List<ScreenItem> {
                                    new Option {
                                        title = "Gathering Swarm Patch",
                                        description = "Enable or disable the Gathering Swarm charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "swarm"]
                                    },
                                    new Option {
                                        title = "Wayward Compass Patch",
                                        description = "Enable or disable the Wayward Compass charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "compass"]
                                    },
                                    new Option {
                                        title = "Stalwart Shell Patch",
                                        description = "Enable or disable the Stalwart Shell charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "stalwart"]
                                    },
                                    new Option {
                                        title = "Soul Catcher Patch",
                                        description = "Enable or disable the Soul Catcher charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "soul_catcher"]
                                    },
                                    new Option {
                                        title = "Shaman Stone Patch",
                                        description = "Enable or disable the Shaman Stone charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "shaman_stone"]
                                    },
                                    new Option {
                                        title = "Soul Eater Patch",
                                        description = "Enable or disable the Soul Eater charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "soul_eater"]
                                    },
                                    new Option {
                                        title = "Dashmaster Patch",
                                        description = "Enable or disable the Dashmaster charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "dashmaster"]
                                    },
                                    new Option {
                                        title = "Sprintmaster Patch",
                                        description = "Enable or disable the Sprintmaster charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "sprintmaster"]
                                    },
                                    new Option {
                                        title = "Grubsong Patch",
                                        description = "Enable or disable the Grubsong charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "grubsong"]
                                    },
                                    new Option {
                                        title = "Grubberfly's Elegy Patch",
                                        description = "Enable or disable the Grubberfly's Elegy charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "grubberflys_elegy"]
                                    }
                                }
                            },

                            new Submenu {
                                title = "Charm Row 2",
                                description = "Patches for the second row of charms.",
                                items = new List<ScreenItem> {
                                    new Option {
                                        title = "Fragile/Unbreakable Heart Patch",
                                        description = "Enable or disable the Fragile/Unbreakable Heart charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "fragile_unbreakable_heart"]
                                    },
                                    new Option {
                                        title = "Fragile/Unbreakable Greed Patch",
                                        description = "Enable or disable the Fragile/Unbreakable Greed charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "fragile_unbreakable_greed"]
                                    },
                                    new Option {
                                        title = "Fragile/Unbreakable Strength Patch",
                                        description = "Enable or disable the Fragile/Unbreakable Strength charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "fragile_unbreakable_strength"]
                                    },
                                    new Option {
                                        title = "Spell Twister Patch",
                                        description = "Enable or disable the Spell Twister charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "spell_twister"]
                                    },
                                    new Option {
                                        title = "Steady Body Patch",
                                        description = "Enable or disable the Steady Body charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "steady_body"]
                                    },
                                    new Option {
                                        title = "Heavy Blow Patch",
                                        description = "Enable or disable the Heavy Blow charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "heavy_blow"]
                                    },
                                    new Option {
                                        title = "Quick Slash Patch",
                                        description = "Enable or disable the Quick Slash charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "quick_slash"]
                                    },
                                    new Option {
                                        title = "Longnail Patch",
                                        description = "Enable or disable the Longnail charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "longnail"]
                                    },
                                    new Option {
                                        title = "Mark of Pride Patch",
                                        description = "Enable or disable the Mark of Pride charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "mark_of_pride"]
                                    },
                                    new Option {
                                        title = "Fury of the Fallen Patch",
                                        description = "Enable or disable the Fury of the Fallen charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "fury_of_the_fallen"]
                                    }
                                }
                            },

                            new Submenu {
                                title = "Charm Row 3",
                                description = "Patches for the third row of charms.",
                                items = new List<ScreenItem> {
                                    new Option {
                                        title = "Thorns of Agony Patch",
                                        description = "Enable or disable the Thorns of Agony charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "thorns_of_agony"]
                                    },
                                    new Option {
                                        title = "Baldur Shell Patch",
                                        description = "Enable or disable the Baldur Shell charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "baldur_shell"]
                                    },
                                    new Option {
                                        title = "Flukenest Patch",
                                        description = "Enable or disable the Flukenest charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "flukenest"]
                                    },
                                    new Option {
                                        title = "Defender's Crest Patch",
                                        description = "Enable or disable the Defender's Crest charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "defenders_crest"]
                                    },
                                    new Option {
                                        title = "Glowing Womb Patch",
                                        description = "Enable or disable the Glowing Womb charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "glowing_womb"]
                                    },
                                    new Option {
                                        title = "Quick Focus Patch",
                                        description = "Enable or disable the Quick Focus charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "quick_focus"]
                                    },
                                    new Option {
                                        title = "Deep Focus Patch",
                                        description = "Enable or disable the Deep Focus charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "deep_focus"]
                                    },
                                    new Option {
                                        title = "Lifeblood Heart Patch",
                                        description = "Enable or disable the Lifeblood Heart charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "lifeblood_heart"]
                                    },
                                    new Option {
                                        title = "Lifeblood Core Patch",
                                        description = "Enable or disable the Lifeblood Core charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "lifeblood_core"]
                                    },
                                    new Option {
                                        title = "Joni's Blessing Patch",
                                        description = "Enable or disable the Joni's Blessing charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "jonis_blessing"]
                                    }
                                }
                            },

                            new Submenu {
                                title = "Charm Row 4",
                                description = "Patches for the fourth row of charms.",
                                items = new List<ScreenItem> {
                                    new Option {
                                        title = "Hiveblood Patch",
                                        description = "Enable or disable the Hiveblood charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "hiveblood"]
                                    },
                                    new Option {
                                        title = "Spore Shroom Patch",
                                        description = "Enable or disable the Spore Shroom charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "spore_shroom"]
                                    },
                                    new Option {
                                        title = "Sharp Shadow Patch",
                                        description = "Enable or disable the Sharp Shadow charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "sharp_shadow"]
                                    },
                                    new Option {
                                        title = "Shape of Unn Patch",
                                        description = "Enable or disable the Shape of Unn charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "shape_of_unn"]
                                    },
                                    new Option {
                                        title = "Nailmaster's Glory Patch",
                                        description = "Enable or disable the Nailmaster's Glory charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "nailmasters_glory"]
                                    },
                                    new Option {
                                        title = "Weaversong Patch",
                                        description = "Enable or disable the Weaversong charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "weaversong"]
                                    },
                                    new Option {
                                        title = "Dream Wielder Patch",
                                        description = "Enable or disable the Dream Wielder charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "dream_wielder"]
                                    },
                                    new Option {
                                        title = "Grimmchild Patch",
                                        description = "Enable or disable the Grimmchild charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "grimmchild"]
                                    },
                                    new Option {
                                        title = "Carefree Melody Patch",
                                        description = "Enable or disable the Carefree Melody charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "carefree_melody"]
                                    },
                                    new Option {
                                        title = "Kingsoul Patch",
                                        description = "Enable or disable the Kingsoul charm patch.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "kingsoul"]
                                    },
                                    new Option {
                                        title = "Voidsoul",
                                        description = "Enable or disable the Voidsoul charm.",
                                        values = new string[] { "Disabled", "Enabled" },
                                        id = ["patchesEnabled", "voidsoul"]
                                    }
                                }
                            }
                        }
                    }
                };
                public static MenuScreen CreateEntriesAndBuild(Modmenus.ModMenuScreenBuilder rootBuilder)
                {
                    Dictionary<Submenu, Modmenus.ModMenuScreenBuilder> builders = new();
                    Dictionary<Submenu, MenuScreen> screens = new();

                    Queue<(Modmenus.ModMenuScreenBuilder parentBuilder, List<ScreenItem> items, MenuScreen parentScreen)> q =
                        new Queue<(Modmenus.ModMenuScreenBuilder, List<ScreenItem>, MenuScreen)>();

                    q.Enqueue((rootBuilder, screenItems, rootBuilder.menuBuilder.Screen));

                    while (q.Count > 0)
                    {
                        var (currentBuilder, items, parentScreen) = q.Dequeue();

                        foreach (var item in items)
                        {
                            if (item is Option opt)
                            {
                                currentBuilder.AddHorizontalOption(new IMenuMod.MenuEntry
                                {
                                    Name = opt.title,
                                    Description = opt.description,
                                    Values = opt.values,
                                    Loader = () =>
                                    {
                                        string[] path = opt.id.ToArray();
                                        object obj = settingsInstance;
                                        Type type = obj.GetType();
                                        string leaf = path[path.Length - 1];
                                        for (int i = 0; i < path.Length - 1; i++)
                                        {
                                            var p = type.GetProperty(path[i]);
                                            obj = p.GetValue(obj);
                                            type = obj.GetType();
                                        }
                                        return (int)type.GetProperty(leaf).GetValue(obj);
                                    },
                                    Saver = (int index) =>
                                    {
                                        string[] path = opt.id.ToArray();
                                        object obj = settingsInstance;
                                        Type type = obj.GetType();
                                        string leaf = path[path.Length - 1];
                                        for (int i = 0; i < path.Length - 1; i++)
                                        {
                                            var p = type.GetProperty(path[i]);
                                            obj = p.GetValue(obj);
                                            type = obj.GetType();
                                        }
                                        type.GetProperty(leaf).SetValue(obj, index);
                                    }
                                });
                            }
                            else if (item is Submenu sub)
                            {
                                var subBuilder = new Modmenus.ModMenuScreenBuilder(sub.title, null);
                                builders[sub] = subBuilder;

                                currentBuilder.AddButton(sub.title, sub.description, () =>
                                {
                                    UIManager.instance.UIGoToDynamicMenu(screens[sub]);
                                });

                                q.Enqueue((subBuilder, sub.items, null));
                            }
                        }
                    }

                    MenuScreen rootScreen = rootBuilder.CreateMenuScreen();

                    Queue<(Submenu sub, MenuScreen parent)> q2 = new();
                    foreach (var kv in builders)
                        q2.Enqueue((kv.Key, rootScreen));

                    while (q2.Count > 0)
                    {
                        var (sub, parent) = q2.Dequeue();
                        var subBuilder = builders[sub];
                        subBuilder.returnScreen = parent;
                        var screen = subBuilder.CreateMenuScreen();
                        screens[sub] = screen;

                        foreach (var child in sub.items)
                            if (child is Submenu childSub)
                                q2.Enqueue((childSub, screen));
                    }

                    return rootScreen;
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
            return Config._Config.CreateEntriesAndBuild(builder);
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