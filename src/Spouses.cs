using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Network;
using System.Collections.Generic;
using System.Reflection;
//using System.Reflection.Emit;

namespace ichortower.TaterToss;

internal sealed class Spouses
{
    public static void ApplyPatches(Harmony harmony)
    {
        MethodInfo NPC_checkAction = typeof(NPC).GetMethod(
                nameof(NPC.checkAction),
                BindingFlags.Public | BindingFlags.Instance);

        harmony.Patch(NPC_checkAction,
                postfix: new HarmonyMethod(typeof(Spouses),
                    "NPC_checkAction_Postfix"));
    }

    public static void NPC_checkAction_Postfix(ref bool __result,
            NPC __instance, Farmer who, GameLocation l)
    {
        if (__result) {
            return;
        }
        if (__instance.IsInvisible) {
            return;
        }
        if (!who.friendshipData.TryGetValue(__instance.Name, out Friendship fr)) {
            return;
        }
        if (fr.Status != FriendshipStatus.Married) {
            return;
        }
        if (!Main.Config.ThrowKey.IsDown()) {
            return;
        }
        if (Main.Config.Blocklist.Contains(__instance.Name)) {
            Main.instance.Monitor.Log("Blocked toss of NPC" +
                    $" '{__instance.Name}', according to block list.",
                    LogLevel.Trace);
            return;
        }
        LovedOne.PerformToss(__instance, who, GuaranteeNPCMutex(__instance));
        __result = true;
    }

    private static NetMutex GuaranteeNPCMutex(NPC c)
    {
        string key = $"{Main.ModId}/{c.Name}";
        return Game1.player.team.GetOrCreateGlobalInventoryMutex(key);
    }
}
