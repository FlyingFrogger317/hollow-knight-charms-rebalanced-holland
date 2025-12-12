using Modding;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
namespace CharmsRebalanced
{
    public class CharmsRebalanced : Mod, ITogglableMod, ICustomMenuMod, IGlobalSettings<CharmsRebalanced.Config._Config.GlobalSettings>, ILocalSettings<CharmsRebalanced.SaveModSettings>
    {
        internal static CharmsRebalanced Instance;
        internal static string ModDisplayName = "Charms Rebalanced";
        internal static string version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        public CharmsRebalanced() : base(ModDisplayName) { }
        public override string GetVersion()
        {
            return version;
        }
        public static void LogMessage(object message)
        {
            Instance.Log(message.ToString());
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
            ModHooks.AfterTakeDamageHook += (int hazType, int damage) =>
            {
                int? retVal = RunHandlers<int?>(UsableHook.AfterDamage, damage);
                return retVal ?? damage;
            };
            On.GameManager.BeginSceneTransition += (orig, self, sceneLoadData) =>
            {
                RunHandlers(UsableHook.BeforeLoad, null);
                orig(self, sceneLoadData);
            };
            ModHooks.HitInstanceHook+= (HutongGames.PlayMaker.Fsm owner, HitInstance inst)=>{
                foreach (var field in inst.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    CharmsRebalanced.LogMessage($"{field} {field.GetValue(inst)}");
                }
                return inst;
            };
            ILHooks.AutoRegisterAll();
            ILHooks.EnableAll();
            On.UIManager.TogglePauseGame += UpdateConsts;
            On.HeroController.Start += OnSaveLoad;
            Log("Loaded");
        }
        public void Unload()
        {
            UsedHooks.UnregisterAllHooks();
            ILHooks.DisableAll();
            ValueOverrides.SetOrigAll();
            Log("Unloaded");
        }
        public void UpdateConsts(On.UIManager.orig_TogglePauseGame orig, UIManager self)
        {
            bool willBePaused = GameManager.instance.isPaused;
            orig(self);
            if (!willBePaused)
            {
                ValueOverrides.SetAll();
                ILHooks.DisableAll();
                ILHooks.EnableAll();
            }
        }
        public void OnSaveLoad(On.HeroController.orig_Start orig, HeroController self)
        {
            orig(self);
            CharmMods.CreateConstEdits();
            ValueOverrides.SetAll();
        }
        public static class ValueOverrides
        {
            private abstract class ValueOverride
            {
                public abstract void Set();
                public abstract void SetOrig();
                public abstract void SetModded();
            };
            private class ValueOverride<T> : ValueOverride
            {
                T orig;
                T modded;
                Action<T> setter;
                string charm;
                public ValueOverride(T orig, T modded, Action<T> setter, string charm)
                {
                    this.orig = orig;
                    this.modded = modded;
                    this.setter = setter;
                    this.charm = charm;
                }
                public override void Set()
                {
                    bool isModded = Config.PatchesEnabled[charm];
                    if (isModded)
                    {
                        SetModded();
                    }
                    else
                    {
                        SetOrig();
                    }
                }
                public override void SetModded()
                {
                    setter(modded);
                }
                public override void SetOrig()
                {
                    setter(orig);
                }
            }
            private static List<ValueOverride> CharmValueOverrides = new();
            public static void RegisterValueOverride<T>(T orig, T modded, Action<T> setter, string charm)
            {
                CharmValueOverrides.Add(new ValueOverride<T>(orig, modded, setter, charm));
            }
            public static void SetAll()
            {
                foreach (var item in CharmValueOverrides)
                {
                    item.Set();
                }
            }
            public static void SetOrigAll()
            {
                foreach (var item in CharmValueOverrides)
                {
                    item.SetOrig();
                }
            }
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
            public static Dictionary<string, bool> PatchesEnabled
            {
                get
                {
                    return _Config.settingsInstance.patchesEnabled
                        .ToDictionary(
                            kv => kv.Key,
                            kv => kv.Value == 1
                        );
                }

                set
                {
                    foreach (var kv in value)
                    {
                        _Config.settingsInstance.patchesEnabled[kv.Key] = kv.Value ? 1 : 0;
                    }
                }
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
                        { "voidsoul", 1 },
                        { "carefree_melody",1}
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

            AddPatch(b, "Fragile Heart Patch", "fragile_heart");
            AddPatch(b, "Fragile Greed Patch", "fragile_greed");
            AddPatch(b, "Fragile Strength Patch", "fragile_strength");
            AddPatch(b, "Spell Twister Patch", "spell_twister");
            AddPatch(b, "Steady Body Patch", "steady_body");
            AddPatch(b, "Heavy Blow Patch", "heavy_blow");
            AddPatch(b, "Quick Slash Patch", "quick_slash");
            AddPatch(b, "Longnail Patch", "longnail");
            AddPatch(b, "Mark of Pride Patch", "mark_of_pride");
            AddPatch(b, "Fury of the Fallen Patch", "fury");

            return b.CreateMenuScreen();
        }
        private MenuScreen BuildRow3(MenuScreen parent)
        {
            var b = new Modmenus.ModMenuScreenBuilder("Charm Row 3", parent);

            AddPatch(b, "Thorns of Agony Patch", "thorns");
            AddPatch(b, "Baldur Shell Patch", "baldur");
            AddPatch(b, "Flukenest Patch", "flukenest");
            AddPatch(b, "Defender's Crest Patch", "crest");
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
            SoulGain,
            BeforeLoad,
            AfterDamage
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
        public static class Timers
        {
            private static Dictionary<string, ThresholdTimer> timers = new();

            private class ThresholdTimer
            {
                private float[] limits;     // sorted from smallest → largest
                private bool[] passed;      // whether each limit has been crossed
                private float timer = 0f;   // current elapsed time
                private float max;          // largest limit

                public ThresholdTimer(float[] limits)
                {
                    if (limits == null || limits.Length == 0)
                        throw new ArgumentException("Timer must have at least one limit.");

                    // clone & sort limits
                    this.limits = limits.OrderBy(x => x).ToArray();

                    passed = new bool[this.limits.Length];
                    max = this.limits[this.limits.Length - 1];
                }

                public void Reset()
                {
                    timer = 0f;
                    for (int i = 0; i < passed.Length; i++)
                        passed[i] = false;
                }

                public void Update()
                {
                    if (timer >= max)
                        return; // finished

                    timer += Time.deltaTime;

                    // check thresholds
                    for (int i = 0; i < limits.Length; i++)
                    {
                        if (!passed[i] && timer >= limits[i])
                        {
                            passed[i] = true;
                            // optionally fire callbacks here
                        }
                    }
                }

                public bool GetCond(int index)
                {
                    if (index < 0 || index >= passed.Length)
                        return false;
                    return passed[index];
                }
            }

            // --- Public API ---
            public static void Declare(string name, float[] limits)
                => timers[name] = new ThresholdTimer(limits);

            public static void Reset(string name)
            {
                if (timers.TryGetValue(name, out var t))
                    t.Reset();
            }

            public static bool GetCond(string name, int index)
            {
                if (!timers.TryGetValue(name, out var t))
                    return false;
                return t.GetCond(index);
            }

            private static void UpdateAll()
            {
                foreach (var t in timers.Values)
                    t.Update();
            }

            static Timers()
            {
                ModHooks.HeroUpdateHook += UpdateAll;
            }
        }
    }
}