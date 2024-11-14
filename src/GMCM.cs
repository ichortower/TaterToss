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
                name: () => TR.Get("gmcm.ThrowKey.name"),
                tooltip: () => TR.Get("gmcm.ThrowKey.tooltip"),
                getValue: () => Main.Config.ThrowKey,
                setValue: (value) => {
                    Main.Config.ThrowKey = value;
                }
            );
            gmcmApi.AddBoolOption(
                mod: Main.instance.ModManifest,
                name: () => TR.Get("gmcm.UseKeyForChildren.name"),
                tooltip: () => TR.Get("gmcm.UseKeyForChildren.tooltip"),
                getValue: () => Main.Config.UseKeyForChildren,
                setValue: (value) => {
                    Main.Config.UseKeyForChildren = value;
                }
            );
            gmcmApi.AddTextOption(
                mod: Main.instance.ModManifest,
                name: () => TR.Get("gmcm.Blocklist.name"),
                tooltip: () => TR.Get("gmcm.Blocklist.tooltip"),
                getValue: () => string.Join(", ", Main.Config.Blocklist),
                setValue: (value) => {
                    Main.Config.Blocklist.Clear();
                    Main.Config.Blocklist.UnionWith(
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
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);
    }
}
