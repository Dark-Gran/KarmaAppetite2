using System;
using BepInEx;
using On;
using IL;
using RWCustom;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using MonoMod.RuntimeDetour;
using System.Drawing;
using MoreSlugcats;
using MonoMod;

namespace KarmaAppetite
{

    [BepInPlugin(MOD_ID, "Karma Appetite", "2.0")]
    public class Plugin : BaseUnityPlugin
    {

        private const string MOD_ID = "darkgran.karmaappetite";


        //-------CONFIG MENU-------

        private OptionsMenu optionsMenuInstance;
        private bool initialized;

        public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (this.initialized)
            {
                return;
            }
            this.initialized = true;
            this.optionsMenuInstance = new OptionsMenu(this);
            try
            {
                MachineConnector.SetRegisteredOI(MOD_ID, this.optionsMenuInstance);
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Karma Appetite Options: Hook_OnModsInit options failed init error {0}{1}", this.optionsMenuInstance, ex));
                base.Logger.LogError(ex);
                base.Logger.LogMessage("OOPS");
            }
        }


        //-------APPLY HOOKS-------

        private void LoadResources(RainWorld rainWorld) { }

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(this.RainWorld_OnModsInit); //config menu (above)

            On.PlayerGraphics.DrawSprites += hook_PlayerGraphics_DrawSprites;
            On.Spear.ChangeMode += hook_Spear_ChangeMode;
            On.Player.CanIPickThisUp += hook_Player_CanIPickThisUp;
            On.Weapon.Grabbed += hook_Weapon_Grabbed;
            On.HUD.FoodMeter.ctor += hook_FoodMeter_ctor;
            On.StoryGameSession.ctor += hook_StoryGameSession_ctor;
            On.HUD.FoodMeter.QuarterPipShower.Update += hook_QuarterPipShower_Update;
        }


        //-------IMPLEMENTATION-------

        //---VISUALS---

        private void hook_PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
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
                        Vector2 vector = Vector2.Lerp(self.drawPositions[0, 1], self.drawPositions[0, 0], timeStacker);
                        Vector2 vector2 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
                        Vector2 vector3 = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
                        float num = Custom.AimFromOneVectorToAnother(Vector2.Lerp(vector2, vector, 0.5f), vector3);
                        hair_num = Mathf.RoundToInt(Mathf.Abs(num / 360f * 34f));
                    }
                }
            }

            sLeaser.sprites[3].element = Futile.atlasManager.GetElementWithName("HeadB" + hair_num.ToString());
        }

        //---SKILLS---

        //SPEAR

        private void hook_Spear_ChangeMode(On.Spear.orig_ChangeMode orig, Spear self, Weapon.Mode newMode)
        {
            orig.Invoke(self, newMode);

            if (self.mode == Weapon.Mode.StuckInWall && newMode != Weapon.Mode.StuckInWall)
            {
                if (self.abstractSpear.stuckInWallCycles >= 0)
                {
                    for (int i = -1; i < 3; i++)
                    {
                        self.room.GetTile(self.stuckInWall.Value + new Vector2(20f * (float)i, 0f)).horizontalBeam = false;
                    }
                }
                else
                {
                    for (int j = -1; j < 3; j++)
                    {
                        self.room.GetTile(self.stuckInWall.Value + new Vector2(0f, 20f * (float)j)).verticalBeam = false;
                    }
                }
                self.stuckInWall = null;
                self.abstractSpear.stuckInWallCycles = 0;
                self.addPoles = false;
            }
        }

        private bool hook_Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            bool orig_result = orig.Invoke(self, obj);

            if (obj is Spear && !orig_result)
            {
                if ((obj as Spear).mode == Weapon.Mode.Free || (obj as Spear).mode == Weapon.Mode.StuckInCreature || (obj as Spear).mode == Weapon.Mode.StuckInWall)
                {
                    return true;
                }
            }

            return orig_result;
        }

        //SPEAR-PULL PRICE

        private void hook_Weapon_Grabbed(On.Weapon.orig_Grabbed orig, Weapon self, Creature.Grasp grasp)
        {
            if (grasp.grabber is Player && (grasp.grabbed as Weapon).mode == Weapon.Mode.StuckInWall)
            {
                (grasp.grabber as Player).SubtractFood(1);
            }
            orig.Invoke(self, grasp);
        }

        //---APPETITE---

        private const int STARTING_MAX_KARMA = 6;
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

        //KARMA -> FOOD -> STATS

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

        private void FoodToStats(SlugcatStats self, int food, bool extraStats)
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
                self.loudnessFac = 1.45f - statBonus / 2;
                self.visualStealthInSneakMode = 0.11f + statBonus / 2;
                self.bodyWeightFac -= statBonus / 2;
            }
            else
            {
                self.throwingSkill = 0;

                self.loudnessFac = 1.4f;
                self.generalVisibilityBonus = -0.1f;
                self.visualStealthInSneakMode = 0.3f;
            }

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

        //REMOVING FOOD POINTS

        private void RemoveQuarterFood(Player self)
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

        //GLOW

        private void RefreshGlow(Player self)
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

    }
}