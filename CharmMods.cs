using System;
using System.Diagnostics.Eventing.Reader;
namespace CharmsRebalanced
{
    public static class CharmMods
    {
        private static bool hasCreatedConsts = false;
        private static void RegisterCharmHandler(CharmsRebalanced.UsableHook hook, string[] charms, CharmsRebalanced.CharmHandler handler)
        {
            CharmsRebalanced.Instance.RegisterCharmHandler(hook, charms, handler);
        }
        private static void RegisterCharmHandler(CharmsRebalanced.UsableHook hook, string charm, CharmsRebalanced.CharmHandler handler)
        {
            CharmsRebalanced.Instance.RegisterCharmHandler(hook, charm, handler);
        }
        private static void RegisterValueOverride<T>(T orig, T modded, Action<T> setter, string charm) {
            CharmsRebalanced.ValueOverrides.RegisterValueOverride<T>(orig, modded, setter, charm);
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
        public static void CreateConstEdits()
        {
            if (hasCreatedConsts) return;
            //put all logic for HeroController consts here, like quick slash and dashmaster
            RegisterValueOverride<float>(HeroController.instance.ATTACK_COOLDOWN_TIME_CH, HeroController.instance.ATTACK_COOLDOWN_TIME_CH, v => HeroController.instance.ATTACK_COOLDOWN_TIME_CH = v, "quick_slash"); 
            hasCreatedConsts = true;
        }
    }
}