using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Pets;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ichortower.TaterToss;

internal sealed class Pets
{

    public static void ApplyPatches(Harmony harmony)
    {
        MethodInfo Pet_checkAction = typeof(Pet).GetMethod(
                nameof(Pet.checkAction),
                BindingFlags.Public | BindingFlags.Instance);
        MethodInfo Pet_TryBehaviorChange = typeof(Pet).GetMethod(
                nameof(Pet.TryBehaviorChange),
                BindingFlags.Public | BindingFlags.Instance);
        MethodInfo Pet_draw = typeof(Pet).GetMethod(
                nameof(Pet.draw),
                BindingFlags.Public | BindingFlags.Instance,
                null, new []{typeof(SpriteBatch)}, null);
        MethodInfo Pet_drawHat = typeof(Pet).GetMethod(
                nameof(Pet.drawHat),
                BindingFlags.Public | BindingFlags.Instance);
        MethodInfo Pet_update = typeof(Pet).GetMethod(
                nameof(Pet.update),
                BindingFlags.Public | BindingFlags.Instance,
                null, new []{typeof(GameTime), typeof(GameLocation)}, null);

        harmony.Patch(Pet_checkAction,
                prefix: new HarmonyMethod(typeof(Pets),
                    "Pet_checkAction_Prefix"),
                postfix: new HarmonyMethod(typeof(Pets),
                    "Pet_checkAction_Postfix"));
        harmony.Patch(Pet_TryBehaviorChange,
                prefix: new HarmonyMethod(typeof(Pets),
                    "Pet_TryBehaviorChange_Prefix"));
        harmony.Patch(Pet_draw,
                transpiler: new HarmonyMethod(typeof(Pets),
                    "Pet_draw_Transpiler"));
        harmony.Patch(Pet_drawHat,
                transpiler: new HarmonyMethod(typeof(Pets),
                    "Pet_drawHat_Transpiler"));
        harmony.Patch(Pet_update,
                postfix: new HarmonyMethod(typeof(Pets),
                    "Pet_update_Postfix"));
    }

    /*
     * Like with Child, check whether interaction was placing a hat
     */
    public static void Pet_checkAction_Prefix(Pet __instance,
            Farmer who, GameLocation l, ref bool __state)
    {
        if (who.Items.Count > who.CurrentToolIndex &&
                who.Items[who.CurrentToolIndex] != null &&
                who.Items[who.CurrentToolIndex] is Hat) {
            __state = true;
        }
        else {
            __state = false;
        }
    }

    /*
     * cf. Children.cs's version of this. It's mostly the same, since Child
     * and Pet both descend from NPC
     */
    public static void Pet_checkAction_Postfix(ref bool __result,
            Pet __instance, Farmer who, GameLocation l, bool __state)
    {
        if (__result) {
            return;
        }
        if (__state) { // was holding hat
            return;
        }
        if (__instance.IsInvisible) {
            return;
        }
        if (Main.Config.UseKeyForPets && !Main.Config.ThrowKey.IsDown()) {
            return;
        }
        if (__instance.CurrentBehavior == "Sleep") {
            return;
        }
        if (Main.Config.Blocklist.Contains(__instance.displayName)) {
            Main.instance.Monitor.Log("Blocked toss of pet named" +
                    $" '{__instance.displayName}', according to block list.",
                    LogLevel.Trace);
            return;
        }
        if (Main.Config.Blocklist.Contains(__instance.whichBreed.Value)) {
            Main.instance.Monitor.Log("Blocked toss of pet breed" +
                    $" '{__instance.whichBreed.Value}', according to block list.",
                    LogLevel.Trace);
            return;
        }
        if (Main.Config.Blocklist.Contains(__instance.petType.Value)) {
            Main.instance.Monitor.Log("Blocked toss of pet type" +
                    $" '{__instance.petType.Value}', according to block list.",
                    LogLevel.Trace);
            return;
        }
        LovedOne.PerformToss(__instance, who, __instance.mutex);
        __result = true;
    }

    /*
     * Don't try behavior changes while being thrown.
     */
    public static bool Pet_TryBehaviorChange_Prefix(ref bool __result,
            Pet __instance, List<PetBehaviorChanges> changes)
    {
        if (LovedOne.BeingTossed.Contains(__instance as Character)) {
            return false;
        }
        return true;
    }

    public static IEnumerable<CodeInstruction> Pet_draw_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
    {
        FieldInfo field_drawOnTop = typeof(Pet).GetField(
                nameof(Pet.drawOnTop),
                BindingFlags.Public | BindingFlags.Instance);
        Label originalSpot = generator.DefineLabel();
        Label skipSpot = generator.DefineLabel();
        /*
         * Find the calculation for layerDepth, then add a check for drawOnTop to it and
         * load the constant 0.993f instead of the calc if it is true.
         */
        CodeMatcher cm = new(instructions);
        cm.MatchStartForward(
            new CodeMatch(OpCodes.Ldloc_0),
            new CodeMatch(OpCodes.Conv_R4),
            new CodeMatch(i => i.opcode == OpCodes.Ldc_R4 &&
                (float)i.operand == 10000f))
        .AddLabels(new []{originalSpot})
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, field_drawOnTop),
            new CodeInstruction(OpCodes.Brfalse_S, originalSpot),
            new CodeInstruction(OpCodes.Ldc_R4, 0.993f),
            new CodeInstruction(OpCodes.Br_S, skipSpot))
        .MatchStartForward(
            new CodeMatch(OpCodes.Call))
        .AddLabels(new []{skipSpot});

        return cm.InstructionEnumeration();
    }

    public static IEnumerable<CodeInstruction> Pet_drawHat_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
    {
        FieldInfo field_drawOnTop = typeof(Pet).GetField(
                nameof(Pet.drawOnTop),
                BindingFlags.Public | BindingFlags.Instance);
        Label originalSpot = generator.DefineLabel();
        Label skipSpot = generator.DefineLabel();
        /*
         * Find the calc for horse_draw_layer, and insert a check for drawOnTop
         * like we do in the draw transpiler above.
         */
        CodeMatcher cm = new(instructions);
        cm.MatchStartForward(
            new CodeMatch(i => i.opcode == OpCodes.Ldc_R4 &&
                (float)i.operand == 0f),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(i => i.opcode == OpCodes.Ldfld &&
                ((FieldInfo)i.operand).Name == "isSleepingOnFarmerBed"))
        .ExtractLabels(out IEnumerable<Label> bucket)
        .AddLabels(new []{originalSpot})
        .InsertAndAdvanceWithLabels(bucket,
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, field_drawOnTop),
            new CodeInstruction(OpCodes.Brfalse_S, originalSpot),
            new CodeInstruction(OpCodes.Ldc_R4, 0.993f),
            new CodeInstruction(OpCodes.Br_S, skipSpot))
        .MatchStartForward(
            new CodeMatch(OpCodes.Stloc_1))
        .AddLabels(new []{skipSpot});

        return cm.InstructionEnumeration();
    }

    public static void Pet_update_Postfix(Pet __instance,
            GameTime time, GameLocation location)
    {
        if (__instance.yJumpVelocity > 18f) {
            Utility.addSmokePuff(location,
                    __instance.Position + new Vector2(32f, __instance.yJumpOffset),
                    0,
                    __instance.yJumpVelocity / 8f,
                    0.01f, 0.75f, 0.01f);
        }
    }
}

