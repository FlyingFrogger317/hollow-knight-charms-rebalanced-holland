using MonoMod.Cil;
using UnityEngine;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;

// All IL hooks go here
namespace CharmsRebalanced.ILHooks
{
    public static class ILHooks
    {
        public static class SprintmasterMakeWorkInAir
        {
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