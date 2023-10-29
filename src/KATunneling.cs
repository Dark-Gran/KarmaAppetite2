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
    internal class KATunneling //TUNNELING / PATHFINDING
    {

        //-------APPLY HOOKS-------

        public static void KATunneling_Hooks()
        {

            On.Player.MovementUpdate += hook_Player_MovementUpdate;
            TunnelingHooksForHidingCarried(); //hooks DrawSprites (GraphicsModule, Rock, Spear, Data Pearl...)
        }


        //------IMPLEMENTATION------

        //---FUNCTIONALITY---

        private const int TUNNELING_FIND_TIME = 180;
        private const int TUNNELING_PRICE = 0;
        private const int TUNNELING_DISTANCE = 10;
        private const int TUNNELING_ABORT_TIME = 100;
        private static int TunnelingCounter = 0;
        private static bool TunnelingLock = false;
        private static int TunnelingLockCounter = 0;
        private static int TunnelingAbortTimer = 0;
        public static bool IsInTunnel = false;
        private static IntVector2 TunnelDestination = new IntVector2(0, 0);
        private static UnityEngine.Vector2 InputDirection = new UnityEngine.Vector2(0, 0);


        //INPUT

        public static void CheckForTunneling(Player self, bool eu) //called by KABase.hook_Player_Update
        {
            if (IsInTunnel)
            {
                TunnelingAbortTimer++;
                IntVector2 currentPos = new IntVector2(self.abstractCreature.pos.x, self.abstractCreature.pos.y);
                if (currentPos == TunnelDestination || TunnelingAbortTimer > TUNNELING_ABORT_TIME)
                {
                    self.abstractCreature.pos.Tile = currentPos;
                    self.enteringShortCut = null;
                    self.inShortcut = false;
                    IsInTunnel = false;
                }
            }
            else if (Input.GetKey(KeyCode.E) && CanTunnel(self) && !TunnelingLock)
            {
                TunnelingAbortTimer = 0;
                TunnelingCounter++;
                if (TunnelingCounter > TUNNELING_FIND_TIME)
                {
                    TunnelingLock = true;
                    StartTunnel(self, eu);
                    TunnelingCounter = 0;
                }
                else
                {
                    AnimateTunnelFind(self);
                }
            }
            else
            {
                if (TunnelingAbortTimer > 0)
                {
                    TunnelingAbortTimer = 0;
                }
                if (TunnelingCounter > 0)
                {
                    TunnelingCounter = 0;
                }
            }
            if (TunnelingLock && !IsInTunnel)
            {
                TunnelingLockCounter++;
                if (TunnelingLockCounter == 80)
                {
                    TunnelingLock = false;
                    TunnelingLockCounter = 0;
                }
            }
        }

        private static bool CanTunnel(Player self)
        {
            return KABase.CanAffordPrice(self, TUNNELING_PRICE) && self.Consious && self.swallowAndRegurgitateCounter == 0f && self.sleepCurlUp == 0f && self.spearOnBack.counter == 0f && (self.graphicsModule is PlayerGraphics && (self.graphicsModule as PlayerGraphics).throwCounter == 0f) && Custom.DistLess(self.mainBodyChunk.pos, self.mainBodyChunk.lastPos, 1.0f);
        }

        private static void hook_Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (IsInTunnel || TunnelingCounter > 0)
            {
                if (self.input[0].x != 0 || self.input[0].y != 0)
                {
                    InputDirection.x = self.input[0].x;
                    InputDirection.y = self.input[0].y;
                }
                self.input[0].x = 0;
                self.input[0].y = 0;
            }
            orig.Invoke(self, eu);
        }

        //TUNNELING

        private static void StartTunnel(Player self, bool eu)
        {
            blinkingCounter = 0;

            IntVector2 direction = new IntVector2(1, 0);
            if (InputDirection.y == 0)
            {
                if (InputDirection.x < 0)
                {
                    direction.x = -1;
                }
            }
            else
            {
                direction.x = 0;
                if (InputDirection.y < 0)
                {
                    direction.y = -1;
                }
                else
                {
                    direction.y = 1;
                }
            }

            direction *= TUNNELING_DISTANCE;
            IntVector2 startPos = new IntVector2(self.abstractCreature.pos.x, self.abstractCreature.pos.y);
            TunnelDestination = startPos + direction;

            IsInTunnel = true;
            self.inShortcut = true;
            self.abstractCreature.pos.Tile = TunnelDestination;
            self.enteringShortCut = TunnelDestination;

        }


        //---TUNNELING VISUALS---

        //SLUGCAT ANIMATIONS

        private static void AnimateTunnelFind(Player self) //Pathfinding animation
        {
            if (self.graphicsModule != null && self.graphicsModule is PlayerGraphics)
            {
                PlayerGraphics pg = self.graphicsModule as PlayerGraphics;
                if (TunnelingCounter % 50 == 0)
                {
                    pg.blink = 25;
                }
                pg.objectLooker.currentMostInteresting = self;
                pg.head.vel += Custom.DirVec(pg.drawPositions[0, 0], pg.objectLooker.mostInterestingLookPoint);
            }
        }

        //SLUGCAT HIDE

        private const int TUNNEL_SHOW_TIME = 14; //in-tunnel blinking
        private const int TUNNEL_HIDE_TIME = 10;
        private static int blinkingCounter = 0;
        private static bool tunnelShow = true;
        
        public static void AnimateTunneling(PlayerGraphics pg, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos) //called by KABase.hook_PlayerGraphics_DrawSprites
        {
            blinkingCounter++;
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                {
                    if (IsInTunnel)
                    {
                        if (i != 11)
                        {
                            sLeaser.sprites[i].isVisible = false;
                        }
                        else
                        {
                            if ((tunnelShow && blinkingCounter >= TUNNEL_SHOW_TIME) || (!tunnelShow && blinkingCounter >= TUNNEL_HIDE_TIME))
                            {
                                tunnelShow = !tunnelShow;
                                blinkingCounter = 0;
                            }
                            sLeaser.sprites[i].isVisible = tunnelShow;
                            sLeaser.sprites[i].x = pg.head.pos.x - camPos.x;
                            sLeaser.sprites[i].y = pg.head.pos.y - camPos.y;
                            sLeaser.sprites[i].alpha = 0.9f;
                            if (InputDirection.y == 0)
                            {
                                sLeaser.sprites[i].scaleX = 12f;
                                sLeaser.sprites[i].scaleY = 6f;
                            }
                            else
                            {
                                sLeaser.sprites[i].scaleX = 6f;
                                sLeaser.sprites[i].scaleY = 12f;
                            }
                        }
                    }
                    else if ((i < 4 || i > 8) && i != 12 && i != 13 && i < 15)
                    {
                        sLeaser.sprites[i].isVisible = true;
                        if (i == 11)
                        {
                            sLeaser.sprites[i].scale = 5f;
                        }
                    }
                }
            }
        }

        //HIDE CARRIED

        private static void TunnelingHooksForHidingCarried()
        {
            //generic
            On.GraphicsModule.DrawSprites += hook_GraphicsModule_DrawSprites;
            //objects
            On.Rock.DrawSprites += hook_Rock_DrawSprites;
            On.Spear.DrawSprites += hook_Spear_DrawSprites;
            On.DataPearl.DrawSprites += hook_DataPearl_DrawSprites;
            On.OverseerCarcass.DrawSprites += hook_OverseerCarcass_DrawSprites;
            On.OracleSwarmer.DrawSprites += hook_OracleSwarmer_DrawSprites;
            On.FirecrackerPlant.DrawSprites += hook_FirecrackerPlant_DrawSprites;
            On.ScavengerBomb.DrawSprites += hook_ScavengerBomb_DrawSprites;
            On.DangleFruit.DrawSprites += hook_DangleFruit_DrawSprites;
            On.SlimeMold.DrawSprites += hook_SlimeMold_DrawSprites;
            On.WaterNut.DrawSprites += hook_WaterNut_DrawSprites;
            On.SwollenWaterNut.DrawSprites += hook_SwollenWaterNut_DrawSprites;
            On.FlyLure.DrawSprites += hook_FlyLure_DrawSprites;
            On.Mushroom.DrawSprites += hook_Mushroom_DrawSprites;
            On.SporePlant.DrawSprites += hook_SporePlant_DrawSprites;
            On.Lantern.DrawSprites += hook_Lantern_DrawSprites;
            On.PuffBall.DrawSprites += hook_PuffBall_DrawSprites;
            On.KarmaFlower.DrawSprites += hook_KarmaFlower_DrawSprites;
            On.FlareBomb.DrawSprites += hook_FlareBomb_DrawSprites;
            On.BubbleGrass.DrawSprites += hook_BubbleGrass_DrawSprites;
            On.NeedleEgg.DrawSprites += hook_NeedleEgg_DrawSprites;
            On.EggBugEgg.DrawSprites += hook_EggBugEgg_DrawSprites;
            On.VultureMask.DrawSprites += hook_VultureMask_DrawSprites;
            On.MoreSlugcats.FireEgg.DrawSprites += hook_FireEgg_DrawSprites;
            On.MoreSlugcats.ElectricSpear.DrawSprites += hook_ElectricSpear_DrawSprites;
            On.MoreSlugcats.EnergyCell.DrawSprites += hook_EnergyCell_DrawSprites;
            On.MoreSlugcats.SingularityBomb.DrawSprites += hook_SingularityBomb_DrawSprites;
            On.MoreSlugcats.GooieDuck.DrawSprites += hook_GooieDuck_DrawSprites;
            On.MoreSlugcats.LillyPuck.DrawSprites += hook_LillyPuck_DrawSprites;
            On.MoreSlugcats.DandelionPeach.DrawSprites += hook_DandelionPeach_DrawSprites;
            On.MoreSlugcats.GlowWeed.DrawSprites += hook_GlowWeed_DrawSprites;
            On.MoreSlugcats.MoonCloak.DrawSprites += hook_MoonCloak_DrawSprites;
            On.JokeRifle.DrawSprites += hook_JokeRifle_DrawSprites;
            //creatures
            On.FlyGraphics.DrawSprites += hook_FlyGraphics_DrawSprites;
            On.JellyFish.DrawSprites += hook_JellyFish_DrawSprites;

        }

        private static void HideIfGrabbedInTunnel(RoomCamera.SpriteLeaser sLeaser, PhysicalObject po, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos, List<int> exclude = null)
        {
            foreach (Creature.Grasp grabbedBy in po.grabbedBy)
            {
                if (grabbedBy.grabber is Player)
                {
                    ChangeSpritesVisibility(sLeaser, !IsInTunnel, exclude);
                    break;
                }
            }
            if (po is Spear)
            {
                if ((po as Spear).onPlayerBack)
                {
                    ChangeSpritesVisibility(sLeaser, !IsInTunnel, exclude);
                }
            }
        }

        private static void ChangeSpritesVisibility(RoomCamera.SpriteLeaser sLeaser, bool visibility, List<int> exclude = null)
        {
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (exclude == null || !exclude.Contains(i))
                {
                    sLeaser.sprites[i].isVisible = visibility;
                }
            }
        }

        private static void hook_GraphicsModule_DrawSprites(On.GraphicsModule.orig_DrawSprites orig, GraphicsModule self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos) //Works only on those that call base method without adding too much
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self.owner, rCam, timeStacker, camPos);
        }

        private static void hook_Rock_DrawSprites(On.Rock.orig_DrawSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos, new List<int> { 1 });
        }

        private static void hook_Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_DataPearl_DrawSprites(On.DataPearl.orig_DrawSprites orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_OverseerCarcass_DrawSprites(On.OverseerCarcass.orig_DrawSprites orig, OverseerCarcass self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_OracleSwarmer_DrawSprites(On.OracleSwarmer.orig_DrawSprites orig, OracleSwarmer self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_FirecrackerPlant_DrawSprites(On.FirecrackerPlant.orig_DrawSprites orig, FirecrackerPlant self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_ScavengerBomb_DrawSprites(On.ScavengerBomb.orig_DrawSprites orig, ScavengerBomb self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos, new List<int> { self.spikes.Length + 3 });
        }

        private static void hook_DangleFruit_DrawSprites(On.DangleFruit.orig_DrawSprites orig, DangleFruit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_SlimeMold_DrawSprites(On.SlimeMold.orig_DrawSprites orig, SlimeMold self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_WaterNut_DrawSprites(On.WaterNut.orig_DrawSprites orig, WaterNut self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_SwollenWaterNut_DrawSprites(On.SwollenWaterNut.orig_DrawSprites orig, SwollenWaterNut self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_FlyLure_DrawSprites(On.FlyLure.orig_DrawSprites orig, FlyLure self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_Mushroom_DrawSprites(On.Mushroom.orig_DrawSprites orig, Mushroom self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_SporePlant_DrawSprites(On.SporePlant.orig_DrawSprites orig, SporePlant self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_Lantern_DrawSprites(On.Lantern.orig_DrawSprites orig, Lantern self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_PuffBall_DrawSprites(On.PuffBall.orig_DrawSprites orig, PuffBall self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_KarmaFlower_DrawSprites(On.KarmaFlower.orig_DrawSprites orig, KarmaFlower self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_FlareBomb_DrawSprites(On.FlareBomb.orig_DrawSprites orig, FlareBomb self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos, new List<int> { 1 });
        }

        private static void hook_BubbleGrass_DrawSprites(On.BubbleGrass.orig_DrawSprites orig, BubbleGrass self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_NeedleEgg_DrawSprites(On.NeedleEgg.orig_DrawSprites orig, NeedleEgg self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_EggBugEgg_DrawSprites(On.EggBugEgg.orig_DrawSprites orig, EggBugEgg self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_VultureMask_DrawSprites(On.VultureMask.orig_DrawSprites orig, VultureMask self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_FireEgg_DrawSprites(On.MoreSlugcats.FireEgg.orig_DrawSprites orig, MoreSlugcats.FireEgg self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_ElectricSpear_DrawSprites(On.MoreSlugcats.ElectricSpear.orig_DrawSprites orig, MoreSlugcats.ElectricSpear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_EnergyCell_DrawSprites(On.MoreSlugcats.EnergyCell.orig_DrawSprites orig, MoreSlugcats.EnergyCell self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_SingularityBomb_DrawSprites(On.MoreSlugcats.SingularityBomb.orig_DrawSprites orig, MoreSlugcats.SingularityBomb self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos, new List<int> { self.connections.Length });
        }
        private static void hook_GooieDuck_DrawSprites(On.MoreSlugcats.GooieDuck.orig_DrawSprites orig, MoreSlugcats.GooieDuck self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }
        private static void hook_LillyPuck_DrawSprites(On.MoreSlugcats.LillyPuck.orig_DrawSprites orig, MoreSlugcats.LillyPuck self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }
        private static void hook_DandelionPeach_DrawSprites(On.MoreSlugcats.DandelionPeach.orig_DrawSprites orig, MoreSlugcats.DandelionPeach self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }
        private static void hook_GlowWeed_DrawSprites(On.MoreSlugcats.GlowWeed.orig_DrawSprites orig, MoreSlugcats.GlowWeed self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_MoonCloak_DrawSprites(On.MoreSlugcats.MoonCloak.orig_DrawSprites orig, MoreSlugcats.MoonCloak self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_JokeRifle_DrawSprites(On.JokeRifle.orig_DrawSprites orig, JokeRifle self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

        private static void hook_FlyGraphics_DrawSprites(On.FlyGraphics.orig_DrawSprites orig, FlyGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self.owner, rCam, timeStacker, camPos);
        }

        private static void hook_JellyFish_DrawSprites(On.JellyFish.orig_DrawSprites orig, JellyFish self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            HideIfGrabbedInTunnel(sLeaser, self, rCam, timeStacker, camPos);
        }

    }
}
