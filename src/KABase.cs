﻿using System;
using System.Collections.Generic;
using BepInEx;
using MonoMod.RuntimeDetour;
using MonoMod;
using On;
using RWCustom;
using UnityEngine;
using SlugBase.Features;
using MoreSlugcats;

namespace KarmaAppetite
{

    [BepInPlugin(MOD_ID, "Karma Appetite", "2.0")]

    public class KABase : BaseUnityPlugin //CONTAINS: ConfigMenu, Visuals, Spear proficiencies, Karma:Food:Stats system, and Crafting
    {

        private const string MOD_ID = "darkgran.karmaappetite";

        //-------APPLY HOOKS-------

        public void OnEnable()
        {
            
            //Resources
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            //Config menu
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(this.RainWorld_OnModsInit);

            //Enums
            KarmaAppetiteEnums.APOType.RegisterValues();

            //Visuals
            On.PlayerGraphics.DrawSprites += hook_PlayerGraphics_DrawSprites;
            On.PlayerProgression.SaveToDisk += hook_PlayerProgression_SaveToDisk;
            On.LightSource.ApplyPalette += hook_LightSource_ApplyPalette;
            On.OracleSwarmer.BitByPlayer += hook_OracleSwarmer_BitByPlayer;
            On.SLOracleSwarmer.BitByPlayer += hook_SLOracleSwarmer_BitByPlayer;
            //Appetite
            On.Player.ThrownSpear += hook_Player_ThrownSpear;
            On.HUD.FoodMeter.ctor += hook_FoodMeter_ctor;
            On.StoryGameSession.ctor += hook_StoryGameSession_ctor;
            On.Player.SetMalnourished += hook_Player_SetMalnourished;
            On.Player.AddFood += hook_Player_AddFood;
            On.HUD.FoodMeter.QuarterPipShower.Update += hook_QuarterPipShower_Update;
            //Skills
            On.Player.Update += hook_Player_Update;
            On.Spear.ChangeMode += hook_Spear_ChangeMode;
            On.Player.CanIPickThisUp += hook_Player_CanIPickThisUp;
            //Crafting
            On.AbstractPhysicalObject.Realize += hook_AbstractPhysicalObject_Realize;

            //Tunneling
            KATunneling.KATunneling_Hooks();

        }

        public void OnDisable()
        {
            //Enums
            KarmaAppetiteEnums.APOType.UnregisterValues();
        }

        private void LoadResources(RainWorld rainWorld) { }


        //-------IMPLEMENTATION-------

        //---CONFIG MENU---

        private KAOptions optionsInstance;
        private bool initialized;

        public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (this.initialized)
            {
                return;
            }
            this.initialized = true;
            this.optionsInstance = new KAOptions(this);
            try
            {
                MachineConnector.SetRegisteredOI(MOD_ID, this.optionsInstance);
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Karma Appetite Options: Hook_OnModsInit options failed init error {0}{1}", this.optionsInstance, ex));
                base.Logger.LogError(ex);
                base.Logger.LogMessage("OOPS");
            }
        }


        //---ENUMS---

        public class KarmaAppetiteEnums
        {
            public class APOType
            {
                public static AbstractPhysicalObject.AbstractObjectType SpearShard; //CRAFTING - "rocks made out of spear"

                public static void RegisterValues()
                {
                    SpearShard = new AbstractPhysicalObject.AbstractObjectType("SpearShard", true);
                }

                public static void UnregisterValues()
                {
                    if (SpearShard != null) { SpearShard.Unregister(); SpearShard = null; }
                }
            }
        }


        //---VISUALS---

        private void hook_PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {

            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

            //Saint-Hair
            int hair_num = 0;
            if (self.player.sleepCurlUp > 0f)
            {
                hair_num = Custom.IntClamp((int)Mathf.Lerp((float)7, 4f, self.player.sleepCurlUp), 0, 8);
            }
            else if (self.owner.room != null && self.owner.EffectiveRoomGravity != 0f)
            {
                if (self.player.Consious)
                {
                    if ((self.player.bodyMode == Player.BodyModeIndex.Stand && self.player.input[0].x != 0) || self.player.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        hair_num = self.player.bodyMode == Player.BodyModeIndex.Crawl ? 7 : 6;
                    }
                    else
                    {
                        UnityEngine.Vector2 vector = UnityEngine.Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
                        UnityEngine.Vector2 vector2 = UnityEngine.Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                        UnityEngine.Vector2 vector3 = UnityEngine.Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
                        float num = Custom.AimFromOneVectorToAnother(UnityEngine.Vector2.Lerp(vector2, vector, 0.5f), vector3);
                        hair_num = Mathf.RoundToInt(Mathf.Abs(num / 360f * 34f));
                    }
                }
            }

            sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName("HeadB" + hair_num.ToString());

            //Golden eyes on Karma10

            if (self.owner is Player && (self.owner as Player).Karma >= 9)
            {
                sLeaser.sprites[9].color = new Color(0.976f, 0.584f, 0f);
            }

            //Tunneling
            KATunneling.AnimateTunneling(self, sLeaser, rCam, timeStacker, camPos);

        }

        private bool hook_PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (saveCurrentState && self.currentSaveState != null)
            {
                self.currentSaveState.theGlow = self.currentSaveState.deathPersistentSaveData.karma + 1 > 4 && self.currentSaveState.food != 0;
            }
            return orig.Invoke(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        private static void RefreshGlow(Player self)
        {
            bool glowing = self.Karma + 1 > 4 && self.CurrentFood != 0;
            if (self.glowing != glowing)
            {
                self.glowing = glowing;
                if (!glowing && self.graphicsModule != null && self.graphicsModule is PlayerGraphics)
                {
                    ((PlayerGraphics)self.graphicsModule).lightSource.Destroy();
                }
                if (self.room != null)
                {
                    if (self.room.game.session is StoryGameSession)
                    {
                        (self.room.game.session as StoryGameSession).saveState.theGlow = glowing;
                    }
                }
            }
        }

        private void hook_LightSource_ApplyPalette(On.LightSource.orig_ApplyPalette orig, LightSource self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);

            if (self.tiedToObject is Player)
            {
                if (SlugBase.Features.PlayerFeatures.SlugcatColor.TryGet((self.tiedToObject as Player), out var c))
                {
                    self.color = c;
                }
                
            }
        }

        private void hook_OracleSwarmer_BitByPlayer(On.OracleSwarmer.orig_BitByPlayer orig, OracleSwarmer self, Creature.Grasp grasp, bool eu)
        {
            orig.Invoke(self, grasp, eu);
            RefreshGlow(grasp.grabber as Player);
        }

        private void hook_SLOracleSwarmer_BitByPlayer(On.SLOracleSwarmer.orig_BitByPlayer orig, SLOracleSwarmer self, Creature.Grasp grasp, bool eu)
        {
            orig.Invoke(self, grasp, eu);
            RefreshGlow(grasp.grabber as Player);
        }


        //---APPETITE---

        private const int STARTING_MAX_KARMA = 6;
        private const int DISLODGE_FOOD = 1; //food in stomach for dislodge
        private const int FOOD_POTENTIAL = 14; //max food with max karma

        //REFRESH

        private void RefreshAllPlayers(StoryGameSession session)
        {
            foreach (AbstractCreature ac in session.Players)
            {
                if (ac.realizedCreature != null && ac.realizedCreature is Player)
                {
                    Player player = ac.realizedCreature as Player;
                    KarmaToFood(player.slugcatStats, player.Karma);
                    FoodToStats(player.slugcatStats, player.CurrentFood, player.Karma >= 9);
                    RefreshGlow(player);
                }
            }
        }

        //KARMA -> FOOD -> STATS (incl. spear-throwing)

        private static IntVector2 GetFoodFromKarma(int karma)
        {
            switch (karma + 1)
            {
                case 1:
                default:
                    return new IntVector2(3, 3);
                case 2:
                    return new IntVector2(4, 4);
                case 3:
                    return new IntVector2(5, 4);
                case 4:
                    return new IntVector2(6, 5);
                case 5:
                    return new IntVector2(7, 6);
                case 6:
                    return new IntVector2(9, 7);
                case 7:
                    return new IntVector2(10, 8);
                case 8:
                    return new IntVector2(11, 9);
                case 9:
                    return new IntVector2(12, 10);
                case 10:
                    return new IntVector2(FOOD_POTENTIAL, 11);
            }
        }

        private void KarmaToFood(SlugcatStats self, int karma)
        {
            self.maxFood = GetFoodFromKarma(karma).x;
            self.foodToHibernate = GetFoodFromKarma(karma).y;
        }

        private static void FoodToStats(SlugcatStats self, int food, bool extraStats)
        {

            if (!self.malnourished)
            {
                self.throwingSkill = (food > 0) ? 2 : 0;

                float statBonus = food * ((extraStats) ? 0.08f : 0.04f);

                const float STAT_BASE = 0.88f;
                self.runspeedFac = STAT_BASE - 0.05f + statBonus;
                self.poleClimbSpeedFac = STAT_BASE + statBonus;
                self.corridorClimbSpeedFac = STAT_BASE + statBonus;
                self.lungsFac = STAT_BASE + statBonus;

                self.generalVisibilityBonus = 0f + statBonus / 10;
                self.loudnessFac = 1.43f - statBonus / 2;
                self.visualStealthInSneakMode = 0.11f + statBonus / 2;
                self.bodyWeightFac -= statBonus / 2;
            }
            else
            {
                self.throwingSkill = 0;

                self.loudnessFac = 1.38f;
                self.generalVisibilityBonus = -0.1f;
                self.visualStealthInSneakMode = 0.3f;
            }

        }

        private void hook_Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            spear.spearDamageBonus = 0.25f + ((self.playerState.foodInStomach / (FOOD_POTENTIAL / 10)) * ((self.Karma >= 9) ? 1f : 0.5f));
            BodyChunk firstChunk2 = spear.firstChunk;
            float speedBoost = 0.73f + (self.playerState.foodInStomach / 10);
            if (speedBoost < 1f && self.playerState.foodInStomach > 0) { speedBoost = 1f; }
            firstChunk2.vel.x = firstChunk2.vel.x * speedBoost;
        }

        //FOOD METER

        private void hook_StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig.Invoke(self, saveStateNumber, game);
            KarmaToFood(self.characterStats, self.saveState.deathPersistentSaveData.karma);
            FoodToStats(self.characterStats, self.saveState.food, self.saveState.deathPersistentSaveData.karma >= 9);
        }

        private void hook_FoodMeter_ctor(On.HUD.FoodMeter.orig_ctor orig, HUD.FoodMeter self, HUD.HUD hud, int maxFood, int survivalLimit, Player associatedPup = null, int pupNumber = 0)
        {
            if (hud.owner is Menu.SlugcatSelectMenu.SlugcatPageContinue)
            {
                IntVector2 fm = GetFoodFromKarma(((Menu.SlugcatSelectMenu.SlugcatPageContinue)hud.owner).saveGameData.karma);
                maxFood = fm.x;
                survivalLimit = fm.y;
            }
            orig.Invoke(self, hud, maxFood, survivalLimit, associatedPup, pupNumber);
        }

        private void hook_Player_SetMalnourished(On.Player.orig_SetMalnourished orig, Player self, bool m)
        {
            orig.Invoke(self, m);
            KarmaToFood(self.slugcatStats, self.Karma);
            FoodToStats(self.slugcatStats, self.playerState.foodInStomach, self.Karma >= 9);
        }

        private void hook_Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
        {
            orig.Invoke(self, add);
            FoodToStats(self.slugcatStats, self.playerState.foodInStomach, self.Karma >= 9);
            RefreshGlow(self);
        }

        //REMOVING FOOD POINTS

        private static void RemoveQuarterFood(Player self)
        {
            if (self.playerState.quarterFoodPoints <= 0)
            {
                if (self.playerState.foodInStomach > 0)
                {
                    self.SubtractFood(1);

                    self.playerState.quarterFoodPoints += 3;
                    FoodToStats(self.slugcatStats, self.playerState.foodInStomach, self.Karma >= 9);
                    RefreshGlow(self);
                }
            }
            else
            {
                self.playerState.quarterFoodPoints--;
            }
        }

        private void hook_QuarterPipShower_Update(On.HUD.FoodMeter.QuarterPipShower.orig_Update orig, HUD.FoodMeter.QuarterPipShower self)
        {
            orig.Invoke(self);
            if (!self.owner.IsPupFoodMeter && (self.owner.hud.owner as Player).playerState.quarterFoodPoints < self.displayQuarterFood)
            {
                self.owner.visibleCounter = 80;
                self.displayQuarterFood--;
                self.lightUp = 1f;
                if (self.owner.showCount < self.owner.circles.Count)
                {
                    self.owner.circles[self.owner.showCount].QuarterCirclePlop();
                }
            }
        }


        //---SKILLS---

        private void hook_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            KATunneling.CheckForTunneling(self, eu); //See KATunneling
            if (!KATunneling.IsInTunnel)
            {
                CheckForCrafting(self, eu); //See below Skills: Spear Pull
            }

        }

        public static bool CanAffordPrice(Player self, int price, bool never_free = false)
        {
            return (self.playerState.foodInStomach * 4 + self.playerState.quarterFoodPoints) >= price || (self.Karma >= STARTING_MAX_KARMA && !never_free);
        }


        //SPEAR PULL

        private void hook_Spear_ChangeMode(On.Spear.orig_ChangeMode orig, Spear self, Weapon.Mode newMode)
        {
            if (self.mode == Weapon.Mode.StuckInWall && newMode != Weapon.Mode.StuckInWall)
            {
                if (self.abstractSpear.stuckInWallCycles >= 0)
                {
                    for (int i = -1; i < 3; i++)
                    {
                        self.room.GetTile(self.stuckInWall.Value + new UnityEngine.Vector2(20f * (float)i, 0f)).horizontalBeam = false;
                    }
                }
                else
                {
                    for (int j = -1; j < 3; j++)
                    {
                        self.room.GetTile(self.stuckInWall.Value + new UnityEngine.Vector2(0f, 20f * (float)j)).verticalBeam = false;
                    }
                }
                self.stuckInWall = null;
                self.abstractSpear.stuckInWallCycles = 0;
                self.addPoles = false;
            }

            orig.Invoke(self, newMode);

        }

        private bool hook_Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            bool orig_result = orig.Invoke(self, obj);

            if (!orig_result && obj is Spear && (obj as Spear).mode == Weapon.Mode.StuckInWall)
            {
                return self.FoodInStomach >= DISLODGE_FOOD;
            }

            return orig_result;
        }


        //---CRAFTING---

        //CRAFTING BASICS

        private const int CRAFTING_TIME = 140;
        private int CraftingCounter = 0;
        private bool CraftingLock = false;
        private int LockCounter = 0;
        private bool LookAtPrimary = true;

        private void CheckForCrafting(Player self, bool eu) //called by hook_Player_Update
        {
            if (Input.GetKey(KeyCode.Q) && IsReadyToCraft(self) && !CraftingLock)
            {
                CraftingCounter++;
                if (CraftingCounter > CRAFTING_TIME)
                {
                    CraftingLock = true;
                    Craft(self, eu);
                    CraftingCounter = 0;
                }
                else
                {
                    AnimateCraft(self);
                }
            }
            else if (CraftingCounter > 0)
            {
                CraftingCounter = 0;
                LookAtPrimary = true;
            }
            if (CraftingLock)
            {
                LockCounter++;
                if (LockCounter == 80)
                {
                    CraftingLock = false;
                    LockCounter = 0;
                }
            }
            else
            {
                LockCounter = 0;
            }
        }

        private bool IsReadyToCraft(Player self)
        {
            return self.Consious && self.swallowAndRegurgitateCounter == 0f && self.sleepCurlUp == 0f && self.spearOnBack.counter == 0f && (self.graphicsModule is PlayerGraphics && (self.graphicsModule as PlayerGraphics).throwCounter == 0f) && Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 1.0f);
        }

        public static void PayDay(Player self, int quarterPrice)
        {
            if (self.Karma < STARTING_MAX_KARMA)
            {
                for (int i = 0; i < quarterPrice; i++)
                {
                    RemoveQuarterFood(self);
                }
            }
        }

        private void Craft(Player self, bool eu) //includes recipes
        {
            bool success = false;
            Room room = self.room;
            PhysicalObject physicalObject = null;
            PhysicalObject physicalObject2 = null;
            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    if (i == 0)
                    {
                        physicalObject = self.grasps[i].grabbed;
                    }
                    if (i == 1)
                    {
                        physicalObject2 = self.grasps[i].grabbed;
                    }
                }
            }
            if (physicalObject == null && physicalObject2 == null) //empty-handed
            {
                if (CanAffordPrice(self, 1)) //"find debris"
                {
                    PhysicalObject newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Rock, room, self.abstractCreature.pos, "");
                    PayDay(self, 1);
                    if (newItem != null)
                    {
                        self.SlugcatGrab(newItem, 0);
                        success = true;
                    }
                }
            }
            else if (physicalObject != null && physicalObject2 != null) //combining 2 objects
            {

                bool noDestruction = false;
                PhysicalObject newItem = null;

                for (int j = 0; j < 2; j++)
                {
                    if (physicalObject is Spear && physicalObject2 is ScavengerBomb && CanAffordPrice(self, 1)) //Spear + Bomb = Explosive Spear
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Spear, room, self.abstractCreature.pos, "explosive");
                        PayDay(self, 1);
                        break;
                    }
                    if (physicalObject is Rock && physicalObject2 is Rock && CanAffordPrice(self, 1)) //Rock + Rock = Spear
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Spear, room, self.abstractCreature.pos, "");
                        PayDay(self, 1);
                        break;
                    }
                    if ((physicalObject is FirecrackerPlant && (physicalObject2 is WaterNut || physicalObject2 is SwollenWaterNut || physicalObject2 is Rock)) && CanAffordPrice(self, 2)) //Firecracker + Waternut/Rock = Bomb
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, room, self.abstractCreature.pos, "");
                        PayDay(self, 2);
                        break;
                    }
                    if (physicalObject is FirecrackerPlant && physicalObject2 is FirecrackerPlant && CanAffordPrice(self, 1)) //Firecracker + Firecracker = Beebomb
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.SporePlant, room, self.abstractCreature.pos, "");
                        PayDay(self, 1);
                        break;
                    }
                    if (physicalObject is SlimeMold && physicalObject2 is DangleFruit) //Slimemold + Dangle = Lantern
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Lantern, room, self.abstractCreature.pos, "");
                        break;
                    }
                    if (physicalObject is VultureGrub && physicalObject2 is DangleFruit && CanAffordPrice(self, 1)) //VultureWorm + Dangle = GrappleWorm
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Creature, room, self.abstractCreature.pos, "Tube Worm");
                        PayDay(self, 1);
                        break;
                    }
                    if ((physicalObject is JellyFish && (physicalObject2 is DangleFruit || physicalObject2 is WaterNut || physicalObject2 is SwollenWaterNut)) && CanAffordPrice(self, 1)) //Jellyfish + Dangle/Waternut = Flashbang
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.FlareBomb, room, self.abstractCreature.pos, "");
                        PayDay(self, 1);
                        break;
                    }
                    if (physicalObject is Mushroom && physicalObject2 is Mushroom && CanAffordPrice(self, 1)) //Mushroom + Mushroom = Gasbomb
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.PuffBall, room, self.abstractCreature.pos, "");
                        PayDay(self, 1);
                        break;
                    }
                    if (physicalObject is DataPearl && physicalObject2 is OverseerCarcass && CanAffordPrice(self, 2)) //Pearl + Overseer = Neuron
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer, room, self.abstractCreature.pos, "");
                        (newItem as OracleSwarmer).affectedByGravity = 0f;
                        PayDay(self, 4);
                        break;
                    }
                    if (physicalObject is Mushroom && physicalObject2 is FlyLure && CanAffordPrice(self, 4)) //Mushroom + Flylure = KarmaFlower
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.KarmaFlower, room, self.abstractCreature.pos, "");
                        PayDay(self, 4);
                        break;
                    }
                    if (self.Karma >= 9 && ((physicalObject is SSOracleSwarmer && physicalObject2 is SSOracleSwarmer) || (physicalObject is KarmaFlower && physicalObject2 is OverseerCarcass)) && CanAffordPrice(self, 4)) //Neuron + Neuron OR Overseer + KarmaFlower = SingularityBomb
                    {
                        newItem = SpawnObject(self, MoreSlugcatsEnums.AbstractObjectType.SingularityBomb, room, self.abstractCreature.pos, "");
                        PayDay(self, 4);
                        break;
                    }
                    //alternative route to explosives
                    if (physicalObject is SSOracleSwarmer && physicalObject2 is Rock) //Neuron + Rock = Overseer
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.OverseerCarcass, room, self.abstractCreature.pos, "");
                        physicalObject.Destroy();
                        noDestruction = true;
                        break;
                    }
                    if (physicalObject is OverseerCarcass && physicalObject2 is OverseerCarcass && CanAffordPrice(self, 2)) //Overseer + Overseer = Firecracker
                    {
                        newItem = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant, room, self.abstractCreature.pos, "");
                        PayDay(self, 2);
                        break;
                    }
                    //killing in-hands
                    if (physicalObject is Creature && (physicalObject2 is Rock || physicalObject2 is Spear)) //Creature + Spear/Rock = Killed Creature
                    {
                        if (!(physicalObject as Creature).dead)
                        {
                            (physicalObject as Creature).dead = true;
                            CreatureState state = (physicalObject as Creature).State;
                            if (state != null)
                            {
                                if (state is HealthState)
                                {
                                    (state as HealthState).alive = false;
                                }
                            }
                            room.PlaySound(SoundID.Spear_Stick_In_Creature, self.mainBodyChunk.pos);
                            noDestruction = true;
                        }
                        break;
                    }
                    if (physicalObject is Creature && physicalObject2 is JellyFish) //Creature + Jellyfish = Killed Creature
                    {
                        if ((physicalObject as Creature).dead && !((physicalObject as Creature).State.meatLeft < (physicalObject as Creature).abstractCreature.creatureTemplate.meatPoints))
                        {
                            (physicalObject as Creature).dead = false;
                            CreatureState state2 = (physicalObject as Creature).State;
                            if (state2 != null)
                            {
                                if (state2 is HealthState)
                                {
                                    (state2 as HealthState).health = 1f;
                                    (state2 as HealthState).alive = true;
                                }
                            }
                            room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, self.mainBodyChunk.pos);
                            noDestruction = true;
                        }
                        break;
                    }
                    
                    //switch items for the second check
                    PhysicalObject physicalObject13 = physicalObject;
                    physicalObject = physicalObject2;
                    physicalObject2 = physicalObject13;
                }
                
                //finish 2-object craft
                if (newItem != null || noDestruction)
                {
                    if (!noDestruction)
                    {
                        physicalObject.Destroy();
                        physicalObject2.Destroy();
                    }
                    if (!(newItem is OracleSwarmer))
                    {
                        self.SlugcatGrab(newItem, 0);
                    }
                    success = true;
                }
            }
            else if (physicalObject != null || physicalObject2 != null) //crafting with only 1 object (mostly reverse-engineering)
            {
                if (physicalObject2 != null)
                {
                    physicalObject = physicalObject2;
                }
                PhysicalObject objectPartA = null;
                PhysicalObject objectPartB = null;
                if (physicalObject is Spear) // Spear => 2 Rocks
                {
                    objectPartA = SpawnObject(self, KarmaAppetiteEnums.APOType.SpearShard, room, self.abstractCreature.pos, "");
                    objectPartB = SpawnObject(self, KarmaAppetiteEnums.APOType.SpearShard, room, self.abstractCreature.pos, "");
                }
                else if (physicalObject is ExplosiveSpear) //Explosive Spear => Spear + Bomb
                {
                    objectPartA = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Spear, room, self.abstractCreature.pos, "");
                    objectPartB = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, room, self.abstractCreature.pos, "");
                }
                else if (physicalObject is ScavengerBomb) //Bomb => Firecracker
                {
                    objectPartA = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.FirecrackerPlant, room, self.abstractCreature.pos, "");
                }
                else if (physicalObject is Lantern) //Lantern => Slimemold
                {
                    objectPartA = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.SlimeMold, room, self.abstractCreature.pos, "");
                }
                else if (physicalObject is TubeWorm) //Grappleworm => VultureWorm
                {
                    objectPartA = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.Creature, room, self.abstractCreature.pos, "Vulture Grub");
                }
                else if (physicalObject is FlareBomb) //Flashbang => Waternut
                {
                    objectPartA = SpawnObject(self, AbstractPhysicalObject.AbstractObjectType.WaterNut, room, self.abstractCreature.pos, "swollen");
                }

                //finish 1-object craft
                if (objectPartA != null || objectPartB != null)
                {
                    physicalObject.Destroy();
                    if (objectPartA != null)
                    {
                        self.SlugcatGrab(objectPartA, 0);
                    }
                    if (objectPartB != null)
                    {
                        self.SlugcatGrab(objectPartB, 1);
                    }
                    success = true;
                }
            }

            if (success)
            {
                AnimateSuccess(self);
            }

        }

        private PhysicalObject SpawnObject(Player crafter, AbstractPhysicalObject.AbstractObjectType spawningObject, Room room, WorldCoordinate spawnCoord, string spawnType = "")
        {
            EntityID newID = room.game.GetNewID();
            PhysicalObject realizedObject;
            if (spawningObject == AbstractPhysicalObject.AbstractObjectType.Creature)
            {
                AbstractCreature abstractCreature = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(spawnType), null, spawnCoord, newID);
                abstractCreature.RealizeInRoom();
                realizedObject = abstractCreature.realizedObject;
            }
            else
            {
                AbstractPhysicalObject abstractPhysicalObject;
                if (AbstractConsumable.IsTypeConsumable(spawningObject))
                {
                    if (spawningObject == AbstractPhysicalObject.AbstractObjectType.DataPearl)
                    {
                        abstractPhysicalObject = new DataPearl.AbstractDataPearl(room.world, spawningObject, null, spawnCoord, newID, -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc);
                    }
                    else
                    {
                        abstractPhysicalObject = new AbstractConsumable(room.world, spawningObject, null, spawnCoord, newID, -1, -1, null);
                    }
                }
                else if (spawningObject == AbstractPhysicalObject.AbstractObjectType.Spear)
                {
                    abstractPhysicalObject = new AbstractSpear(room.world, null, spawnCoord, newID, spawnType == "explosive");
                }
                else
                {
                    if (spawningObject == AbstractPhysicalObject.AbstractObjectType.SporePlant)
                    {
                        abstractPhysicalObject = new SporePlant.AbstractSporePlant(room.world, null, spawnCoord, newID, -1, -1, null, false, true);
                    }
                    else if (spawningObject == AbstractPhysicalObject.AbstractObjectType.OverseerCarcass)
                    {
                        abstractPhysicalObject = new OverseerCarcass.AbstractOverseerCarcass(room.world, null, spawnCoord, room.game.GetNewID(), UnityEngine.Color.black, 0);
                    }
                    else
                    {
                        abstractPhysicalObject = new AbstractPhysicalObject(room.world, spawningObject, null, spawnCoord, newID);
                        if (abstractPhysicalObject is WaterNut.AbstractWaterNut)
                        {
                            (abstractPhysicalObject as WaterNut.AbstractWaterNut).swollen = spawnType == "swollen";
                        }
                    }
                }
                try
                {
                    abstractPhysicalObject.RealizeInRoom();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
                realizedObject = abstractPhysicalObject.realizedObject;
            }
            return realizedObject;
        }

        //CRAFTED ROCKS (= SpearFragment visuals)

        private void hook_AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (self.type == KarmaAppetiteEnums.APOType.SpearShard)
            {
                self.realizedObject = new SpearShard(self, self.world);
            }
            orig.Invoke(self);
        }


        public class SpearShard : Rock
        {
            public SpearShard(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world) { }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);

                sLeaser.sprites = new FSprite[2];
                sLeaser.sprites[0] = new FSprite("SpearFragment1", true);
                TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
                {
                    new TriangleMesh.Triangle(0, 1, 2)
                };
                TriangleMesh triangleMesh = new TriangleMesh("Futile_White", tris, false, false);
                sLeaser.sprites[1] = triangleMesh;
                this.AddToContainer(sLeaser, rCam, null);
            }
        }

        //CRAFTING ANIMATION

        private void AnimateCraft(Player self)
        {
            if (self.graphicsModule != null && (self.grasps[0] != null || self.grasps[1] != null) && self.graphicsModule is PlayerGraphics)
            {
                PlayerGraphics pg = self.graphicsModule as PlayerGraphics;
                if (CraftingCounter % 50 == 0)
                {
                    pg.blink = 25;
                }
                if (self.grasps[0] != null && self.grasps[1] != null)
                {
                    if (CraftingCounter % 50 == 0)
                    {
                        LookAtPrimary = !LookAtPrimary;
                    }
                    pg.objectLooker.currentMostInteresting = LookAtPrimary ? self.grasps[0].grabbed : self.grasps[1].grabbed;
                }
                else if (self.grasps[1] != null)
                {
                    pg.objectLooker.currentMostInteresting = self.grasps[1].grabbed;
                }
                else
                {
                    pg.objectLooker.currentMostInteresting = self.grasps[0].grabbed;
                }
                pg.head.vel += Custom.DirVec(pg.drawPositions[0, 0], pg.objectLooker.mostInterestingLookPoint);
            }
        }

        private void AnimateSuccess(Player self)
        {
            if (self.graphicsModule != null && self.graphicsModule is PlayerGraphics)
            {
                PlayerGraphics pg = self.graphicsModule as PlayerGraphics;
                pg.objectLooker.LookAtNothing();
                pg.blink = 50;
            }
        }

    }
}