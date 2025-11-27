using MonoMod.Cil;
using UnityEngine;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;
using System;

// All IL hooks go here
namespace CharmsRebalanced
{
    public static class ILHooks
    {
        private static List<(Action Enable, Action Disable)> registeredHooks = new();
        public static void Register(Action enable, Action disable)
        {
            registeredHooks.Add((enable, disable));
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
        public static class SprintmasterMakeWorkInAir
        {
            static SprintmasterMakeWorkInAir()
            {
                ILHooks.Register(Enable, Disable);
            }
            public static void Enable()
            {
                IL.HeroController.Move += Patch;
            }
            public static void Disable()
            {
                IL.HeroController.Move -= Patch;
            }
            private static void Patch(ILContext il)
            {
                ILCursor c = new ILCursor(il);
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
    }
}