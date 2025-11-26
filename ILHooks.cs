using HarmonyLib;
using MonoMod.Cil;
using UnityEngine;
// All IL hooks go here
[HarmonyPatch(typeof(HeroController), "Move",new System.Type[] {typeof(float)})]
public class SprintmasterMakeWorkInAir {
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, ILContext context) {
        ILCursor c = new ILCursor(context);
        if (c.TryGotoNext(MoveType.Before,x=>x.MatchLdarg(0),x=>x.MatchLdfld("cState"),x=>x.MatchLdfld("onGround"),x=>x.MatchBrfalse(out ILLabel _))) {
            c.RemoveRange(3);
            c.Emit(OpCodes.Ldc_I4_1);
            Log("Patched Sprintmaster to work in air if you also have Dashmaster.");
        }
        if (c.TryGotoNext(MoveType.Before,x=>x.MatchLdarg(0),x=>x.MatchLdfld("cState"),x=>x.MatchLdfld("onGround"),x=>x.MatchBrfalse(out ILLabel _))) {
            c.RemoveRange(3);
            c.Emit(OpCodes.Ldc_I4_1);
            Log("Patched Sprintmaster to work in air.");
        }
        return c.Finish().ToArray();
    }
}
namespace CharmsRebalanced.ILHooks
{
    
    public static class ILHooks {
        static private Harmony harmony;
        static void Initialize() {
            harmony = new Harmony("charmsrebalanced.ilhooks");
            harmony.PatchAll();
            Log("IL Hooks patched.");
        }
    }
}