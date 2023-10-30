using System;
using System.Collections.Generic;
using BepInEx;
using MonoMod.RuntimeDetour;
using MonoMod;
using On;
using RWCustom;
using UnityEngine;
using SlugBase.Features;
using MoreSlugcats;
using SlugBase.DataTypes;

namespace KarmaAppetite
{
    internal class KAWorld //WORLD / STORY
    {

        //-------APPLY HOOKS-------

        public void KAWorld_Hooks()
        {
            On.GateKarmaGlyph.ctor += hook_GateKarmaGlyph_ctor;
            On.RegionGate.customKarmaGateRequirements += hook_RegionGate_customKarmaGateRequirements;
            On.SlugcatStats.getSlugcatOptionalRegions += hook_SlugcatStats_getSlugcatOptionalRegions;
        }


        //------IMPLEMENTATION------


        //---OE+LC REGIONS ACCESS---

        private void hook_GateKarmaGlyph_ctor(On.GateKarmaGlyph.orig_ctor orig, GateKarmaGlyph self, bool side, RegionGate gate, RegionGate.GateRequirement requirement)
        {
            orig.Invoke(self, side, gate, requirement);
            if (ModManager.MSC)
            {
                if (requirement == MoreSlugcatsEnums.GateRequirement.RoboLock)
                {
                    self.requirement = RegionGate.GateRequirement.FiveKarma;
                }
                else if (requirement == MoreSlugcatsEnums.GateRequirement.OELock)
                {
                    self.requirement = RegionGate.GateRequirement.OneKarma;
                }
            }
        }

        private void hook_RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig.Invoke(self);
            if (self.karmaRequirements[0] == MoreSlugcatsEnums.GateRequirement.RoboLock)
            {
                self.karmaRequirements[0] = RegionGate.GateRequirement.FiveKarma;
            }
            else if (self.karmaRequirements[0] == MoreSlugcatsEnums.GateRequirement.OELock)
            {
                self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma;
            }
            if (self.karmaRequirements[1] == MoreSlugcatsEnums.GateRequirement.RoboLock)
            {
                self.karmaRequirements[1] = RegionGate.GateRequirement.FiveKarma;
            }
            else if (self.karmaRequirements[1] == MoreSlugcatsEnums.GateRequirement.OELock)
            {
                self.karmaRequirements[1] = RegionGate.GateRequirement.OneKarma;
            }
        }

        private string[] hook_SlugcatStats_getSlugcatOptionalRegions(On.SlugcatStats.orig_getSlugcatOptionalRegions orig, SlugcatStats.Name i)
        {
            string[] collection = orig.Invoke(i);
            List<string> list = new List<string>(collection);
            bool flag = !list.Contains("OE");
            if (flag)
            {
                list.Add("OE");
            }
            bool flag2 = !list.Contains("LC");
            if (flag2)
            {
                list.Add("LC");
            }
            /*bool flag3 = !list.Contains("HR");
            if (flag3)
            {
                list.Add("HR");
            }*/
            return list.ToArray();
        }

    }
}