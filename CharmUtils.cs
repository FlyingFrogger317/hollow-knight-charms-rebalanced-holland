using System.Collections.Generic;
using System.Linq;
namespace CharmsRebalanced
{
    public static class CharmUtils
    {
        public static CharmData GetCharm(int charmId)
        {
            return new CharmData(charmNames[charmId]);
        }

        public static CharmData GetCharm(string charmName)
        {
            return new CharmData(charmName);
        }
        public static CharmData[] GetCharmsIfEquippedOrNot(params string[] charmNames)
        {
            List<CharmData> equippedCharms = new List<CharmData>();
            foreach (string charmName in charmNames)
            {
                if (charmName[0] == '!')
                {
                    CharmData charmDataNeg = GetCharm(charmName.Substring(1));
                    if (!charmDataNeg.equipped)
                    {
                        equippedCharms.Add(charmDataNeg);
                    }
                    continue;
                }
                CharmData charmData = GetCharm(charmName);
                if (charmData.equipped)
                {
                    equippedCharms.Add(charmData);
                }
            }
            return equippedCharms.ToArray();
        }
        readonly private static Dictionary<int, string> charmNames = new Dictionary<int, string>
        {
            { 1, "swarm" },//need screen leave clear
            { 2, "compass" },//no changes
            { 3, "grubsong" },//done, no changes
            { 4, "stalwart" },//total overhaul
            { 5, "baldur" },//total overhaul
            { 6, "fury" },//total overhaul
            { 7, "quick_focus" },//no changes
            { 8, "lifeblood_heart" },//- cost + hearts
            { 9, "lifeblood_core" },//- cost + hearts
            { 10, "crest" },//+range i think
            { 11, "flukenest" },//-1 fluke for base, and more
            { 12, "thorns" },//dont know if i can do this
            { 13, "mark_of_pride" },
            { 14, "steady_body" },//no changes
            { 15, "heavy_blow" },
            { 16, "sharp_shadow" },
            { 17, "spore_shroom" },
            { 18, "longnail" },
            { 19, "shaman_stone" },//rework
            { 20, "soul_catcher" },//done
            { 21, "soul_eater" },//done, except the cast time (must figure out how to do it)
            { 22, "glowing_womb" },
            { 23, "fragile_heart" },//down to +25%
            { 24, "fragile_greed" },
            { 25, "fragile_strength" },
            { 26, "nailmasters_glory" },//no changes
            { 27, "jonis_blessing" },
            { 28, "shape_of_unn" },//idk but i think its just cost -1
            { 29, "hiveblood" },//unlimit
            { 30, "dream_wielder" },//idk
            { 31, "dashmaster" },//- shade cloak cd
            { 32, "quick_slash" },//+ attack cooldown, should be HeroController.ATTACK_DURATION_CH and HeroController.ATTACK_COOLDOWN_TIME_CH
            { 33, "spell_twister" },//nothing to do
            { 34, "deep_focus" },//decrease the penalty
            { 35, "grubberflys_elegy" },//figure out how to patch out stuff on lines around 17126 in c#
            { 36, "kingsoul" },// figure out how to deal with voidsoul being an extension of void heart and being unable to equip
            { 37, "sprintmaster" },//+more speed (edit HeroController.RUN_SPEED_CH or HeroController.RUN_SPEED_CH_COMBO if also dashmaster) and in the air (remove check for on ground at 17029 and 17033)
            { 38, "dreamshield" },
            { 39, "weaversong" },
            { 40, "grimmchild" }
        };
        private static Dictionary<string, int> charmCostChange = new Dictionary<string, int>
        {
            { "lifeblood_heart", -1 },
            { "soul_catcher", -1 },
            { "soul_eater",-1 }
        };
        private static Dictionary<string, int> charmInts = charmNames.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        public class CharmData
        {
            public int charmId => charmInts[charmName];
            public int costChange => CharmUtils.charmCostChange.TryGetValue(charmName, out int change) ? change : 0;
            public string charmName;
            internal CharmData(string charmName)
            {
                this.charmName = charmName;
            }
            public bool equipped
            {
                get
                {
                    return PlayerData.instance.GetBool($"equippedCharm_{charmId}");
                }
            }
        }
    }
}