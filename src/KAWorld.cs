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
using IL;
using static KarmaAppetite.KABase;

namespace KarmaAppetite
{
    internal class KAWorld //WORLD / STORY
    {

        //-------APPLY HOOKS-------

        public void KAWorld_Hooks()
        {
            //Region access
            On.GateKarmaGlyph.ctor += hook_GateKarmaGlyph_ctor;
            On.RegionGate.customKarmaGateRequirements += hook_RegionGate_customKarmaGateRequirements;
            On.SlugcatStats.getSlugcatOptionalRegions += hook_SlugcatStats_getSlugcatOptionalRegions;
            //Energy cell
            On.MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell.Update += hook_RM_CORE_EnergyCell;
            //Pearls
            On.StoryGameSession.ctor += hook_StoryGameSession_ctor;
            On.DataPearl.ApplyPalette += hook_DataPearl_ApplyPalette;
            On.DataPearl.UniquePearlMainColor += hook_DataPearl_UniquePearlMainColor;
            On.DataPearl.UniquePearlHighLightColor += hook_DataPearl_UniquePearlHighLightColor;
            On.SLOracleBehaviorHasMark.GrabObject += hook_SLOracleBehaviorHasMark_GrabObject;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += hook_MoonConversation_AddEvents;
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
            return list.ToArray();
        }

        //---5P: ENERGY CELL---

        private void hook_StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig.Invoke(self, saveStateNumber, game);
            self.saveState.miscWorldSaveData.pebblesEnergyTaken = true;
            self.saveState.miscWorldSaveData.moonHeartRestored = true;
        }

        private void hook_RM_CORE_EnergyCell(On.MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell.orig_Update orig, MSCRoomSpecificScript.RM_CORE_EnergyCell self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.myEnergyCell != null)
            {
                self.room.RemoveObject(self.myEnergyCell);
            }
        }

        //---OVERSEERS---



        //---DATA PEARLS--- (enums added in Base)

        //Pearl visuals
        private void hook_DataPearl_ApplyPalette(On.DataPearl.orig_ApplyPalette orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);
            if ((self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType == KarmaAppetiteEnums.KAType.TCoSPearl || (self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType == KarmaAppetiteEnums.KAType.CorruptionPearl || (self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType == KarmaAppetiteEnums.KAType.VoidPearl)
            {
                self.color = DataPearl.UniquePearlMainColor((self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType);
                self.highlightColor = DataPearl.UniquePearlHighLightColor((self.abstractPhysicalObject as DataPearl.AbstractDataPearl).dataPearlType);
            }
        }

        private Color hook_DataPearl_UniquePearlMainColor(On.DataPearl.orig_UniquePearlMainColor orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
        {
            Color orig_result = orig.Invoke(pearlType);
            if (pearlType == KarmaAppetiteEnums.KAType.TCoSPearl)
            {
                return new Color(0.89f, 0.42f, 0f);
            }
            if (pearlType == KarmaAppetiteEnums.KAType.CorruptionPearl)
            {
                return new Color(0f, 0.12f, 0.63f);
            }
            if (pearlType == KarmaAppetiteEnums.KAType.VoidPearl)
            {
                return new Color(0.98f, 0.741f, 0f);
            }
            return orig_result;
        }

        private Color? hook_DataPearl_UniquePearlHighLightColor(On.DataPearl.orig_UniquePearlHighLightColor orig, DataPearl.AbstractDataPearl.DataPearlType pearlType)
        {
            Color? orig_result = orig.Invoke(pearlType);
            if (pearlType == KarmaAppetiteEnums.KAType.CorruptionPearl)
            {
                return new Color(0.043f, 0.318f, 1f);
            }
            if (pearlType == KarmaAppetiteEnums.KAType.VoidPearl)
            {
                return new Color(1f, 0.55f, 0f);
            }
            return orig_result;
        }

        //Conversations
        private void hook_SLOracleBehaviorHasMark_GrabObject(On.SLOracleBehaviorHasMark.orig_GrabObject orig, SLOracleBehaviorHasMark self, PhysicalObject item)
        {
            if (item is DataPearl)
            {
                if ((item as DataPearl).AbstractPearl.dataPearlType == KarmaAppetiteEnums.KAType.TCoSPearl)
                {
                    self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(KarmaAppetiteEnums.KAType.Moon_Pearl_TCoS, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                }
                else if ((item as DataPearl).AbstractPearl.dataPearlType == KarmaAppetiteEnums.KAType.CorruptionPearl)
                {
                    self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(KarmaAppetiteEnums.KAType.Moon_Pearl_Corruption, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                }
                else if ((item as DataPearl).AbstractPearl.dataPearlType == KarmaAppetiteEnums.KAType.VoidPearl)
                {
                    self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(KarmaAppetiteEnums.KAType.Moon_Pearl_Void, self, SLOracleBehaviorHasMark.MiscItemType.NA);
                }
            }
            orig.Invoke(self, item);
        }

        private void hook_MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            orig.Invoke(self);
            if (self.id == KarmaAppetiteEnums.KAType.Moon_Pearl_TCoS)
            {
                self.PearlIntro();
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate(
                    "... [TCoS Pearl text]"
                    ), 10));
                return;
            }
            if (self.id == KarmaAppetiteEnums.KAType.Moon_Pearl_Corruption)
            {
                self.PearlIntro();
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate(
                    "... [Corruption Pearl text]"
                    ), 10));
                return;
            }
            if (self.id == KarmaAppetiteEnums.KAType.Moon_Pearl_Void)
            {
                self.PearlIntro();
                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate(
                    "... [Void Pearl text]"
                    ), 10));
                return;
            }
        }


    }
}