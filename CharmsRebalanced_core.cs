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
        internal static string version = "1.0.1.5";
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
            public static Dictionary<string, int> PatchesEnabled
            {
                get => _Config.settingsInstance.patchesEnabled;
                set => _Config.settingsInstance.patchesEnabled = value;
            }
            public static class _Config
            {
                public class GlobalSettings
                {
                    public int ExampleOption { get; set; } = 0;
                    public Dictionary<string, int> patchesEnabled = new()
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
            var root = new Modmenus.ModMenuScreenBuilder(ModDisplayName, modListMenu);

            // Main toggle
            root.AddHorizontalOption(new IMenuMod.MenuEntry
            {
                Name = "Enabled",
                Description = "Whether the mod is enabled or not.",
                Values = new[] { "True", "False" },
                Saver = v => toggleDelegates?.SetModEnabled(v == 0),
                Loader = () => toggleDelegates?.GetModEnabled() == true ? 0 : 1
            });

            // Example option
            root.AddHorizontalOption(new IMenuMod.MenuEntry
            {
                Name = "Example Option",
                Description = "Example global toggle.",
                Values = new[] { "True", "False" },
                Saver = v => Config._Config.settingsInstance.ExampleOption = v,
                Loader = () => Config._Config.settingsInstance.ExampleOption
            });

            // Patches Enabled submenu
            var patchMenu = BuildPatchesMenu(root.menuBuilder.Screen);
            root.AddSubpage("Patches Enabled", "Enable or disable individual charm patches.", patchMenu);

            return root.CreateMenuScreen();
        }
        private MenuScreen BuildPatchesMenu(MenuScreen parent)
        {
            var b = new Modmenus.ModMenuScreenBuilder("Patches Enabled", parent);

            b.AddSubpage("Charm Row 1", "Patches for the first row of charms.",
                BuildRow1(b.menuBuilder.Screen));

            b.AddSubpage("Charm Row 2", "Patches for the second row of charms.",
                BuildRow2(b.menuBuilder.Screen));

            b.AddSubpage("Charm Row 3", "Patches for the third row of charms.",
                BuildRow3(b.menuBuilder.Screen));

            b.AddSubpage("Charm Row 4", "Patches for the fourth row of charms.",
                BuildRow4(b.menuBuilder.Screen));

            return b.CreateMenuScreen();
        }
        private void AddPatch(Modmenus.ModMenuScreenBuilder b, string title, string key)
        {
            b.AddHorizontalOption(new IMenuMod.MenuEntry
            {
                Name = title,
                Description = "Enable or disable this charm patch.",
                Values = new[] { "Disabled", "Enabled" },

                Loader = () =>
                {
                    if (!Config._Config.settingsInstance.patchesEnabled.TryGetValue(key, out int v))
                        return 0;
                    return v;
                },

                Saver = v =>
                {
                    Config._Config.settingsInstance.patchesEnabled[key] = v;
                }
            });
        }
        private MenuScreen BuildRow1(MenuScreen parent)
        {
            var b = new Modmenus.ModMenuScreenBuilder("Charm Row 1", parent);

            AddPatch(b, "Gathering Swarm Patch", "swarm");
            AddPatch(b, "Wayward Compass Patch", "compass");
            AddPatch(b, "Stalwart Shell Patch", "stalwart");
            AddPatch(b, "Soul Catcher Patch", "soul_catcher");
            AddPatch(b, "Shaman Stone Patch", "shaman_stone");
            AddPatch(b, "Soul Eater Patch", "soul_eater");
            AddPatch(b, "Dashmaster Patch", "dashmaster");
            AddPatch(b, "Sprintmaster Patch", "sprintmaster");
            AddPatch(b, "Grubsong Patch", "grubsong");
            AddPatch(b, "Grubberfly's Elegy Patch", "grubberflys_elegy");

            return b.CreateMenuScreen();
        }
        private MenuScreen BuildRow2(MenuScreen parent)
        {
            var b = new Modmenus.ModMenuScreenBuilder("Charm Row 2", parent);

            AddPatch(b, "Fragile/Unbreakable Heart Patch", "fragile_unbreakable_heart");
            AddPatch(b, "Fragile/Unbreakable Greed Patch", "fragile_unbreakable_greed");
            AddPatch(b, "Fragile/Unbreakable Strength Patch", "fragile_unbreakable_strength");
            AddPatch(b, "Spell Twister Patch", "spell_twister");
            AddPatch(b, "Steady Body Patch", "steady_body");
            AddPatch(b, "Heavy Blow Patch", "heavy_blow");
            AddPatch(b, "Quick Slash Patch", "quick_slash");
            AddPatch(b, "Longnail Patch", "longnail");
            AddPatch(b, "Mark of Pride Patch", "mark_of_pride");
            AddPatch(b, "Fury of the Fallen Patch", "fury_of_the_fallen");

            return b.CreateMenuScreen();
        }
        private MenuScreen BuildRow3(MenuScreen parent)
        {
            var b = new Modmenus.ModMenuScreenBuilder("Charm Row 3", parent);

            AddPatch(b, "Thorns of Agony Patch", "thorns_of_agony");
            AddPatch(b, "Baldur Shell Patch", "baldur_shell");
            AddPatch(b, "Flukenest Patch", "flukenest");
            AddPatch(b, "Defender's Crest Patch", "defenders_crest");
            AddPatch(b, "Glowing Womb Patch", "glowing_womb");
            AddPatch(b, "Quick Focus Patch", "quick_focus");
            AddPatch(b, "Deep Focus Patch", "deep_focus");
            AddPatch(b, "Lifeblood Heart Patch", "lifeblood_heart");
            AddPatch(b, "Lifeblood Core Patch", "lifeblood_core");
            AddPatch(b, "Joni's Blessing Patch", "jonis_blessing");

            return b.CreateMenuScreen();
        }
        private MenuScreen BuildRow4(MenuScreen parent)
        {
            var b = new Modmenus.ModMenuScreenBuilder("Charm Row 4", parent);

            AddPatch(b, "Hiveblood Patch", "hiveblood");
            AddPatch(b, "Spore Shroom Patch", "spore_shroom");
            AddPatch(b, "Sharp Shadow Patch", "sharp_shadow");
            AddPatch(b, "Shape of Unn Patch", "shape_of_unn");
            AddPatch(b, "Nailmaster's Glory Patch", "nailmasters_glory");
            AddPatch(b, "Weaversong Patch", "weaversong");
            AddPatch(b, "Dream Wielder Patch", "dream_wielder");
            AddPatch(b, "Grimmchild Patch", "grimmchild");
            AddPatch(b, "Carefree Melody Patch", "carefree_melody");
            AddPatch(b, "Kingsoul Patch", "kingsoul");
            AddPatch(b, "Voidsoul Patch", "voidsoul");

            return b.CreateMenuScreen();
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