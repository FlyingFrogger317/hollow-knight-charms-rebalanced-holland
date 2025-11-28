using MonoMod.Cil;
using UnityEngine;
using Modding;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;
using System;
using InControl;
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
                CharmsRebalanced.Instance.Log("Registering IL Hook SprintmasterMakeWorkInAir");
                ILHooks.Register(Enable, Disable);
            }
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
    }
    public static class GrubberflyRemoveMaxHealthRestraint
    {
        static GrubberflyRemoveMaxHealthRestraint()
        {
            CharmsRebalanced.Instance.Log("Registering IL Hook GrubberflyRemoveMaxHealthRestraint");
            ILHooks.Register(Enable, Disable);
        }
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
            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(
                    MoveType.Before,
                    x => x.MatchCallvirt<PlayerData>("GetInt"),    
                    x => x.MatchCallvirt<PlayerData>("GetInt")     
                );

                // Remove original condition (until next branch)
                c.GotoNext(MoveType.After, x => x.Match(OpCodes.Brfalse) || x.Match(OpCodes.Brtrue));
                Instruction branchingInstr = c.Prev;
                var target = branchingInstr.Operand;

                // Remove all IL that created the boolean
                c.GotoPrev(MoveType.Before, x => x.MatchCallvirt<PlayerData>("GetInt"));
                int startIndex = c.Index;
                int endIndex = il.Body.Instructions.IndexOf(branchingInstr);
                c.Index = startIndex;
                c.RemoveRange(endIndex - startIndex);

                // Inject delegate result
                c.EmitDelegate(ElegyBeamCondition);
                c.Emit(OpCodes.Brfalse, target);
            }
            for (int i = 0; i < 3; i++)
            {
                c.GotoNext(
                    MoveType.Before,
                    x => x.MatchCallvirt<PlayerData>("GetInt"),    
                    x => x.Match(OpCodes.Ldc_I4_1)                 
                );

                c.GotoNext(MoveType.After, x => x.Match(OpCodes.Brfalse) || x.Match(OpCodes.Brtrue));
                Instruction branch = c.Prev;
                var brTarget = branch.Operand;

                c.GotoPrev(MoveType.Before, x => x.MatchCallvirt<PlayerData>("GetInt"));
                int start = c.Index;
                int end = il.Body.Instructions.IndexOf(branch);
                c.Index = start;
                c.RemoveRange(end - start);

                c.EmitDelegate(FuryCondition);
                c.Emit(OpCodes.Brfalse, brTarget);
            }
        }
        private static bool ElegyBeamCondition()
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