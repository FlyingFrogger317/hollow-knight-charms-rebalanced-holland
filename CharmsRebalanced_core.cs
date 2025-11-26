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
            CharmMods.Init();
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
            Initialize
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