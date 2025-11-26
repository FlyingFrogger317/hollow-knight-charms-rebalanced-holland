using System;
using System.Runtime.InteropServices;
namespace CharmsRebalanced
{
    public static class CharmMods
    {
        private static void RegisterCharmHandler(CharmsRebalanced.UsableHook hook, string[] charms, CharmsRebalanced.CharmHandler handler)
        {
            CharmsRebalanced.Instance.RegisterCharmHandler(hook, charms, handler);
        }
        private static void RegisterCharmHandler(CharmsRebalanced.UsableHook hook, string charm, CharmsRebalanced.CharmHandler handler)
        {
            CharmsRebalanced.Instance.RegisterCharmHandler(hook, charm, handler);
        }
        public static void Init()
        {

            RegisterCharmHandler(CharmsRebalanced.UsableHook.SoulGain, ["soul_catcher", "!soul_eater"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul--;
                return addSoul;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.SoulGain, ["soul_eater", "!soul_catcher"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= 2;
                return addSoul;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.SoulGain, ["soul_eater", "soul_catcher"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= 3;
                return addSoul;
            });
        }
    }
}