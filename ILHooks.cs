using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using UnityEngine;
[AttributeUsage(AttributeTargets.Class)]
public sealed class AutoInit : Attribute {}
// All IL hooks go here
namespace CharmsRebalanced
{
    public static class ILHooks
    {
        private static List<(Action Enable, Action Disable)> registeredHooks = new();
        public static void AutoRegisterAll()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var hookClasses = assembly.GetTypes()
                .Where(t => t.IsAbstract && t.IsSealed) // static class
                .Where(t => t.IsDefined(typeof(AutoInit), false))
                .OrderBy(t => t.Name);
            foreach (Type hook in hookClasses)
            {
                Register(hook);
            }
        }

        public static void Register(Type inClass)
        {
            CharmsRebalanced.Instance.Log($"Registering {inClass.Name}");
            Action enable = () =>
            {
                CharmsRebalanced.Instance.Log($"Enabling {inClass.Name}");
                inClass.GetMethod("Enable").Invoke(null, null);
            };
            Action disable = () =>
            {
                CharmsRebalanced.Instance.Log($"Disabling {inClass.Name}");
                inClass.GetMethod("Disable").Invoke(null, null);
            };
            registeredHooks.Add((enable,disable));
        }
        public static void EnableAll()
        {
            foreach (var (enable, _) in registeredHooks)
            {
                enable();
            }
        }
        public static void DisableAll()
        {
            foreach (var (_, disable) in registeredHooks)
            {
                disable();
            }
        }
    }
    [AutoInit]
    public static class SprintmasterMakeWorkInAir
    {
        public static void Enable()
        {
            if (CharmsRebalanced.Config.PatchesEnabled["sprintmaster"]) IL.HeroController.Move += Patch;
        }
        public static void Disable()
        {
            IL.HeroController.Move -= Patch;
        }
        private static void Patch(ILContext il)
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.Before, x => x.MatchLdarg(0), x => x.MatchLdfld<HeroController>("cState"), x => x.MatchLdfld<HeroControllerStates>("onGround"), x => x.MatchBrfalse(out ILLabel _)))
            {
                c.RemoveRange(3);
                c.Emit(OpCodes.Ldc_I4_1);
                CharmsRebalanced.Instance.Log("Patched Sprintmaster to work in air if you also have Dashmaster.");
            }
            if (c.TryGotoNext(MoveType.Before, x => x.MatchLdarg(0), x => x.MatchLdfld<HeroController>("cState"), x => x.MatchLdfld<HeroControllerStates>("onGround"), x => x.MatchBrfalse(out ILLabel _)))
            {
                c.RemoveRange(3);
                c.Emit(OpCodes.Ldc_I4_1);
                CharmsRebalanced.Instance.Log("Patched Sprintmaster to work in air.");
            }
        }
    }
    [AutoInit]
    public static class GrubberflyRemoveMaxHealthRestraint
    {
        public static void Enable()
        {
            if (CharmsRebalanced.Config.PatchesEnabled["grubberflys_elegy"])
            {
                IL.HeroController.Attack += Patch;
            }
        }
        public static void Disable()
        {
            IL.HeroController.Attack -= Patch;
        }

        private static void Patch(ILContext il)
        {
            ILCursor c = new(il);

            // -------------------------------------------------------
            // PATCH ALL 3 ELEGY BEAM CONDITIONS
            // -------------------------------------------------------
            for (int i = 0; i < 3; i++)
            {
                // Match: ldstr "equippedCharm_35" → GetBool
                if (!c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<HeroController>("playerData"),
                    x => x.MatchLdstr("equippedCharm_35"),
                    x => x.MatchCallvirt<PlayerData>("GetBool")
                ))
                {
                    CharmsRebalanced.LogMessage($"ElegyCond matcher {i} FAILED");
                    break;
                }

                // At this point the GetBool result is on the stack.
                // Replace it with our delegate value.
                c.EmitDelegate(GrubberflyBeamCondition);
                c.Emit(OpCodes.Pop); // discard original GetBool bool
            }


            // Reset cursor for Fury
            c.Index = 0;

            // -------------------------------------------------------
            // PATCH ALL 3 FURY BEAM CONDITIONS
            // -------------------------------------------------------
            for (int i = 0; i < 3; i++)
            {
                // Find: ldstr "equippedCharm_6" → callvirt GetBool
                if (!c.TryGotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<HeroController>("playerData"),
                    x => x.MatchLdstr("equippedCharm_6"),
                    x => x.MatchCallvirt<PlayerData>("GetBool")
                ))
                {
                    CharmsRebalanced.LogMessage($"FuryCond matcher {i} FAILED");
                    break;
                }

                c.EmitDelegate(FuryCondition);
                c.Emit(OpCodes.Pop);
            }
        }

        private static bool GrubberflyBeamCondition()
        {
            CharmsRebalanced.LogMessage("Elegy1");
            return !FuryCondition();
        }
        private static bool FuryCondition()
        {
            CharmsRebalanced.LogMessage("Elegy2");
            bool hasFuryEquipped = PlayerData.instance.GetBool("charmEquipped_6");
            bool willFuryApply = PlayerData.instance.health <= 3;
            return hasFuryEquipped && willFuryApply;
        }
    }
}