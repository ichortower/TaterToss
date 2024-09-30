using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System.Collections.Generic;

internal class ModConfig
{
    public KeybindList AnimalThrowKey = new(SButton.LeftShift);
    public HashSet<string> AnimalBlocklist = new();
}
