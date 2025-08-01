using HarmonyLib;
using Microsoft.Xna.Framework;
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
        MethodInfo NPC_update = typeof(NPC).GetMethod(
                nameof(NPC.update),
                BindingFlags.Public | BindingFlags.Instance,
                null, new []{typeof(GameTime), typeof(GameLocation)}, null);

        harmony.Patch(NPC_checkAction,
                postfix: new HarmonyMethod(typeof(Spouses),
                    "NPC_checkAction_Postfix"));
        harmony.Patch(NPC_update,
                postfix: new HarmonyMethod(typeof(Spouses),
                    "NPC_update_Postfix"));
    }

    public static void NPC_checkAction_Postfix(ref bool __result,
            NPC __instance, Farmer who, GameLocation l)
    {
        if (__result) {
            int kissFrame = __instance.GetData()?.KissSpriteIndex ?? 28;
            if (__instance.Sprite.CurrentFrame != kissFrame) {
                return;
            }
        }
        if (__instance.IsInvisible) {
            return;
        }
        if (__instance.isSleeping.Value) {
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
        if (Main.Config.Blocklist.Contains(__instance.displayName)) {
            Main.instance.Monitor.Log("Blocked toss of NPC named" +
                    $" '{__instance.displayName}', according to block list.",
                    LogLevel.Trace);
            return;
        }
        LovedOne.PerformToss(__instance, who, GuaranteeNPCMutex(__instance));
        __result = true;
    }

    public static void NPC_update_Postfix(NPC __instance,
            GameTime time, GameLocation location)
    {
        if (__instance.yJumpVelocity > 18f) {
            // add half of spritewidth at 4x, then subtract half of the
            // puff (10px wide) at 4x
            float x = (float)__instance.Sprite.SpriteWidth * 2f - 5*4;
            Utility.addSmokePuff(location,
                    __instance.Position + new Vector2(x, __instance.yJumpOffset),
                    0,
                    __instance.yJumpVelocity / 8f,
                    0.01f, 0.75f, 0.01f);
        }
    }

    private static NetMutex GuaranteeNPCMutex(NPC c)
    {
        string key = $"{Main.ModId}/{c.Name}";
        return Game1.player.team.GetOrCreateGlobalInventoryMutex(key);
    }
}
