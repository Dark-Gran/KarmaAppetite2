using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BepInEx;
using MonoMod.RuntimeDetour;
using MonoMod;
using UnityEngine;
using UnityEngine.PlayerLoop;
using On;
using IL;
using DevInterface;
using RWCustom;
using HUD;
using SlugBase.Features;
using MoreSlugcats;
using SlugBase;
using SlugBase.DataTypes;
using static KarmaAppetite.KABase;
using static KarmaAppetite.KAWorld.SXOracleBehavior;


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
            //Iterator - Thanks to IteratorKit mod! (github.com/Twofour2/IteratorKit)
            OracleHooks();
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

        private Color overseerColor = new Color(0.7f, 0.6f, 0.5f);
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
            bool inSX = self.owner.OwnerRoom.abstractRoom.subregionName == "Solemn Quarry" || self.owner.OwnerRoom.abstractRoom.subregionName == "Two Colors of Smoke";
            bool isOverseer = self.owner is OverseerGraphics && (self.owner as OverseerGraphics).overseer.abstractCreature.abstractAI is OverseerAbstractAI;
            if ((inSX && !isOverseer) || (isOverseer && ((self.owner as OverseerGraphics).overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator == 6))
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
            if (self.world.region.name == "SX")
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
                if (self.RelevantPlayer.Room.name == "SX_D01" && !self.goToPlayer && !self.playerGuide)
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
                        "SX",
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
                        "SX",
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
            if (currentRegion == "SX")
            {
                return "SX_D01";
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



        //
        //-----------------------------------------ITERATOR-----------------------------------------
        //
        //Thanks to IteratorKit mod! (github.com/Twofour2/IteratorKit)


        private void OracleHooks()
        {
            On.Room.ReadyForAI += SpawnOracle;
            On.OracleGraphics.Gown.Color += SXOracleGraphics.SRSGown.SRSColor;
            On.Oracle.Update += SXOracle.Update;
            On.Oracle.OracleArm.Update += SXOracleArm.ArmUpdate;
            On.Oracle.SetUpSwarmers += SXOracle.SetUpSwarmers;
        }

        //Oracle General

        public SXOracle oracle;
        public static float talkHeight = 0f;

        private void SpawnOracle(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            if (self.game == null)
            {
                return;
            }

            //Spawn Iterator
            if (self.abstractRoom.name == "SX_AI")
            {
                self.loadingProgress = 3;
                self.readyForNonAICreaturesToEnter = true;
                WorldCoordinate worldCoordinate = new WorldCoordinate(self.abstractRoom.index, 15, 15, -1);
                AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(self.world, global::AbstractPhysicalObject.AbstractObjectType.Oracle, null, worldCoordinate, self.game.GetNewID());
                oracle = new SXOracle(abstractPhysicalObject, self);
                self.AddObject(oracle);
                self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 20);
            }

        }

        public class SXOracle : Oracle
        {
            public SXOracle(AbstractPhysicalObject abstractPhysicalObject, Room room) : base(abstractPhysicalObject, room)
            {

                this.room = room;
                base.bodyChunks = new BodyChunk[2];

                this.mySwarmers = new List<OracleSwarmer>();
                base.airFriction = 0.99f;
                base.gravity = 0.9f;
                this.bounce = 0.1f;
                this.surfaceFriction = 0.17f;
                this.collisionLayer = 1;
                base.waterFriction = 0.92f;
                this.health = 10f;
                this.stun = 0;
                base.buoyancy = 0.95f;
                this.ID = KABase.KarmaAppetiteEnums.KAType.SX;
                for (int k = 0; k < base.bodyChunks.Length; k++)
                {
                    Vector2 pos = new Vector2(350f, 350f);
                    base.bodyChunks[k] = new BodyChunk(this, k, pos, 6f, 0.5f);

                }
                this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[1];
                this.bodyChunkConnections[0] = new PhysicalObject.BodyChunkConnection(base.bodyChunks[0], base.bodyChunks[1], 9f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f);
                this.mySwarmers = new List<OracleSwarmer>();
                //base.airFriction = 0.99f;


                this.oracleBehavior = new SXOracleBehavior(this);
                this.arm = new SXOracleArm(this);
            }

            public static void Update(On.Oracle.orig_Update orig, Oracle self, bool eu)
            {
                if (self is SXOracle)
                {
                    SXOracle sxOracle = (SXOracle)self;
                    OracleArm tmpOracleArm = self.arm;
                    float tmpHealth = self.health;
                    self.arm = null;
                    self.health = -10;
                    orig(self, eu);
                    self.arm = tmpOracleArm;
                    self.health = tmpHealth;
                    if (self.Consious)
                    {
                        self.behaviorTime++;
                        sxOracle.oracleBehavior.Update(eu);
                    }

                    if (self.arm != null)
                    {
                        sxOracle.arm.Update();
                    }

                }
                else
                {
                    orig(self, eu);
                }
            }

            public override void InitiateGraphicsModule()
            {
                if (base.graphicsModule == null)
                {
                    base.graphicsModule = new SXOracleGraphics(this, this);
                }
            }

            public static void SetUpSwarmers(On.Oracle.orig_SetUpSwarmers orig, Oracle self)
            {
                if (self is SXOracle)
                {
                    return;
                }
                else
                {
                    orig(self);
                    return;
                }
            }

            public override void HitByWeapon(Weapon weapon)
            {
                base.HitByWeapon(weapon);
                (this.oracleBehavior as SXOracleBehavior).ReactToHitByWeapon(weapon);
            }

        }

        //Oracle Arm

        public class SXOracleArm : Oracle.OracleArm
        {

            public SXOracleArm(SXOracle oracle) : base(oracle)
            {
                this.oracle = oracle;
                this.baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);

                this.cornerPositions = new Vector2[4];

                this.cornerPositions[0] = oracle.room.MiddleOfTile(7, 35);
                this.cornerPositions[1] = oracle.room.MiddleOfTile(35, 35);
                this.cornerPositions[2] = oracle.room.MiddleOfTile(35, 5);
                this.cornerPositions[3] = oracle.room.MiddleOfTile(7, 5);

                this.joints = new Oracle.OracleArm.Joint[4];
                for (int k = 0; k < this.joints.Length; k++)
                {
                    this.joints[k] = new Oracle.OracleArm.Joint(this, k);
                    if (k > 0)
                    {
                        this.joints[k].previous = this.joints[k - 1];
                        this.joints[k - 1].next = this.joints[k];
                    }
                }
                this.framePos = 10002.5f;
                this.lastFramePos = this.framePos;

            }

            public static void ArmUpdate(On.Oracle.OracleArm.orig_Update orig, Oracle.OracleArm self)
            {
                if (self.oracle is SXOracle)
                {
                    SXOracle cMOracle = (SXOracle)self.oracle;
                    float num = 1f / 240f;
                    self.oracle.bodyChunks[1].vel *= 0.4f;
                    self.oracle.bodyChunks[0].vel *= 0.4f;
                    self.oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(cMOracle.oracleBehavior.OracleGetToPos - cMOracle.bodyChunks[0].pos, 100f) / 100f * 6.2f;
                    self.oracle.bodyChunks[1].vel += Vector2.ClampMagnitude(cMOracle.oracleBehavior.OracleGetToPos - cMOracle.oracleBehavior.GetToDir * cMOracle.bodyChunkConnections[0].distance - cMOracle.bodyChunks[0].pos, 100f) / 100f * 3.2f * num;

                    Vector2 baseGetToPos = cMOracle.oracleBehavior.BaseGetToPos;

                    Vector2 vector = new Vector2(Mathf.Clamp(baseGetToPos.x, self.cornerPositions[0].x, self.cornerPositions[1].x), self.cornerPositions[0].y);

                    float num2 = Vector2.Distance(vector, baseGetToPos);
                    float num3 = Mathf.InverseLerp(self.cornerPositions[0].x, self.cornerPositions[1].x, baseGetToPos.x);

                    self.baseMoving = (Vector2.Distance(self.BasePos(1f), vector) > (self.baseMoving ? 50f : 350f) && cMOracle.oracleBehavior.consistentBasePosCounter > 30);
                    self.lastFramePos = self.framePos;
                    if (self.baseMoving)
                    {

                        self.framePos = Mathf.MoveTowardsAngle(self.framePos * 90f, num3 * 90f, 1f) / 90f;
                        if (self.baseMoveSoundLoop != null)
                        {
                            self.baseMoveSoundLoop.volume = Mathf.Min(self.baseMoveSoundLoop.volume + 0.1f, 1f);
                            self.baseMoveSoundLoop.pitch = Mathf.Min(self.baseMoveSoundLoop.pitch + 0.025f, 1f);
                        }
                    }
                    else if (self.baseMoveSoundLoop != null)
                    {
                        self.baseMoveSoundLoop.volume = Mathf.Max(self.baseMoveSoundLoop.volume - 0.1f, 0f);
                        self.baseMoveSoundLoop.pitch = Mathf.Max(self.baseMoveSoundLoop.pitch - 0.025f, 0.5f);
                    }

                    if (self.baseMoveSoundLoop != null)
                    {
                        self.baseMoveSoundLoop.pos = self.BasePos(1f);
                        self.baseMoveSoundLoop.Update();
                        if (ModManager.MSC)
                        {
                            self.baseMoveSoundLoop.volume *= 1f - self.oracle.noiseSuppress;
                        }
                    }
                    self.oracle.ID = Oracle.OracleID.SS; // force use pebbles joints code, avoids rewriting it
                    for (int j = 0; j < self.joints.Length; j++)
                    {
                        self.joints[j].Update();
                    }
                    self.oracle.ID = KABase.KarmaAppetiteEnums.KAType.SX; // set back
                }
                else
                {
                    orig(self);
                }
            }


        }

        //Oracle Graphics

        public class SXOracleGraphics : OracleGraphics
        {
            public new SXOracle oracle
            {
                get
                {
                    return base.owner as SXOracle;
                }
            }
            public bool IsSuns = true;

            public int sigilSprite;

            public int sunFinL, sunFinR;

            public static ArmBase staticCheckArmBase;

            public SXOracleGraphics(PhysicalObject ow, SXOracle oracle) : base(ow)
            {

                UnityEngine.Random.State state = UnityEngine.Random.state;
                UnityEngine.Random.InitState(10544);
                this.totalSprites = 0;
                this.armJointGraphics = new OracleGraphics.ArmJointGraphics[this.oracle.arm.joints.Length];
                for (int i = 0; i < this.oracle.arm.joints.Length; i++)
                {
                    this.armJointGraphics[i] = new SXOracleGraphics.ArmJointGraphics(this, this.oracle.arm.joints[i], this.totalSprites);
                    this.totalSprites += this.armJointGraphics[i].totalSprites;
                }

                this.firstUmbilicalSprite = this.totalSprites;
                this.umbCord = new OracleGraphics.UbilicalCord(this, this.totalSprites);
                this.totalSprites += this.umbCord.totalSprites;

                this.firstBodyChunkSprite = this.totalSprites;
                this.totalSprites += 2;
                this.neckSprite = this.totalSprites;
                this.totalSprites++;
                this.firstFootSprite = this.totalSprites;
                this.totalSprites += 4;

                this.halo = null;// new OracleGraphics.Halo(this, this.totalSprites);

                /*if (this.bodyJson.gown != null)
                {
                    this.gown = new OracleGraphics.Gown(this);
                    this.robeSprite = this.totalSprites;
                    this.totalSprites++;
                }
                else
                {*/
                this.gown = null;
                //}

                this.firstHandSprite = this.totalSprites;
                this.totalSprites += 4;
                this.head = new GenericBodyPart(this, 5f, 0.5f, 0.995f, this.oracle.firstChunk);
                this.firstHeadSprite = this.totalSprites;
                this.totalSprites += 10;
                this.fadeSprite = this.totalSprites;
                this.totalSprites++;


                this.hands = new GenericBodyPart[2];
                for (int i = 0; i < 2; i++)
                {
                    this.hands[i] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, this.oracle.firstChunk);
                }
                this.feet = new GenericBodyPart[2];
                for (int i = 0; i < 2; i++)
                {
                    this.feet[i] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, this.oracle.firstChunk);
                }

                this.knees = new Vector2[2, 2];
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        this.knees[i, j] = this.oracle.firstChunk.pos;
                    }
                }

                this.firstArmBaseSprite = this.totalSprites;
                this.armBase = new OracleGraphics.ArmBase(this, this.firstArmBaseSprite);
                staticCheckArmBase = this.armBase;
                this.totalSprites += this.armBase.totalSprites;
                this.voiceFreqSamples = new float[64];
                this.eyesOpen = 1f;
                UnityEngine.Random.state = state;


            }

            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                base.ApplyPalette(sLeaser, rCam, palette);
                this.SLArmBaseColA = new Color(0.52156866f, 0.52156866f, 0.5137255f);
                this.SLArmHighLightColA = new Color(0.5686275f, 0.5686275f, 0.54901963f);
                this.SLArmBaseColB = palette.texture.GetPixel(5, 1);
                this.SLArmHighLightColB = palette.texture.GetPixel(5, 2);

                Color oracleColor = Color.yellow;
                for (int j = 0; j < base.owner.bodyChunks.Length; j++)
                {
                    sLeaser.sprites[this.firstBodyChunkSprite + j].color = oracleColor; //torso
                }
                sLeaser.sprites[this.neckSprite].color = oracleColor;
                sLeaser.sprites[this.HeadSprite].color = oracleColor;
                sLeaser.sprites[this.ChinSprite].color = oracleColor;

                for (int k = 0; k < 2; k++)
                {
                    if (this.armJointGraphics.Length == 0)
                    {
                        sLeaser.sprites[this.PhoneSprite(k, 0)].color = this.GenericJointBaseColor();
                        sLeaser.sprites[this.PhoneSprite(k, 1)].color = this.GenericJointHighLightColor();
                        sLeaser.sprites[this.PhoneSprite(k, 2)].color = this.GenericJointHighLightColor();
                    }
                    else
                    {
                        sLeaser.sprites[this.PhoneSprite(k, 0)].color = this.armJointGraphics[0].BaseColor(default(Vector2));
                        sLeaser.sprites[this.PhoneSprite(k, 1)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                        sLeaser.sprites[this.PhoneSprite(k, 2)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                    }
                    sLeaser.sprites[this.HandSprite(k, 0)].color = oracleColor;
                    if (this.gown != null)
                    {
                        for (int l = 0; l < 7; l++)
                        {
                            (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4] = this.gown.Color(0.4f);
                            (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = this.gown.Color(0f);
                            (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = this.gown.Color(0.4f);
                            (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = this.gown.Color(0f);
                        }
                    }
                    else
                    {
                        sLeaser.sprites[this.HandSprite(k, 1)].color = oracleColor;
                    }
                    sLeaser.sprites[this.FootSprite(k, 0)].color = oracleColor;
                    sLeaser.sprites[this.FootSprite(k, 1)].color = oracleColor;

                    sLeaser.sprites[this.EyeSprite(k)].color = new Color(0f, 0f, 0f);

                }


                sLeaser.sprites[this.sunFinL].color = new Color(1f, 0f, 0f);
                sLeaser.sprites[this.sunFinR].color = new Color(1f, 0f, 0f);

                if (this.umbCord != null)
                {
                    this.umbCord.ApplyPalette(sLeaser, rCam, palette);
                    sLeaser.sprites[this.firstUmbilicalSprite].color = palette.blackColor;
                }
                if (this.armBase != null)
                {
                    this.armBase.ApplyPalette(sLeaser, rCam, palette);
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                // Futile.atlasManager.LogAllElementNames();
                sLeaser.sprites = new FSprite[this.totalSprites];
                for (int i = 0; i < base.owner.bodyChunks.Length; i++)
                {
                    sLeaser.sprites[this.firstBodyChunkSprite + i] = new FSprite("Circle20", true);
                    sLeaser.sprites[this.firstBodyChunkSprite + i].scale = base.owner.bodyChunks[i].rad / 10f;
                    sLeaser.sprites[this.firstBodyChunkSprite + i].color = new Color(1f, (i == 0) ? 0.5f : 0f, (i == 0) ? 0.5f : 0f);

                }
                for (int j = 0; j < this.armJointGraphics.Length; j++)
                {
                    this.armJointGraphics[j].InitiateSprites(sLeaser, rCam);
                }
                if (this.gown != null)
                {
                    this.gown.InitiateSprite(this.robeSprite, sLeaser, rCam);
                }
                if (this.halo != null)
                {
                    this.halo.InitiateSprites(sLeaser, rCam);
                }
                if (this.armBase != null)
                {
                    this.armBase.InitiateSprites(sLeaser, rCam);
                }
                sLeaser.sprites[this.neckSprite] = new FSprite("pixel", true);
                sLeaser.sprites[this.neckSprite].scaleX = 3f;
                sLeaser.sprites[this.neckSprite].anchorY = 0f;
                sLeaser.sprites[this.HeadSprite] = new FSprite("Circle20", true);
                sLeaser.sprites[this.ChinSprite] = new FSprite("Circle20", true);
                for (int k = 0; k < 2; k++)
                {
                    sLeaser.sprites[this.EyeSprite(k)] = new FSprite("pixel", true);
                    sLeaser.sprites[this.EyeSprite(k)].color = new Color(0.02f, 0f, 0f);

                    sLeaser.sprites[this.PhoneSprite(k, 0)] = new FSprite("Circle20", true);
                    sLeaser.sprites[this.PhoneSprite(k, 1)] = new FSprite("Circle20", true);
                    sLeaser.sprites[this.PhoneSprite(k, 2)] = new FSprite("LizardScaleA1", true);
                    sLeaser.sprites[this.PhoneSprite(k, 2)].anchorY = 0f;
                    sLeaser.sprites[this.PhoneSprite(k, 2)].scaleY = 0.8f;
                    sLeaser.sprites[this.PhoneSprite(k, 2)].scaleX = ((k == 0) ? -1f : 1f) * 0.75f;
                    sLeaser.sprites[this.HandSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                    sLeaser.sprites[this.HandSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                    sLeaser.sprites[this.FootSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                    sLeaser.sprites[this.FootSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                }

                if (this.umbCord != null)
                {
                    this.umbCord.InitiateSprites(sLeaser, rCam);
                }
                else if (this.discUmbCord != null)
                {
                    this.discUmbCord.InitiateSprites(sLeaser, rCam);
                }

                sLeaser.sprites[this.HeadSprite].scaleX = this.head.rad / 9f;
                sLeaser.sprites[this.HeadSprite].scaleY = this.head.rad / 11f;
                sLeaser.sprites[this.ChinSprite].scale = this.head.rad / 15f;
                sLeaser.sprites[this.fadeSprite] = new FSprite("Futile_White", true);
                sLeaser.sprites[this.fadeSprite].scale = 12.5f;
                sLeaser.sprites[this.fadeSprite].color = new Color(0f, 0f, 0f);
                sLeaser.sprites[this.fadeSprite].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
                sLeaser.sprites[this.fadeSprite].alpha = 0.2f;

                sLeaser.sprites[this.killSprite] = new FSprite("Futile_White", true);
                sLeaser.sprites[this.killSprite].shader = rCam.game.rainWorld.Shaders["FlatLight"];

                this.AddToContainer(sLeaser, rCam, null);

            }

            public override void Update()
            {
                Room tmpRoom = this.oracle.room;

                this.oracle.room = null;// hide so base.Update() doesnt do anything aside from calling base.Update(), this has a side effect of that base.Update not having access to oracle.room but I dont think it uses it
                base.Update();
                this.oracle.room = tmpRoom;

                if (this.oracle == null || this.oracle.room == null)
                {
                    return;
                }

                this.breathe += 1f / Mathf.Lerp(10f, 60f, this.oracle.health);
                this.lastBreatheFac = this.breathFac;
                this.breathFac = Mathf.Lerp(0.5f + 0.5f * Mathf.Sin(this.breathe * 3.1415927f * 2f), 1f, Mathf.Pow(this.oracle.health, 2f));

                if (this.gown != null)
                {
                    this.gown.Update();
                }

                if (this.armBase != null)
                {
                    this.armBase.Update();
                }
                // may want to add flag?
                this.lastLookDir = this.lookDir;
                if (this.oracle.Consious)
                {
                    Vector2 tmpVector2 = Vector2.ClampMagnitude(this.oracle.oracleBehavior.lookPoint - this.oracle.firstChunk.pos, 100f) / 100f;
                    this.lookDir = Vector2.ClampMagnitude(tmpVector2 + this.randomTalkVector * this.averageVoice * 0.3f, 1f);

                }

                this.head.Update();
                this.head.ConnectToPoint(this.oracle.firstChunk.pos + Custom.DirVec(this.oracle.bodyChunks[1].pos, this.oracle.firstChunk.pos) * 6f, 8f, true, 0f, this.oracle.firstChunk.vel, 0.5f, 0.01f);
                var torso = this.oracle.bodyChunks[1]; // is torso-ish i guess

                if (this.oracle.Consious)
                {
                    this.head.vel += Custom.DirVec(torso.pos, this.oracle.firstChunk.pos) * this.breathFac;
                    this.head.vel += this.lookDir * 0.5f * this.breathFac;
                }
                else
                {
                    this.head.vel += Custom.DirVec(torso.pos, this.oracle.firstChunk.pos) * 0.75f;
                    this.head.vel.y = this.head.vel.y - 0.7f;
                }

                for (int i = 0; i < 2; i++)
                {
                    var foot = this.feet[i];
                    foot.Update();
                    foot.ConnectToPoint(torso.pos, 10f, false, 0f, torso.vel, 0.3f, 0.01f);
                    foot.vel += Custom.DirVec(this.oracle.firstChunk.pos, torso.pos) * 0.3f;
                    foot.vel += Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 0.15f * ((i == 0) ? -1f : 1f);

                    var hand = this.hands[i];
                    hand.Update();
                    hand.ConnectToPoint(this.oracle.firstChunk.pos, 15f, false, 0f, this.oracle.firstChunk.vel, 0.3f, 0.01f);
                    hand.vel.y = hand.vel.y - 0.5f;

                    hand.vel += Custom.DirVec(this.oracle.firstChunk.pos, torso.pos) * 0.3f;
                    hand.vel += Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 0.3f * ((i == 0) ? -1f : 1f);
                    this.knees[i, 1] = this.knees[i, 0];

                    hand.vel += this.randomTalkVector * this.averageVoice * 0.8f;
                    if (this.oracle.oracleBehavior.player != null && i == 0 && false)
                    {
                        // <--- hand towards player stuff goes here! must also fix above cond.

                    }
                    this.knees[i, 1] = this.knees[i, 0];
                    this.knees[i, 0] = (foot.pos + torso.pos) / 2f +
                    Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 4f * ((i == 0) ? -1f : 1f);
                    // TestMod.LogVector2(this.knees[i, 0]);
                    // after end of big if block

                    for (int j = 0; j < this.armJointGraphics.Length; j++)
                    {
                        this.armJointGraphics[j].Update();
                    }
                    if (this.umbCord != null)
                    {
                        this.umbCord.Update();
                    }

                    // voice?
                }

            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                //this.armBase.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                // this function lets the orig draw sprites function do its thing, then we fix its issues here
                // sLeaser.sprites[this.killSprite].isVisible = false;
                Vector2 sunSpritePos = new Vector2(sLeaser.sprites[this.firstHeadSprite].x, sLeaser.sprites[this.firstHeadSprite].y);


                Vector2 bodyVector = Vector2.Lerp(base.owner.firstChunk.lastPos, base.owner.firstChunk.pos, timeStacker);
                Vector2 headVector = Vector2.Lerp(this.head.lastPos, this.head.pos, timeStacker);
                Vector2 vector6 = Custom.DirVec(headVector, bodyVector); // subtracts both vectors?
                Vector2 vector7 = Custom.PerpendicularVector(vector6); // flips vector across-wise (horizontally?)
                Vector2 lookVector = this.RelativeLookDir(timeStacker); // direction oracle is looking

                // moon sigil graphic scale y works because it is above the zero point of the lookVector.y (aka middle of oracle head)
                // we offset the scaleY calcs by 1f so we get what we want

                // sLeaser.sprites[this.sunFinL].x = (sunVector.x - camPos.x);
                // sLeaser.sprites[this.sunFinL].y = (sunVector.y - camPos.y) + 1f;
                // sLeaser.sprites[this.sunFinL].rotation = Custom.AimFromOneVectorToAnother(sunVector, headVector - vector6 * 10f);
                //// sLeaser.sprites[this.sunFinL].scaleX = Custom.LerpMap(lookVector.x + 0.5f, 0.8f, 0f, 0.4f, 0f);
                // //TestMod.Logger.LogWarning(lookVector.x);
                //// sLeaser.sprites[this.sunFinL].scaleY = Custom.LerpMap(lookVector.y + 0.2f, 0.8f, 0f, 1f, 0f); 

                // sLeaser.sprites[this.sunFinR].x = (sunVector.x - camPos.x);
                // sLeaser.sprites[this.sunFinR].y = (sunVector.y - camPos.y) + 1f;
                // sLeaser.sprites[this.sunFinR].rotation = Custom.AimFromOneVectorToAnother(sunVector, headVector - vector6 * 10f);
                // sLeaser.sprites[this.sunFinR].scaleX = -Mathf.Lerp(1f, 0f, lookVector.x);
                // float scaleY = Mathf.Lerp(0f, 1f, lookVector.y + 0.5f);
                // sLeaser.sprites[this.sunFinR].scaleY = (scaleY >= 0f) ? scaleY : 0f;
                // TestMod.Logger.LogWarning(lookVector.y);
                // TestMod.Logger.LogWarning(scaleY);

                // looking up
                // 1f = full scale 1f
                // 0.9f = 

            }

            public class SRSGown
            {
                public static Color SRSColor(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
                {
                    Color origRes = orig(self, f);

                    if (self.owner.oracle is SXOracle)
                    {
                        SXOracle cmOracle = (SXOracle)self.owner.oracle;

                        //gradient
                        /*return Custom.HSL2RGB(
                            Mathf.Lerp(gownColor.from.h, gownColor.to.h, Mathf.Pow(f, 2f)),
                            Mathf.Lerp(gownColor.from.s, gownColor.to.s, f),
                            Mathf.Lerp(gownColor.from.s, gownColor.to.s, f)
                        );*/
                        return Color.black;

                    }
                    else
                    {
                        return origRes;
                    }

                }
            }
        }

        //Oracle Behavior

        public class SXOracleBehavior : OracleBehavior, Conversation.IOwnAConversation
        {
            public Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal;

            public float pathProgression, investigateAngle, invstAngSped, working, getToWorking, discoverCounter, killFac, lastKillFac;

            public bool floatyMovement, hasNoticedPlayer, rainInterrupt;

            public int throwOutCounter, playerOutOfRoomCounter;
            public int sayHelloDelay = -1;
            public int timeSinceSeenPlayer = 0;

            public bool forceGravity;
            public float roomGravity; // enable force gravity to use

            public SXOracleMovement movementBehavior;


            //public List<CMOracleSubBehavior> allSubBehaviors;
            //public CMOracleSubBehavior currSubBehavior;

            public new SXOracle oracle;

            public DataPearl inspectPearl;
            public SXConversation conversation = null;

            public SXOracleAction action;
            public string actionParam = null;

            public int playerScore;
            public bool oracleAngry = false;
            public bool oracleAnnoyed = false;
            public SXConversation conversationResumeTo;
            public List<EntityID> alreadyDiscussedItems = new List<EntityID>();

            public enum SXOracleAction
            {
                generalIdle,
                giveMark,
                giveKarma,
                giveMaxKarma,
                giveFood,
                startPlayerConversation,
                kickPlayerOut,
                killPlayer,
            }

            public enum SXOracleMovement
            {
                idle,
                meditate,
                investigate,
                keepDistance,
                talk
            }


            public override DialogBox dialogBox
            {
                get
                {
                    if (this.oracle.room.game.cameras[0].hud.dialogBox == null)
                    {
                        this.oracle.room.game.cameras[0].hud.InitDialogBox();
                        this.oracle.room.game.cameras[0].hud.dialogBox.defaultYPos = -10f;
                    }
                    return this.oracle.room.game.cameras[0].hud.dialogBox;
                }
            }

            public SXOracleBehavior(SXOracle oracle) : base(oracle)
            {
                this.oracle = oracle;
                this.currentGetTo = oracle.firstChunk.pos;
                this.lastPos = oracle.firstChunk.pos;
                this.nextPos = oracle.firstChunk.pos;
                this.pathProgression = 1f;
                //this.allSubBehaviors = new List<CMOracleSubBehavior>();
                //this.currSubBehavior = new CMOracleSubBehavior.NoSubBehavior(this);

                this.investigateAngle = UnityEngine.Random.value * 360f;
                this.working = 1f;
                this.getToWorking = 1f;
                this.movementBehavior = SXOracleMovement.idle;
                this.action = SXOracleAction.generalIdle;
                this.playerOutOfRoomCounter = 1;

                // move?
                this.SetNewDestination(this.oracle.room.RandomPos()); //startPos
                
                this.investigateAngle = 0f;
                this.lookPoint = this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);

            }

            public override void Update(bool eu)
            {
                // for the most part seems to handle changing states, i.e if player enters the room
                this.Move();
                base.Update(eu);

                this.pathProgression = Mathf.Min(1f, this.pathProgression + 1f / Mathf.Lerp(40f + this.pathProgression * 80f, Vector2.Distance(this.lastPos, this.nextPos) / 5f, 0.5f));

                this.currentGetTo = Custom.Bezier(this.lastPos, this.ClampToRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampToRoom(this.nextPos + this.nextPosHandle), this.pathProgression);


                this.floatyMovement = false;

                if (this.pathProgression >= 1f && this.consistentBasePosCounter > 100 && !this.oracle.arm.baseMoving)
                {
                    this.allStillCounter++;
                }
                else
                {
                    this.allStillCounter = 0;
                }

                this.inActionCounter++;
                CheckActions(); // runs actions like giveMark. moved out of update to avoid mess. 

                // look at player
                this.lookPoint = this.player.firstChunk.pos;

                // pearl code
                if (this.inspectPearl == null)
                {

                }

                if (this.player != null && this.player.room == this.oracle.room)
                {
                    this.hasNoticedPlayer = true;


                    if (this.playerOutOfRoomCounter > 0)
                    {
                        // first seeing player
                        this.timeSinceSeenPlayer = 0;

                    }
                    this.playerOutOfRoomCounter = 0;
                }
                else
                {
                    this.playerOutOfRoomCounter++;
                }

                if (this.inspectPearl != null && this.conversation == null)
                {
                    this.StartItemConversation(this.inspectPearl);
                }

                if (this.player != null && this.player.room == this.oracle.room && this.conversation == null)
                {
                    List<PhysicalObject>[] physicalObjects = this.oracle.room.physicalObjects;
                    foreach (List<PhysicalObject> physicalObject in physicalObjects)
                    {

                        foreach (PhysicalObject physObject in physicalObject)
                        {
                            if (this.alreadyDiscussedItems.Contains(physObject.abstractPhysicalObject.ID))
                            {
                                continue;
                            }
                            this.alreadyDiscussedItems.Add(physObject.abstractPhysicalObject.ID);
                            if (this.inspectPearl == null && this.conversation == null && physObject is DataPearl)
                            {
                                DataPearl pearl = (DataPearl)physObject;
                                if (pearl.grabbedBy.Count == 0)
                                {
                                    this.inspectPearl = pearl;
                                }

                            }
                            else if (this.conversation == null)
                            {
                                SLOracleBehaviorHasMark.MiscItemType msc = new SLOracleBehaviorHasMark.MiscItemType("NA", false);
                                if (SLOracleBehaviorHasMark.MiscItemType.TryParse(msc.enumType, physicalObject.GetType().ToString(), true, out ExtEnumBase result))
                                {
                                    this.StartItemConversation(physObject);
                                }
                            }
                        }
                    }
                    CheckConversationEvents();

                }

                if (this.forceGravity == true)
                {
                    this.oracle.room.gravity = this.roomGravity;
                }



                if (this.conversation != null)
                {
                    // check if we are resuming
                    if (this.conversationResumeTo != null && this.conversation.events.Count <= 0)
                    {
                        if (this.oracleAngry)
                        {
                            this.conversationResumeTo = new SXConversation(this, SXConversation.SXDialogType.Generic, "oracleAngry");
                        }
                        else if (this.oracleAnnoyed) // todo: checks here to avoid overwriting important convos, although it really is the players choice in this case.
                        {
                            this.conversationResumeTo = new SXConversation(this, SXConversation.SXDialogType.Generic, "oracleAnnoyed");
                        }

                        this.conversation = this.conversationResumeTo;
                        this.conversation.RestartCurrent();
                        this.conversationResumeTo = null;
                    }

                    this.conversation.Update();
                }
                if (this.conversation != null && this.conversation.slatedForDeletion)
                {
                    this.inspectPearl = null;
                    this.conversation = null;
                }

            }

            public void Move()
            {
                switch (this.movementBehavior)
                {
                    case SXOracleMovement.idle:
                        // usually just looks at marbles, for now just sit still
                        break;
                    case SXOracleMovement.meditate:
                        //if (this.nextPos != this.oracle.room.MiddleOfTile(24, 17))
                        //{
                        //    this.SetNewDestination(this.oracle.room.MiddleOfTile(24, 17));
                        //}
                        this.investigateAngle = 0f;
                        this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
                        break;
                    //  TestMod.Logger.LogWarning(this.lookPoint);
                    case SXOracleMovement.investigate:
                        if (this.player == null)
                        {
                            this.movementBehavior = SXOracleMovement.idle;
                            break;
                        }
                        this.lookPoint = this.player.DangerPos;
                        if (this.investigateAngle < -90f || this.investigateAngle > 90f || (float)this.oracle.room.aimap.getAItile(this.nextPos).terrainProximity < 2f)
                        {
                            this.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
                            this.invstAngSped = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1 : 1f);
                        }
                        Vector2 getToVector = this.player.DangerPos + Custom.DegToVec(this.investigateAngle) * 150f;
                        if ((float)this.oracle.room.aimap.getAItile(getToVector).terrainProximity >= 2f)
                        {
                            if (this.pathProgression > 0.9f)
                            {
                                if (Custom.DistLess(this.oracle.firstChunk.pos, getToVector, 30f))
                                {
                                    this.floatyMovement = true;
                                }
                                else if (!Custom.DistLess(this.nextPos, getToVector, 30f))
                                {
                                    this.SetNewDestination(getToVector);
                                }
                            }
                            this.nextPos = getToVector;
                        }
                        break;
                    case SXOracleMovement.keepDistance:
                        if (this.player == null)
                        {
                            this.movementBehavior = SXOracleMovement.idle;
                        }
                        else
                        {
                            this.lookPoint = this.player.DangerPos;
                            Vector2 distancePoint = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                            if (!this.oracle.room.GetTile(distancePoint).Solid && this.oracle.room.aimap.getAItile(distancePoint).terrainProximity > 2
                                && Vector2.Distance(distancePoint, this.player.DangerPos) > Vector2.Distance(this.nextPos, this.player.DangerPos) + 100f)
                            {
                                this.SetNewDestination(distancePoint);
                            }
                        }
                        break;
                    case SXOracleMovement.talk:
                        if (this.player == null)
                        {
                            this.movementBehavior = SXOracleMovement.idle;
                        }
                        else
                        {
                            this.lookPoint = this.player.DangerPos;
                            Vector2 tryPos = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                            if (this.CommunicatePosScore(tryPos) + 40f < this.CommunicatePosScore(this.nextPos) && !Custom.DistLess(tryPos, this.nextPos, 30f))
                            {
                                this.SetNewDestination(tryPos);
                            }
                        }
                        break;
                }

                this.consistentBasePosCounter++;
                Vector2 vector2 = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                if (this.oracle.room.GetTile(vector2).Solid || this.BasePosScore(vector2) + 40.0 >= this.BasePosScore(this.baseIdeal))
                {
                    return;
                }
                this.baseIdeal = vector2;
                this.consistentBasePosCounter = 0;


            }

            public float CommunicatePosScore(Vector2 tryPos)
            {
                if (this.oracle.room.GetTile(tryPos).Solid || this.player == null)
                {
                    return float.MaxValue;
                }

                Vector2 dangerPos = this.player.DangerPos;
                //dangerPos *= talkHeight;
                float num = Vector2.Distance(tryPos, dangerPos);
                num -= (tryPos.x + KAWorld.talkHeight);
                num -= ((float)this.oracle.room.aimap.getAItile(tryPos).terrainProximity) * 10f;
                return num;
            }

            public float BasePosScore(Vector2 tryPos)
            {
                if (this.movementBehavior == SXOracleMovement.meditate || this.player == null)
                {
                    return Vector2.Distance(tryPos, this.oracle.room.MiddleOfTile(24, 5));
                }

                return Mathf.Abs(Vector2.Distance(this.nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(this.player.DangerPos, tryPos), 40f, 300f, 800f, 0f);
            }

            public void SetNewDestination(Vector2 dst)
            {
                this.lastPos = this.currentGetTo;
                this.nextPos = dst;
                this.lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
                this.nextPosHandle = -this.GetToDir * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
                this.pathProgression = 0f;
            }

            public Vector2 ClampToRoom(Vector2 vector)
            {
                vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
                vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
                return vector;
            }

            public Vector2 RandomRoomPoint()
            {
                return this.ClampToRoom(new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
            }

            public override Vector2 OracleGetToPos
            {
                get
                {
                    Vector2 v = this.currentGetTo;
                    if (this.floatyMovement && Custom.DistLess(this.oracle.firstChunk.pos, this.nextPos, 50f))
                    {
                        v = this.nextPos;
                    }
                    return this.ClampToRoom(v);
                }
            }

            public override Vector2 BaseGetToPos
            {
                get
                {
                    return this.baseIdeal;
                }
            }

            public override Vector2 GetToDir
            {
                get
                {
                    if (this.movementBehavior == SXOracleMovement.idle)
                    {
                        return Custom.DegToVec(this.investigateAngle);
                    }
                    if (this.movementBehavior == SXOracleMovement.keepDistance)
                    {
                        return Custom.DegToVec(this.investigateAngle);
                    }
                    return Custom.DegToVec(0);// new Vector2(0f, 1f);
                }
            }

            public void NewAction(SXOracleAction nextAction, string actionParam)
            {
                if (nextAction == this.action)
                {
                    return;
                }
                this.inActionCounter = 0;

                this.action = nextAction;
                this.actionParam = actionParam;
            }



            public enum Action
            {
                GeneralIdle,
                MeetPlayer
            }

            public enum MovementBehavior
            {
                Idle,
                Meditate,
                KeepDistance,
                Talk,
                Investigate,
                ShowMedia
            }


            public void CheckConversationEvents()
            {
                if (this.hasNoticedPlayer)
                {
                    if (this.sayHelloDelay < 0 && this.oracle.room.world.rainCycle.TimeUntilRain + this.oracle.room.world.rainCycle.pause > 2000)
                    {
                        this.sayHelloDelay = 30;
                    }
                    else
                    {
                        if (this.sayHelloDelay > 0)
                        {
                            this.sayHelloDelay--;
                        }
                        if (this.sayHelloDelay == 1)
                        {
                            this.oracle.room.game.cameras[0].EnterCutsceneMode(this.player.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
                            // now we can start calling player dialogs!
                            this.conversation = new SXConversation(this, SXConversation.SXDialogType.Generic, "playerEnter");

                        }
                    }
                    if (this.player.dead)
                    {
                        this.conversation = new SXConversation(this, SXConversation.SXDialogType.Generic, "playerDead");
                    }
                    if (!this.rainInterrupt && this.player.room == this.oracle.room && this.oracle.room.world.rainCycle.TimeUntilRain < 1600 && this.oracle.room.world.rainCycle.pause < 1)
                    {
                        if (this.conversation != null)
                        {
                            this.conversation = new SXConversation(this, SXConversation.SXDialogType.Generic, "rain");
                            this.rainInterrupt = true;
                        }
                    }
                }


            }

            public void StartItemConversation(DataPearl item)
            {
                Conversation.ID id = Conversation.DataPearlToConversation(item.AbstractPearl.dataPearlType);
                this.conversation = new SXConversation(this, SXConversation.SXDialogType.Pearls, item.AbstractPearl.dataPearlType.value, item.AbstractPearl.dataPearlType);
            }

            public void StartItemConversation(PhysicalObject item)
            {
                this.conversation = new SXConversation(this, SXConversation.SXDialogType.Item, item.GetType().ToString());
            }

            public void ChangePlayerScore(string operation, int amount)
            {
                SlugBase.SaveData.SlugBaseSaveData saveData = SlugBase.SaveData.SaveDataExtension.GetSlugBaseData(((StoryGameSession)this.oracle.room.game.session).saveState.miscWorldSaveData);
                if (!saveData.TryGet($"{this.oracle.ID}_playerScore", out int playerScore))
                {
                    playerScore = 0;
                }
                this.playerScore = playerScore;
                if (operation == "add")
                {
                    this.playerScore += amount;
                }
                else if (operation == "subtract")
                {
                    this.playerScore -= amount;
                }
                else
                {
                    this.playerScore = amount;
                }
                saveData.Set($"{this.oracle.ID}_playerScore", this.playerScore);
                if (this.playerScore < 10.0f)
                {
                    this.oracleAnnoyed = true;
                }
                if (this.playerScore < 0.0f)
                {
                    this.oracleAngry = true;
                }
            }

            public void ReactToHitByWeapon(Weapon weapon)
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    this.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, this.oracle.firstChunk).requireActiveUpkeep = false;
                }
                else
                {
                    this.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, this.oracle.firstChunk).requireActiveUpkeep = false;
                }
                if (this.conversation != null)
                {
                    this.conversationResumeTo = this.conversation;
                    // clear the current dialog box
                    if (this.dialogBox.messages.Count > 0)
                    {
                        this.dialogBox.messages = new List<DialogBox.Message>
                    {
                        this.dialogBox.messages[0]
                    };
                        this.dialogBox.lingerCounter = this.dialogBox.messages[0].linger + 1;
                        this.dialogBox.showCharacter = this.dialogBox.messages[0].text.Length + 2;
                    }
                    this.conversation = new SXConversation(this, SXConversation.SXDialogType.Generic, "playerAttack");

                }
            }

            public void CheckActions()
            {
                switch (this.action)
                {
                    case SXOracleAction.generalIdle:
                        if (this.player != null && this.player.room == this.oracle.room)
                        {
                            this.discoverCounter++;

                            // see player code?
                        }
                        break;
                    case SXOracleAction.giveMark:
                        if (((StoryGameSession)this.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                        {
                            this.action = SXOracleAction.generalIdle;
                            return;
                        }
                        if (this.inActionCounter > 30 && this.inActionCounter < 300)
                        {
                            if (this.inActionCounter < 300)
                            {
                                if (ModManager.CoopAvailable)
                                {
                                    base.StunCoopPlayers(20);
                                }
                                else
                                {
                                    this.player.Stun(20);
                                }
                            }
                            Vector2 holdPlayerAt = Vector2.ClampMagnitude(this.oracle.room.MiddleOfTile(24, 14) - this.player.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, (float)this.inActionCounter);

                            foreach (Player player in base.PlayersInRoom)
                            {
                                player.mainBodyChunk.vel += holdPlayerAt;
                            }

                        }
                        if (this.inActionCounter == 30)
                        {
                            this.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                        }
                        if (this.inActionCounter == 300)
                        {
                            this.action = SXOracleAction.generalIdle;
                            this.player.AddFood(10);
                            foreach (Player player in base.PlayersInRoom)
                            {
                                for (int i = 0; i < 20; i++)
                                {
                                    this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                                }
                            }

                            ((StoryGameSession)this.player.room.game.session).saveState.deathPersistentSaveData.theMark = true;
                            this.conversation = new SXConversation(this, SXConversation.SXDialogType.Generic, "afterGiveMark");
                        }
                        this.action = SXOracleAction.generalIdle;
                        break;
                    case SXOracleAction.giveKarma:
                        // set player to max karma level
                        if (Int32.TryParse(this.actionParam, out int karmaCap))
                        {
                            StoryGameSession session = ((StoryGameSession)this.oracle.room.game.session);
                            if (karmaCap >= 0)
                            {
                                session.saveState.deathPersistentSaveData.karmaCap = karmaCap;
                                session.saveState.deathPersistentSaveData.karma = karmaCap;
                            }
                            else
                            { // -1 passed, set to current max
                                session.saveState.deathPersistentSaveData.karma = karmaCap;
                            }

                            this.oracle.room.game.manager.rainWorld.progression.SaveDeathPersistentDataOfCurrentState(false, false);

                            foreach (RoomCamera camera in this.oracle.room.game.cameras)
                            {

                                if (camera.hud.karmaMeter != null)
                                {
                                    camera.hud.karmaMeter.forceVisibleCounter = 80;
                                    camera.hud.karmaMeter.UpdateGraphic();
                                    camera.hud.karmaMeter.reinforceAnimation = 1;
                                    ((StoryGameSession)this.oracle.room.game.session).AppendTimeOnCycleEnd(true);
                                }
                            }

                            foreach (Player player in base.PlayersInRoom)
                            {
                                for (int i = 0; i < 20; i++)
                                {
                                    this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                                }
                            }

                        }
                        this.action = SXOracleAction.generalIdle;

                        break;
                    case SXOracleAction.giveFood:
                        if (!Int32.TryParse(this.actionParam, out int playerFood))
                        {
                            playerFood = this.player.MaxFoodInStomach;
                        }
                        this.player.AddFood(playerFood);
                        this.action = SXOracleAction.generalIdle;
                        break;
                    case SXOracleAction.startPlayerConversation:
                        this.conversation = new SXConversation(this, SXConversation.SXDialogType.Generic, "playerConversation");
                        this.action = SXOracleAction.generalIdle;
                        break;
                    case SXOracleAction.kickPlayerOut:
                        ShortcutData? shortcut = this.GetShortcutToRoom(this.actionParam);
                        if (shortcut == null)
                        {
                            this.action = SXOracleAction.generalIdle;
                            return;
                        }

                        Vector2 vector2 = this.oracle.room.MiddleOfTile(shortcut.Value.startCoord);

                        foreach (Player player in this.PlayersInRoom)
                        {
                            player.mainBodyChunk.vel += Custom.DirVec(player.mainBodyChunk.pos, vector2);
                        }
                        this.ChangePlayerScore("set", -10);
                        break;
                    case SXOracleAction.killPlayer:
                        if (!this.player.dead && this.player.room == this.oracle.room)
                        {
                            this.player.mainBodyChunk.vel += Custom.RNV() * 12f;
                            for (int i = 0; i < 20; i++)
                            {
                                this.oracle.room.AddObject(new Spark(this.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                                this.player.Die();
                            }
                        }
                        break;
                }
            }

            private ShortcutData? GetShortcutToRoom(string roomId)
            {
                foreach (ShortcutData shortcut in this.oracle.room.shortcuts)
                {
                    IntVector2 destTile = shortcut.connection.DestTile;
                    AbstractRoom destRoom = this.oracle.room.WhichRoomDoesThisExitLeadTo(destTile);
                    if (destRoom != null)
                    {
                        if (destRoom.name == roomId)
                        {
                            return shortcut;
                        }
                    }
                }
                return null;
            }

        }

        //Oracle Conversation

        public class SXConversation : Conversation
        {
            public SXOracleBehavior owner;
            public string eventId;
            public SXDialogType eventType;
            public DataPearl.AbstractDataPearl.DataPearlType pearlType;

            public enum SXDialogType
            {
                Generic,
                Pearls,
                Item
            }
            // public ConversationBehavior convBehav;
            public SXConversation(SXOracleBehavior owner, SXDialogType eventType, string eventId, DataPearl.AbstractDataPearl.DataPearlType pearlType = null) : base(owner, Conversation.ID.None, owner.dialogBox)
            {
                this.owner = owner;
                // this.convBehav = convBehav;
                this.eventType = eventType;
                this.eventId = eventId;
                this.pearlType = pearlType;
                this.AddEvents();
            }

            public override void AddEvents()
            {
                // read this.id
                //IteratorKit.Logger.LogInfo($"Adding events for {this.eventId}");
                //List<OracleEventObjectJson> dialogList = this.oracleDialogJson.generic;

                switch (eventType)
                {
                    case SXDialogType.Generic:
                        //dialogList = this.oracleDialogJson.generic;
                        break;
                    case SXDialogType.Pearls:
                        //dialogList = this.oracleDialogJson.pearls;
                        break;
                    case SXDialogType.Item:
                        //dialogList = this.oracleDialogJson.items;
                        break;
                    default:
                        //dialogList = this.oracleDialogJson.generic;
                        break;
                }

                //List<OracleEventObjectJson> dialogData = dialogList?.FindAll(x => x.eventId == this.eventId);
                /*if (dialogData.Count > 0)
                {
                    foreach (OracleEventObjectJson item in dialogData)
                    {
                        if (!item.forSlugcats.Contains(this.owner.oracle.room.game.GetStorySession.saveStateNumber))
                        {
                            continue; // skip as this one isnt for us
                        }

                        if (item.action != null)
                        {
                            if (Enum.TryParse(item.action, out CMOracleBehavior.CMOracleAction tmpAction))
                            {
                                this.events.Add(new CMOracleActionEvent(this, tmpAction, item));
                            }
                            else
                            {
                                IteratorKit.Logger.LogError($"Given JSON action not valid. {item.action}");
                            }
                        }

                        if (!((StoryGameSession)this.owner.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                        {
                            // dont run any dialogs until we have given the player the mark.
                            return;
                        }

                        // add the texts. get texts handles randomness
                        foreach (string text in item.getTexts(this.owner.oracle.room.game.StoryCharacter))
                        {
                            if (text != null)
                            {
                                this.events.Add(new CMOracleTextEvent(this, this.ReplaceParts(text), item));
                            }

                        }


                    }

                }
                else
                {*/

                    /*if (this.TryLoadCustomPearls())
                    {
                        return;
                    }
                    else if (this.TryLoadFallbackPearls())
                    {
                        return;
                    }
                    else
                    {
                        return;
                    }*/

                //}


            }

            /*private bool TryLoadCustomPearls()
            {
                CustomPearls.CustomPearls.DataPearlRelationStore dataPearlRelation = CustomPearls.CustomPearls.pearlJsonDict.FirstOrDefault(x => x.Value.pearlType == this.pearlType).Value;
                if (dataPearlRelation != null)
                {
                    OracleEventObjectJson pearlJson = null; ;
                    switch (this.owner.oracle.oracleJson.pearlFallback?.ToLower() ?? "default")
                    {
                        case "pebbles":
                            pearlJson = dataPearlRelation.pearlJson.dialogs.pebbles;
                            break;
                        case "moon":
                            pearlJson = dataPearlRelation.pearlJson.dialogs.moon;
                            break;
                        case "pastmoon":
                            pearlJson = dataPearlRelation.pearlJson.dialogs.pastMoon;
                            break;
                        case "futuremoon":
                            pearlJson = dataPearlRelation.pearlJson.dialogs.futureMoon;
                            break;
                    }
                    if (pearlJson == null && dataPearlRelation.pearlJson.dialogs.defaultDiags != null)
                    {
                        // use default as fallback
                        pearlJson = dataPearlRelation.pearlJson.dialogs.defaultDiags;
                    }

                    if (pearlJson != null)
                    {
                        foreach (string text in pearlJson.getTexts((this.interfaceOwner as OracleBehavior).oracle.room.game.GetStorySession.saveStateNumber))
                        {
                            this.events.Add(new Conversation.TextEvent(this, pearlJson.delay, this.ReplaceParts(text), pearlJson.hold));
                        }
                        return true;
                    }
                    else
                    {
                        IteratorKit.Logger.LogError($"Failed to load dialog texts for this oracle.");
                    }
                }
                return false;
            }*/

           /* private bool TryLoadFallbackPearls()
            {
                if (this.pearlType != null && this.owner.oracle.oracleJson.pearlFallback != null)
                {
                    // is not a custom pearl. switch which set of pearl dialogs to use, null save file uses default moon dialogs, so any value except below will use moons dialogs.
                    SlugcatStats.Name saveFileName = null;
                    switch (this.owner.oracle.oracleJson.pearlFallback.ToLower())
                    {
                        case "pebbles":
                            saveFileName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                            break;
                        case "pastmoon":
                            saveFileName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
                            break;
                        case "futuremoon":
                            saveFileName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
                            break;
                    }

                    int id = MoreSlugcats.CollectionsMenu.DataPearlToFileID(this.pearlType); // very useful method
                    if (this.pearlType == MoreSlugcats.MoreSlugcatsEnums.DataPearlType.Spearmasterpearl)
                    {
                        id = 106;
                    }
                    this.LoadEventsFromFile(id, saveFileName, false, 0);
                    return true;
                }
                return false;
            }*/

            public string Translate(string s)
            {
                return this.owner.Translate(s);
            }

            public string ReplaceParts(string s)
            {
                s = Regex.Replace(s, "<PLAYERNAME>", this.NameForPlayer(false));
                s = Regex.Replace(s, "<CAPPLAYERNAME>", this.NameForPlayer(true));
                s = Regex.Replace(s, "<PlayerName>", this.NameForPlayer(false));
                s = Regex.Replace(s, "<CapPlayerName>", this.NameForPlayer(true));
                return s;
            }


            public string NameForPlayer(bool capitalized)
            {
                return "little creature";

            }

            public void FallbackPearlConvo(PhysicalObject physicalObject)
            {
                base.LoadEventsFromFile(38, true, physicalObject.abstractPhysicalObject.ID.RandomSeed);
            }

            public override void Update()
            {
                if (this.paused)
                {
                    return;
                }
                if (this.events.Count == 0)
                {
                    this.Destroy();
                    return;
                }
                this.events[0].Update();
                if (this.events[0].IsOver)
                {
                    this.events.RemoveAt(0);
                }
            }

            /*public void OnEventActivate(DialogueEvent dialogueEvent, OracleEventObjectJson dialogData)
            {
                if (dialogData.score != null)
                {
                    this.owner.ChangePlayerScore(dialogData.score.action, dialogData.score.amount);
                }
                if (dialogData.movement != null)
                {
                    IteratorKit.Logger.LogWarning($"Change movement to {dialogData.movement}");
                    if (Enum.TryParse(dialogData.movement, out CMOracleMovement tmpMovement))
                    {
                        this.owner.movementBehavior = tmpMovement;
                    }
                    else
                    {
                        IteratorKit.Logger.LogError($"Invalid movement option provided: {dialogData.movement}");
                    }

                }
            }*/

            /*public class CMOracleTextEvent : TextEvent
            {
                public new CMConversation owner;
                public ChangePlayerScoreJson playerScoreData;
                public OracleEventObjectJson dialogData;
                public CMOracleTextEvent(CMConversation owner, string text, OracleEventObjectJson dialogData) : base(owner, dialogData.delay, text, dialogData.hold)
                {
                    this.owner = owner;
                    this.playerScoreData = dialogData.score;
                    this.dialogData = dialogData;

                }

                public override void Activate()
                {
                    base.Activate();
                    this.owner.dialogBox.currentColor = this.dialogData.color;
                    this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks
                }
            }*/


            /*public class CMOracleActionEvent : DialogueEvent
            {

                public new CMConversation owner;
                CMOracleBehavior.CMOracleAction action;
                public string actionParam;
                public ChangePlayerScoreJson playerScoreData;
                public OracleEventObjectJson dialogData;

                public CMOracleActionEvent(CMConversation owner, CMOracleBehavior.CMOracleAction action, OracleEventObjectJson dialogData) : base(owner, dialogData.delay)
                {
                    IteratorKit.Logger.LogWarning("Adding custom event");
                    this.owner = owner;
                    this.action = action;
                    this.actionParam = dialogData.actionParam;
                    this.playerScoreData = dialogData.score;
                    this.dialogData = dialogData;
                }

                public override void Activate()
                {
                    base.Activate();
                    IteratorKit.Logger.LogInfo($"Triggering action ${action}");
                    this.owner.owner.NewAction(action, this.actionParam); // this passes the torch over to CMOracleBehavior to run the rest of this shite
                    this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks
                }

                public static void LogAllDialogEvents()
                {
                    for (int i = 0; i < DataPearl.AbstractDataPearl.DataPearlType.values.Count; i++)
                    {
                        IteratorKit.Logger.LogInfo($"Pearl: {DataPearl.AbstractDataPearl.DataPearlType.values.GetEntry(i)}");
                    }
                    for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.Count; i++)
                    {
                        IteratorKit.Logger.LogInfo($"Item: {AbstractPhysicalObject.AbstractObjectType.values.GetEntry(i)}");
                    }
                }


            }*/
        }

    }
}