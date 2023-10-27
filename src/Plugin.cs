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
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(this.RainWorld_OnModsInit);
            On.PlayerGraphics.DrawSprites += hook_PlayerGraphics_DrawSprites;
            On.Spear.ChangeMode += hook_Spear_ChangeMode;
            On.Player.CanIPickThisUp += hook_Player_CanIPickThisUp;
        }


        //-------IMPLEMENT HOOKS-------

        //---VISUALS---

        //SAINT HAIR
        private void hook_PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

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
    }
}