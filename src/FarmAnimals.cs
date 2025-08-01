using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Network;
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
    internal sealed class FarmAnimals
    {
        private static bool WasAlreadyPet = false;
        internal static NetMutex CurrentMutex = null;

        /*
         * Feels yucky to use the global inventory mutex dict for this, but
         * it's probably better to do that than try to set it up myself and
         * have countless bugs.
         */
        private static NetMutex GuaranteeAnimalMutex(FarmAnimal fa)
        {
            string key = $"{Main.ModId}/{fa.myID.Value}";
            return Game1.player.team.GetOrCreateGlobalInventoryMutex(key);
        }

        public static void ApplyPatches(Harmony harmony)
        {
            try {
                MethodInfo FarmAnimal_pet = typeof(FarmAnimal).GetMethod(
                        nameof(FarmAnimal.pet),
                        BindingFlags.Public | BindingFlags.Instance);
                MethodInfo FarmAnimal_updateWhenCurrentLocation = typeof(FarmAnimal).GetMethod(
                        nameof(FarmAnimal.updateWhenCurrentLocation),
                        BindingFlags.Public | BindingFlags.Instance);
                MethodInfo FarmAnimal_draw = typeof(FarmAnimal).GetMethod(
                        nameof(FarmAnimal.draw),
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new Type[]{typeof(SpriteBatch)},
                        null);
                harmony.Patch(FarmAnimal_pet,
                        prefix: new HarmonyMethod(typeof(FarmAnimals),
                            "FarmAnimal_pet_Prefix"),
                        postfix: new HarmonyMethod(typeof(FarmAnimals),
                            "FarmAnimal_pet_Postfix"));
                harmony.Patch(FarmAnimal_updateWhenCurrentLocation,
                        postfix: new HarmonyMethod(typeof(FarmAnimals),
                            "FarmAnimal_updateWhenCurrentLocation_Postfix"));
                harmony.Patch(FarmAnimal_draw,
                        transpiler: new HarmonyMethod(typeof(FarmAnimals),
                            "FarmAnimal_draw_Transpiler"));
            }
            catch(Exception e) {
                Main.instance.Monitor.Log($"Patch failed: {e}", LogLevel.Error);
            }
        }

        public static void FarmAnimal_pet_Prefix(
                FarmAnimal __instance, Farmer who, bool is_auto_pet)
        {
            WasAlreadyPet = __instance.wasPet.Value;
        }

        public static void FarmAnimal_pet_Postfix(
                FarmAnimal __instance, Farmer who, bool is_auto_pet)
        {
            if (is_auto_pet || !WasAlreadyPet) {
                return;
            }
            if (__instance.IsActuallySwimming()) {
                return;
            }
            // "trying to sleep"
            if (Game1.timeOfDay >= 1900 && !__instance.isMoving()) {
                return;
            }
            if (who.ActiveObject?.QualifiedItemId == "(O)GoldenAnimalCracker") {
                return;
            }
            if (!Main.Config.ThrowKey.IsDown()) {
                return;
            }
            bool blocked = false;
            if (Main.Config.Blocklist.Contains(__instance.displayName)) {
                Main.instance.Monitor.Log("Blocked toss of animal named" +
                        $" '{__instance.displayName}', according to block list.",
                        LogLevel.Trace);
                blocked = true;
            }
            if (Main.Config.Blocklist.Contains(__instance.type.Value)) {
                Main.instance.Monitor.Log("Blocked toss of animal type" +
                        $" '{__instance.type.Value}', according to block list.",
                        LogLevel.Trace);
                blocked = true;
            }
            // skip the AnimalQueryMenu by exiting it immediately
            Game1.exitActiveMenu();
            if (!blocked) {
                LovedOne.PerformToss(__instance, who, GuaranteeAnimalMutex(__instance));
            }
        }

        public static void FarmAnimal_updateWhenCurrentLocation_Postfix(
                FarmAnimal __instance, GameTime time, GameLocation location)
        {
            if (__instance.Sprite.CurrentAnimation != null &&
                    __instance.yJumpOffset != 0) {
                __instance.update(time, location, __instance.myID.Value, move:false);
            }
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

        /*
         * Two changes in this transpiler:
         * 1. avoid adding yJumpOffset to the draw offset vector a second time
         *    if the animal's hopOffset vector is zero.
         * 2. honor drawOnTop with a high layer_depth, like some other classes.
         *
         * These actually target immediately adjacent sections of the CIL, so
         * even though I have them visually separated, the patches are pretty
         * intertwined. Hopefully easier to back out just one of them if needed.
         */
        public static IEnumerable<CodeInstruction> FarmAnimal_draw_Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase original)
        {
            CodeMatcher cm = new(instructions);

            // the yJumpOffset patch
            Label offsetSkip = generator.DefineLabel();
            FieldInfo hopOffsetField = typeof(FarmAnimal).GetField(
                    nameof(FarmAnimal.hopOffset),
                    BindingFlags.Public | BindingFlags.Instance);
            MethodInfo Vector2GetZero = typeof(Vector2).GetProperty(
                    nameof(Vector2.Zero),
                    BindingFlags.Public | BindingFlags.Static).GetGetMethod();
            MethodInfo Vector2OpEquality = typeof(Vector2).GetMethod(
                    "op_Equality",
                    BindingFlags.Public | BindingFlags.Static);
            // the callvirt is just to disambiguate, hence the Advance(1) after
            cm.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt),
                    new(OpCodes.Ldloca_S),
                    new(OpCodes.Ldflda))
            .Advance(1)
            .ExtractLabels(out IEnumerable<Label> existing)
            .InsertAndAdvanceWithLabels(existing,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, hopOffsetField),
                    new(OpCodes.Call, Vector2GetZero),
                    new(OpCodes.Call, Vector2OpEquality),
                    new(OpCodes.Brtrue_S, offsetSkip))
            .MatchStartForward(
                    new CodeMatch(OpCodes.Ldloca_S),
                    new(OpCodes.Call),
                    new(OpCodes.Ldfld))
            .AddLabels(new []{offsetSkip});

            // the drawOnTop patch
            Label drawStart = generator.DefineLabel();
            Label drawSkip = generator.DefineLabel();
            FieldInfo drawOnTopField = typeof(FarmAnimal).GetField(
                    nameof(FarmAnimal.drawOnTop),
                    BindingFlags.Public | BindingFlags.Instance);
            // already in the correct spot
            cm.ExtractLabels(out IEnumerable<Label> bucket)
            .AddLabels(new []{drawStart})
            .InsertAndAdvanceWithLabels(bucket,
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, drawOnTopField),
                    new(OpCodes.Brfalse_S, drawStart),
                    new(OpCodes.Ldc_R4, 0.991f),
                    new(OpCodes.Br_S, drawSkip))
            .MatchStartForward(
                    new CodeMatch(OpCodes.Stloc_S))
            .AddLabels(new []{drawSkip});

            return cm.InstructionEnumeration();
        }
    }

}
