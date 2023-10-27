using System;
using BepInEx;
using On;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using MonoMod.RuntimeDetour;

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
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += new On.RainWorld.hook_OnModsInit(this.RainWorld_OnModsInit);
        }

        private void LoadResources(RainWorld rainWorld)
        {

        }

        //-------IMPLEMENT-------
    }

}