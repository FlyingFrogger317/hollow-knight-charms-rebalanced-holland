using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using System.IO;
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
        public static void Log(string message)
        {
            CharmsRebalanced.Instance.Log(message);
        }
        public static void NopRange(this ILContext il, int firstOne, int lastOne)
        {
            ILCursor nop = new(il);
            nop.Index = firstOne;
            for (int i = firstOne; i < lastOne + 1; i++)
            {
                nop.Next.OpCode = OpCodes.Nop;
                nop.Next.Operand = null;
                nop.Index++;
            }
        }
        public static void Set(this ILContext il, int id, OpCode opCode, object operand)
        {
            ILCursor setc = new(il);
            setc.Goto(id);
            setc.Next.OpCode = opCode;
            setc.Next.Operand = operand;
        }
        public static ILLabel NewLabel(this ILContext il, int id)
        {
            ILCursor labelC = new(il);
            labelC.Goto(id);
            return labelC.MarkLabel();
        }
        public static void DumpIL(this ILContext ctx, string dumpName)
        {
            string path = $"/tmp/{dumpName}.il";
            bool end = true;
            ILCursor c = new(ctx);
            c.Index = 0;
            StreamWriter writer = new(path, false);
            string dump = $"{c.Index}: {c.Next.OpCode} {c.Next.Operand}";
            Action<ILCursor> writedump = (ILCursor c) =>
            {
                if (c.Next.Operand is ILLabel label)
                {
                    Instruction target = label.Target;
                    int index = ctx.IndexOf(target);
                    dump += $"\n{c.Index}: {c.Next.OpCode} Label:";
                    dump += $"\n        {index}: {target.OpCode} {target.Operand}";
                }
                else
                {
                    dump += $"\n{c.Index}: {c.Next.OpCode} {c.Next.Operand}";
                }
            };
            do
            {
                c.Index++;
                writedump(c);
                end = c.Index >= c.Instrs.Count - 1;
            } while (!end);
            writer.Write(dump);
            writer.Close();
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
            il.DumpIL("initial");
            MethodInfo condElegy = typeof(GrubberflyRemoveMaxHealthRestraint)
                .GetMethod("GrubberflyBeamCondition", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo condFury = typeof(GrubberflyRemoveMaxHealthRestraint)
                .GetMethod("FuryBeamCondition", BindingFlags.Static | BindingFlags.NonPublic);
            il.NopRange(86, 106);
            il.Set(106, OpCodes.Call, condElegy);
            il.NopRange(148, 163);
            il.Set(163, OpCodes.Call, condFury);
            il.Set(164, OpCodes.Brfalse, il.NewLabel(489));
            il.NopRange(226, 246);
            il.Set(246, OpCodes.Call, condElegy);
            il.NopRange(289, 304);
            il.Set(304, OpCodes.Call, condFury);
            il.Set(305, OpCodes.Brfalse, il.NewLabel(489));
            il.NopRange(368,388);
            il.Set(388, OpCodes.Call, condElegy);
            il.NopRange(431, 446);
            il.Set(446, OpCodes.Call, condFury);
            il.Set(447, OpCodes.Brfalse, il.NewLabel(489));
            il.DumpIL("final");
        }
        //false means fury, true means normal
        private static bool GrubberflyBeamCondition()
        {
            CharmsRebalanced.LogMessage("Elegy");
            bool hasFuryEquipped = CharmUtils.GetCharm("fury").equipped;
            bool willFuryApply = PlayerData.instance.health <= 3;
            CharmsRebalanced.LogMessage(hasFuryEquipped.ToString());
            CharmsRebalanced.LogMessage(willFuryApply.ToString());
            CharmsRebalanced.LogMessage((hasFuryEquipped && willFuryApply).ToString());
            return !(hasFuryEquipped && willFuryApply);
        }
        private static bool FuryBeamCondition()
        {
            return !GrubberflyBeamCondition();
        }
    }
}