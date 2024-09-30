using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;

namespace ichortower.TaterToss
{
    internal sealed class GMCMIntegration
    {
        public static void Setup()
        {
            var gmcmApi = Main.instance.Helper.ModRegistry.GetApi
                    <IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi is null) {
                return;
            }
            gmcmApi.Register(mod: Main.instance.ModManifest,
                reset: () => {},
                save: () => {
                    Main.instance.Helper.WriteConfig(Main.Config);
                });
            gmcmApi.AddKeybindList(
                mod: Main.instance.ModManifest,
                name: () => TR.Get("gmcm.AnimalThrowKey.name"),
                tooltip: () => TR.Get("gmcm.AnimalThrowKey.tooltip"),
                getValue: () => Main.Config.AnimalThrowKey,
                setValue: (value) => {
                    Main.Config.AnimalThrowKey = value;
                }
            );
            gmcmApi.AddTextOption(
                mod: Main.instance.ModManifest,
                name: () => TR.Get("gmcm.AnimalBlocklist.name"),
                tooltip: () => TR.Get("gmcm.AnimalBlocklist.tooltip"),
                getValue: () => string.Join(", ", Main.Config.AnimalBlocklist),
                setValue: (value) => {
                    Main.Config.AnimalBlocklist.Clear();
                    Main.Config.AnimalBlocklist.UnionWith(
                            value.Split(",", StringSplitOptions.RemoveEmptyEntries |
                                StringSplitOptions.TrimEntries)
                    );
                }
            );
        }
    }

    internal sealed class TR
    {
        public static string Get(string key)
        {
            return Main.instance.Helper.Translation.Get(key);
        }
    }
}

namespace GenericModConfigMenu
{
    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);
    }
}
