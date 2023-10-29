using System;
using System.Collections.Generic;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace KarmaAppetite
{

    public class KAOptions : OptionInterface
    {

        public readonly Configurable<bool> noHair;
        public readonly Configurable<bool> noBlinks;
        public readonly Configurable<bool> freeCraft;
        public readonly Configurable<bool> freeTunnels;
        public readonly Configurable<bool> alwaysGlow;

        public KAOptions()
        {
            this.noHair = this.config.Bind<bool>("noHair", false, new ConfigurableInfo(null));
            this.noBlinks = this.config.Bind<bool>("noBlinks", false, new ConfigurableInfo(null));
            this.freeCraft = this.config.Bind<bool>("freeCraft", false, new ConfigurableInfo(null));
            this.freeTunnels = this.config.Bind<bool>("freeTunnels", false, new ConfigurableInfo(null));
            this.alwaysGlow = this.config.Bind<bool>("alwaysGlow", false, new ConfigurableInfo(null));
        }

        public override void Initialize()
        {
            base.Initialize();
            OpTab opTab = new OpTab(this, "GENERAL");
            OpTab opTab2 = new OpTab(this, "CHEATS");
            this.Tabs = new OpTab[]
            {
                opTab,
                opTab2
            };
            //FIRST TAB: GENERAL
            OpContainer opContainer = new OpContainer(new Vector2(0f, 0f));
            opTab.AddItems(new UIelement[]
            {
                opContainer
            });
            UIelement[] array = new UIelement[]
            {
                new OpLabel(30f, 550f, "VISUALS", false),
                new OpCheckBox(this.noHair, 20f, 520f),
                new OpLabel(50f, 521.5f, "Disable Pathfinder's hair (might be required not to mess with other visual mods).", false),
                new OpCheckBox(this.noBlinks, 20f, 490f),
                new OpLabel(50f, 491.5f, "Disable blinking effect when tunneling/pathfinding.", false),
            };
            opTab.AddItems(array);
            //SECOND TAB: CHEATS
            OpContainer opContainer2 = new OpContainer(new Vector2(0f, 0f));
            opTab2.AddItems(new UIelement[]
            {
                opContainer2
            });
            UIelement[] array2 = new UIelement[]
            {
                new OpLabel(30f, 550f, "CHEATS!", false),
                new OpCheckBox(this.freeCraft, 20f, 520f),
                new OpLabel(50f, 521.5f, "Make Crafting always free.", false),
                new OpCheckBox(this.freeTunnels, 20f, 490f),
                new OpLabel(50f, 491.5f, "Make Tunneling always free.", false),
                new OpCheckBox(this.alwaysGlow, 20f, 460f),
                new OpLabel(50f, 461.5f, "Enable light at all times.", false),
            };
            opTab2.AddItems(array2);

        }

        public override void Update()
        {
            base.Update();
        }

    }
}