using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

internal class ModConfig
{
    public KeybindList ThrowKey = new(SButton.LeftShift);
    public bool UseKeyForChildren = false;
    public HashSet<string> Blocklist = new();
}
