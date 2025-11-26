using Modding;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CharmsRebalanced.CharmMods;
using CharmsRebalanced.CharmUtils;
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
            SoulGain,
            Initialize
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