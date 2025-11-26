using Modding;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace CharmsRebalanced
{
    public class CharmsRebalanced : Mod
    {
        internal static CharmsRebalanced Instance;
        internal static string ModDisplayName = "CharmsRebalanced";
        internal static string version = "1.0.0.2";
        public CharmsRebalanced() : base(ModDisplayName) { }
        public override string GetVersion()
        {
            return version;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Instance = this;
            CreateCharmMods();
            ModHooks.GetPlayerIntHook += OnPlayerDataGetInt;
            ModHooks.CharmUpdateHook += (PlayerData data, HeroController controller) =>
            {
                RunHandlers(UsableHook.CharmUpdate, data, controller);
            };
            ModHooks.SoulGainHook += soul =>
            {
                int? retVal = RunHandlers<int?>(UsableHook.SoulGain, soul);
                return retVal ?? soul;
            };
        }
        readonly static Dictionary<int, string> charmNames = new Dictionary<int, string>
        {
            { 1, "swarm" },
            { 2, "compass" },//no changes
            { 3, "grubsong" },
            { 4, "stalwart" },
            { 5, "baldur" },
            { 6, "fury" },
            { 7, "quick_focus" },
            { 8, "lifeblood_heart" },
            { 9, "lifeblood_core" },
            { 10, "crest" },
            { 11, "flukenest" },
            { 12, "thorns" },
            { 13, "mark_of_pride" },
            { 14, "steady_body" },
            { 15, "heavy_blow" },
            { 16, "sharp_shadow" },
            { 17, "spore_shroom" },
            { 18, "longnail" },
            { 19, "shaman_stone" },
            { 20, "soul_catcher" },//done
            { 21, "soul_eater" },//done
            { 22, "glowing_womb" },
            { 23, "fragile_heart" },
            { 24, "fragile_greed" },
            { 25, "fragile_strength" },
            { 26, "nailmasters_glory" },//no changes
            { 27, "jonis_blessing" },
            { 28, "shape_of_unn" },
            { 29, "hiveblood" },
            { 30, "dream_wielder" },
            { 31, "dashmaster" },
            { 32, "quick_slash" },
            { 33, "spell_twister" },
            { 34, "deep_focus" },
            { 35, "grubberflys_elegy" },
            { 36, "kingsoul" },// figure out how to deal with voidsoul being an extension of void heart and being unable to equip
            { 37, "sprintmaster" },
            { 38, "dreamshield" },
            { 39, "weaversong" },
            { 40, "grimmchild" }
        };
        static Dictionary<string, int> charmCostChange = new Dictionary<string, int>
        {
            { "lifeblood_heart", -1 },
            { "soul_catcher", -1 },
            { "soul_eater",-1 }
        };
        static Dictionary<string, int> charmInts = charmNames.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        private int OnPlayerDataGetInt(string field, int orig)
        {
            if (field.StartsWith("charmCost_"))
            {
                int.TryParse(field.Substring(10), out int charmId);
                string charmName = charmNames[charmId];
                if (!charmCostChange.TryGetValue(charmName, out int valueChange)) return orig;
                int newCharmCost = orig + valueChange;
                return newCharmCost;
            }
            return orig;
        }
        private bool HasCharm(string charmName)
        {
            bool invert = false;
            if (charmName[0] == '!')
            {
                charmName = charmName.Substring(1);
                invert = true;
            }
            int charmInt = charmInts[charmName];

            string identifier = "equippedCharm_" + charmInt.ToString();
            bool hasCharm = PlayerData.instance.GetBool(identifier);
            return invert ? !hasCharm : hasCharm;
        }
        //handle registerable callbacks
#nullable enable
        private delegate object? CharmHandler(object[] args);
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
                foreach (string charm in charms)
                {
                    if (HasCharm(charm))
                    {
                        charmsNeeded--;
                    }
                }
                if (charmsNeeded == 0)
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
        private void RegisterCharmHandler(UsableHook hook, string charm, CharmHandler handler)
        {
            RegisterCharmHandler(hook, [charm], handler);
        }
        private void RegisterCharmHandler(UsableHook hook, string[] charms, CharmHandler handler)
        {
            RegisteredHandlers[hook].Add((charms, handler));
        }
#nullable disable
        internal void CreateCharmMods()
        {
            RegisterCharmHandler(UsableHook.SoulGain, ["soul_catcher", "!soul_eater"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul--;
                return addSoul;
            });
            RegisterCharmHandler(UsableHook.SoulGain, ["soul_eater", "!soul_catcher"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= 2;
                return addSoul;
            });
            RegisterCharmHandler(UsableHook.SoulGain, ["soul_eater","soul_catcher"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= 3;
                return addSoul;
            });
        }
    }
}