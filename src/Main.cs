using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

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
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            GMCMIntegration.Setup();
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            FarmAnimals.EarnedTossFriendship.Clear();
        }
    }

}
