using System;
using System.Collections.Generic;
using UnityEngine;
using On;
using RWCustom;
using MoreSlugcats;
using static KarmaAppetite.KABase;
using IL;
using System.IO;

namespace KarmaAppetite
{
    internal class KAWorld //WORLD / STORY
    {

        //-------APPLY HOOKS-------

        public void KAWorld_Hooks()
        {
            //General
            On.RainWorldGame.SpawnPlayers_int_WorldCoordinate += hook_RainWorldGame_SpawnPlayers_A;
            On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += hook_RainWorldGame_SpawnPlayers_B;
            On.Player.ctor += hook_Player_ctor;
            //Region access
            On.GateKarmaGlyph.ctor += hook_GateKarmaGlyph_ctor;
            On.RegionGate.customKarmaGateRequirements += hook_RegionGate_customKarmaGateRequirements;
            On.SlugcatStats.getSlugcatOptionalRegions += hook_SlugcatStats_getSlugcatOptionalRegions;
            //Energy cell
            On.MoreSlugcats.MSCRoomSpecificScript.RM_CORE_EnergyCell.Update += hook_RM_CORE_EnergyCell;
            //Overseers
            On.OverseerGraphics.ctor += hook_OverseerGraphics_ctor;
            On.OverseerGraphics.DrawSprites += hook_OverseerGraphics_DrawSprites;
            On.OverseerGraphics.ColorOfSegment += hook_OverseerGraphics_ColorOfSegment;
            On.CoralBrain.Mycelium.UpdateColor += hook_CoralBrain_Mycelium_UpdateColor;
            On.OverseerGraphics.HologramMatrix.DrawSprites += hook_OverseerGraphics_HologramMatrix_DrawSprites;
            On.WorldLoader.GeneratePopulation += hook_WorldLoader_GeneratePopulation;
            On.OverseersWorldAI.DirectionFinder.StoryRoomInRegion += hook_OWAI_DirectionFinder_StoryRoomInRegion;
            On.OverseersWorldAI.DirectionFinder.StoryRegionPrioritys += hook_OWAI_DirectionFinder_StoryRegionPrioritys;
            On.OverseerAbstractAI.AbstractBehavior += hook_OverseerAbstractAI_AbstractBehavior;
            //Pearls
            On.StoryGameSession.ctor += hook_StoryGameSession_ctor;
            On.DataPearl.ApplyPalette += hook_DataPearl_ApplyPalette;
            On.DataPearl.UniquePearlMainColor += hook_DataPearl_UniquePearlMainColor;
            On.DataPearl.UniquePearlHighLightColor += hook_DataPearl_UniquePearlHighLightColor;
            On.SLOracleBehaviorHasMark.GrabObject += hook_SLOracleBehaviorHasMark_GrabObject;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += hook_MoonConversation_AddEvents;
        }


        //------IMPLEMENTATION------


        //---GENERAL---

        //Starting position
        private AbstractCreature hook_RainWorldGame_SpawnPlayers_A(On.RainWorldGame.orig_SpawnPlayers_int_WorldCoordinate orig, RainWorldGame self, int count, WorldCoordinate location)
        {
            if (self.world.GetAbstractRoom(location.room).name == "SZ_F01")
            {
                location.x = 5;
                location.y = 110;
                DeleteMetCM();
            }
            return orig.Invoke(self, count, location);
        }

        private AbstractCreature hook_RainWorldGame_SpawnPlayers_B(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
        {
            if (self.world.GetAbstractRoom(location.room).name == "SZ_F01")
            {
                location.x = 5;
                location.y = 110;
                DeleteMetCM();
            }
            return orig.Invoke(self, player1, player2, player3, player4, location);
        }

        private void DeleteMetCM()
        {
            string saveFilePath = string.Concat(new object[]
            {
                Custom.RootFolderDirectory(),
                Path.DirectorySeparatorChar,
                "metCM",
                ".txt"
            });

            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
        }

        //Starting pearl (TCoS Pearl)
        private void hook_Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            if (self.slugcatStats.name == KABase.Pathfinder)
            {
                AbstractPhysicalObject apo = new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, self.abstractCreature.pos, world.game.GetNewID(), -1, -1, null, KABase.KarmaAppetiteEnums.KAType.TCoSPearl);
                self.objectInStomach = apo;
            }
        }


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

        private Color overseerColor = new Color(0.4f, 0.3f, 0.2f);
        private Color overseerMyceliumColor = new Color(0.9f, 0.44f, 0f);
        private void hook_OverseerGraphics_ctor(On.OverseerGraphics.orig_ctor orig, OverseerGraphics self, PhysicalObject ow)
        {
            orig.Invoke(self, ow);
            self.myceliaColor = overseerMyceliumColor;
            for (int i = 0; i < self.mycelia.Length; i++)
            {
                self.mycelia[i].color = overseerMyceliumColor;
            }
        }

        private void hook_OverseerGraphics_DrawSprites(On.OverseerGraphics.orig_DrawSprites orig, OverseerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if ((self.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator == 6)
            {
                sLeaser.sprites[self.GlowSprite].color = overseerColor;
            }
            sLeaser.sprites[self.WhiteSprite].color = Color.Lerp(self.ColorOfSegment(0.75f, timeStacker), overseerMyceliumColor, 0.5f);
        }

        private Color hook_OverseerGraphics_ColorOfSegment(On.OverseerGraphics.orig_ColorOfSegment orig, OverseerGraphics self, float f, float timeStacker)
        {
            Color orig_result = orig.Invoke(self, f, timeStacker);
            if ((self.overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator == 6)
            {
                return Color.Lerp(Color.Lerp(Custom.RGB2RGBA((overseerColor + new Color(0f, 0f, 1f) + self.earthColor * 8f) / 10f, 0.5f), Color.Lerp(overseerColor, Color.Lerp(self.NeutralColor, self.earthColor, Mathf.Pow(f, 2f)), self.overseer.SandboxOverseer ? 0.15f : 0.5f), self.ExtensionOfSegment(f, timeStacker)), Custom.RGB2RGBA(overseerColor, 0f), Mathf.Lerp(self.overseer.lastDying, self.overseer.dying, timeStacker));
            }
            return orig_result;
        }

        private void hook_CoralBrain_Mycelium_UpdateColor(On.CoralBrain.Mycelium.orig_UpdateColor orig, CoralBrain.Mycelium self, Color newColor, float gradientStart, int spr, RoomCamera.SpriteLeaser sLeaser)
        {
            orig.Invoke(self, newColor, gradientStart, spr, sLeaser);
            bool inSZ = self.owner.OwnerRoom.abstractRoom.subregionName == "Solemn Quarry" || self.owner.OwnerRoom.abstractRoom.subregionName == "Two Colors of Smoke";
            bool isOverseer = self.owner is OverseerGraphics && (self.owner as OverseerGraphics).overseer.abstractCreature.abstractAI is OverseerAbstractAI;
            if ((inSZ && !isOverseer) || (isOverseer && ((self.owner as OverseerGraphics).overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator == 6))
            {
                self.color = overseerColor;
                for (int j = 1; j < 3; j++)
                {
                    (sLeaser.sprites[spr] as TriangleMesh).verticeColors[(sLeaser.sprites[spr] as TriangleMesh).verticeColors.Length - j] = overseerMyceliumColor;
                }
            }
        }

        private void hook_OverseerGraphics_HologramMatrix_DrawSprites(On.OverseerGraphics.HologramMatrix.orig_DrawSprites orig, OverseerGraphics.HologramMatrix self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            for (int j = 0; j < self.totalSprites; j++)
            {
                sLeaser.sprites[self.firstSprite + j].color = overseerColor;
            }
        }

        private void hook_WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            orig.Invoke(self, fresh);
            if (self.world.region.name == "SZ")
            {
                int num2 = UnityEngine.Random.Range(self.world.region.regionParams.overseersMin, self.world.region.regionParams.overseersMax);
                for (int num3 = 0; num3 < num2; num3++)
                {
                    AbstractCreature ac = new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, new WorldCoordinate(self.world.offScreenDen.index, -1, -1, 0), self.game.GetNewID());
                    (ac.abstractAI as OverseerAbstractAI).ownerIterator = 6;
                    self.world.offScreenDen.entitiesInDens.Add(ac);
                }
            }
        }
        private void hook_OverseerAbstractAI_AbstractBehavior(On.OverseerAbstractAI.orig_AbstractBehavior orig, OverseerAbstractAI self, int time)
        {
            if (self.world.game.Players.Count > 0 && self.RelevantPlayer != null)
            {
                if (self.RelevantPlayer.Room.name == "SZ_D01" && !self.goToPlayer && !self.playerGuide)
                {
                    self.goToPlayer = true;
                    if (ModManager.MMF)
                    {
                        self.SetTargetCreature(self.RelevantPlayer);
                    }
                    else
                    {
                        self.targetCreature = self.RelevantPlayer;
                    }
                    //self.playerGuideCounter = 1000;
                }
            }

            orig.Invoke(self, time);
        }

        private List<string> hook_OWAI_DirectionFinder_StoryRegionPrioritys(On.OverseersWorldAI.DirectionFinder.orig_StoryRegionPrioritys orig, OverseersWorldAI.DirectionFinder self, SlugcatStats.Name saveStateNumber, string currentRegion, bool metMoon, bool metPebbles)
        {
            List<string> list = orig.Invoke(self, saveStateNumber, currentRegion, metMoon, metPebbles);
            if (saveStateNumber == KABase.Pathfinder)
            {
                //if (true) //v-pearl swallow/grab check
                //{
                    list = new List<string>
                    {
                        "SZ",
                        "LF",
                        "SB",
                        "SI",
                        "DS",
                        "SU",
                        "VS",
                        "DM",
                        "SL",
                        "GW",
                        "SH",
                        "HI",
                        "CC",
                        "UW",
                        "RM"
                    };
                /*}
                else
                {
                    list = new List<string>
                    {
                        "SZ",
                        "RM",
                        "UW",
                        "CC",
                        "HI",
                        "SH",
                        "GW",
                        "SL",
                        "DM",
                        "VS",
                        "SU",
                        "DS",
                        "SI",
                        "LF",
                        "SB"
                    };
                }*/
            }
            return list;
        }

        private string hook_OWAI_DirectionFinder_StoryRoomInRegion(On.OverseersWorldAI.DirectionFinder.orig_StoryRoomInRegion orig, OverseersWorldAI.DirectionFinder self, string currentRegion, bool metMoon)
        {
            string orig_result = orig.Invoke(self, currentRegion, metMoon);
            if (currentRegion == "SZ")
            {
                return "SZ_D01";
            }
            else if (currentRegion == "RM")
            {
                return "RM_COREPF";
            }
            return orig_result;
        }

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