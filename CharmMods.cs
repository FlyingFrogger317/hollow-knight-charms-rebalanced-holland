using System;
using UnityEngine;
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
        private static void RegisterValueOverride<T>(T orig, T modded, Action<T> setter, string charm)
        {
            CharmsRebalanced.ValueOverrides.RegisterValueOverride<T>(orig, modded, setter, charm);
        }
        private static class Config {
            public static int catcherAdjust = -1;
            public static int eaterAdjust = -2;
            public static int catcherEaterAdjust = -3;
            public static float carefreeCooldown = 10f;
            public static float shellCooldown = 30f;
            public static float carefreeShellCooldown = 7f;
        }
        public static void Init()
        {

            RegisterCharmHandler(CharmsRebalanced.UsableHook.SoulGain, ["soul_catcher", "!soul_eater"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= Config.catcherAdjust;
                return addSoul;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.SoulGain, ["soul_eater", "!soul_catcher"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= Config.eaterAdjust;
                return addSoul;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.SoulGain, ["soul_eater", "soul_catcher"], args =>
            {
                int addSoul = (int)(args[0]);
                addSoul -= Config.catcherEaterAdjust;
                return addSoul;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.BeforeLoad, "swarm", args =>
            {
                int total = 0;
                foreach (var fsm in GameObject.FindObjectsOfType<PlayMakerFSM>())
                {
                    if (fsm.FsmName == "Geo Control")
                    {
                        string state = fsm.ActiveStateName;

                        bool carrying =
                            state == "Fly To Hero" ||
                            state.Contains("Home") ||
                            state.Contains("Return");   // some variants use "Return"

                        if (carrying)
                        {
                            int amount = fsm.FsmVariables.GetFsmInt("geoAmount").Value;

                            CharmsRebalanced.LogMessage(
                                $"'{fsm.gameObject.name}' in state '{state}' worth {amount}"
                            );
                            total += amount;
                            GameObject.Destroy(fsm.gameObject);
                        }
                    }
                }
                if (total > 0) HeroController.instance.AddGeoQuietly(total);
                return null;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.AfterDamage, ["carefree_melody", "!stalwart"], args =>
            {
                int damage = (int)args[0];
                damage = CalcDamageForShield(damage);
                return damage;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.AfterDamage, ["!carefree_melody", "stalwart"], args =>
            {
                int damage = (int)args[0];
                damage = CalcDamageForShield(damage);
                return damage;
            });
            RegisterCharmHandler(CharmsRebalanced.UsableHook.AfterDamage, ["carefree_melody", "stalwart"], args =>
            {
                int damage = (int)args[0];
                damage = CalcDamageForShield(damage);
                return damage;
            });
            CharmsRebalanced.Timers.Declare("ShieldCarefreeSteady", [Config.carefreeShellCooldown, Config.carefreeCooldown, Config.shellCooldown]);
            On.ObjectPool.Spawn_GameObject_Transform_Vector3_Quaternion += (On.ObjectPool.orig_Spawn_GameObject_Transform_Vector3_Quaternion orig, GameObject prefab, Transform parent, Vector3 pos, Quaternion rot) =>
            {
                GameObject go = orig(prefab, parent, pos, rot);
                if (go.name.Contains("Grubberfly Beam") && CharmsRebalanced.Config.PatchesEnabled["grubberflys_elegy"])
                {
                    if (!go.GetComponent<GrubberflyBeamSoul>())
                        go.AddComponent<GrubberflyBeamSoul>();
                }
                return go;
            };
        }
        public static void CreateConstEdits()
        {
            CharmsRebalanced.Instance.Log("edits");
            if (hasCreatedConsts) return;
            //put all logic for HeroController consts here, like quick slash and dashmaster
            RegisterValueOverride<float>(HeroController.instance.ATTACK_COOLDOWN_TIME_CH, 0.287f, v => HeroController.instance.ATTACK_COOLDOWN_TIME_CH = v, "quick_slash");
            RegisterValueOverride<float>(HeroController.instance.ATTACK_DURATION_CH, 0.305f, v => HeroController.instance.ATTACK_DURATION_CH = v, "quick_slash");
            RegisterValueOverride<float>(HeroController.instance.RUN_SPEED_CH, 10.8f, v => HeroController.instance.RUN_SPEED_CH = v, "sprintmaster");
            RegisterValueOverride<float>(HeroController.instance.RUN_SPEED_CH_COMBO, 11.6f, v => HeroController.instance.RUN_SPEED_CH_COMBO = v, "sprintmaster");
            RegisterValueOverride<float>(HeroController.instance.SHADOW_DASH_COOLDOWN, 1.0f, v => HeroController.instance.SHADOW_DASH_COOLDOWN = v, "dashmaster");
            hasCreatedConsts = true;
        }
        static int CalcDamageForShield(int damage)
        {
            bool carefree = CharmUtils.GetCharm("carefree_melody").equipped;
            bool shell = CharmUtils.GetCharm("stalwart_shell").equipped;
            bool heart = CharmUtils.GetCharm("fragile_heart").equipped;
            if (carefree) damage = 12;
            bool cdReady = false;
            if (carefree && shell)
            {
                cdReady = CharmsRebalanced.Timers.GetCond("ShieldCarefreeSteady", 0);
            }
            else if (carefree)
            {
                cdReady = CharmsRebalanced.Timers.GetCond("ShieldCarefreeSteady", 1);
            }
            else
            {
                cdReady = CharmsRebalanced.Timers.GetCond("ShieldCarefreeSteady", 2);
            }
            if (cdReady && shieldCharges == 0)
            {
                CharmsRebalanced.Timers.Reset("ShieldCarefreeSteady");
                shieldCharges = heart && carefree ? 2 : 1;
            }
            if (shieldCharges > 0)
            {
                damage = 0;
                shieldCharges--;
                if (shell)
                {
                    var hc = HeroController.instance;
                    typeof(HeroController).GetMethod("CancelDamageRecoil").Invoke(hc,null);
                    shouldGiveExtraIFrames = true;
                    // remove knockback
                    hc.cState.recoiling = false;
                    hc.cState.recoilingLeft = false;
                    hc.cState.recoilingRight = false;
                }
            }
            return damage;
        }
        private static int shieldCharges = 0;
        public static bool shouldGiveExtraIFrames = false;
        public class GrubberflyBeamSoul : MonoBehaviour
        {
            private void Awake()
            {
                // Make sure the collider is a trigger (required for OnTriggerEnter2D)
                var soulCol = gameObject.AddComponent<BoxCollider2D>();
                soulCol.isTrigger = true;
                soulCol.size = new Vector2(1f, 1f);
            }

            private void OnTriggerEnter2D(Collider2D other)
            {
                // Enemy layers in HK: 11 and 22 (special cases)
                int layer = other.gameObject.layer;

                if (layer == 11 || layer == 22)
                {
                    int currentSoul = PlayerData.instance.GetInt("MPCharge");
                    HeroController.instance.SoulGain();
                    int newSoul = PlayerData.instance.GetInt("MPCharge");
                    int diff = newSoul - currentSoul;
                    double preciseHalf = (double)diff / 2;
                    int half = (int)Math.Floor(preciseHalf);
                    HeroController.instance.TakeMPQuick(half);
                }
            }
        }
    }
}