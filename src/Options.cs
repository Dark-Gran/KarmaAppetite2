using System;
using System.Collections.Generic;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace KarmaAppetite
{

    public class OptionsMenu : OptionInterface
    {

        public readonly Configurable<bool> testCheckBox;

        public OptionsMenu(KarmaAppetite.KABase plugin)
        {
            this.testCheckBox = this.config.Bind<bool>("KarmaAppetite_TestCheckBox", true, new ConfigurableInfo(null));
        }

        public override void Initialize()
        {
            OpTab opTab = new OpTab(this, "Default Canvas");
            this.Tabs = new OpTab[]
            {
                opTab
            };
            OpContainer opContainer = new OpContainer(new Vector2(0f, 0f));
            opTab.AddItems(new UIelement[]
            {
                opContainer
            });
            UIelement[] array = new UIelement[]
            {
                new OpCheckBox(this.testCheckBox, 20f, 550f),
                new OpLabel(50f, 550f, "Test Check Box", false),
            };
            opTab.AddItems(array);
        }

        public override void Update()
        {
            base.Update();
        }

    }
}