using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Pathfinding;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ichortower.TaterToss
{
    internal sealed class Main : Mod
    {
        public static Main instance = null;
        public static string ModId = null;

        public static ModConfig Config = null;

        public override void Entry(IModHelper helper)
        {
            instance = this;
            Config = helper.ReadConfig<ModConfig>();
            ModId = this.ModManifest.UniqueID;
            Harmony harmony = new(ModId);
            Children.ApplyPatches(harmony);
            FarmAnimals.ApplyPatches(harmony);
        }
    }

}
