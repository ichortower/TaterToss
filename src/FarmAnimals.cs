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
                Utility.addSmokePuff(location,
                        __instance.Position + new Vector2(32f, __instance.yJumpOffset),
                        0,
                        __instance.yJumpVelocity / 8f,
                        0.01f, 0.75f, 0.01f);
            }
        }

        /*
         * Patch FarmAnimal.draw so it honors drawOnTop with a higher
         * layer_depth.
         */
        public static IEnumerable<CodeInstruction> FarmAnimal_draw_Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase original)
        {
            Label defaultStart = generator.DefineLabel();
            Label storeLocal = generator.DefineLabel();
            FieldInfo drawOnTopField = typeof(FarmAnimal).GetField(
                    nameof(FarmAnimal.drawOnTop),
                    BindingFlags.Public | BindingFlags.Instance);
            List<CodeInstruction> injection = new() {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, drawOnTopField),
                new(OpCodes.Brfalse_S, defaultStart),
                new(OpCodes.Ldc_R4, 0.991f),
                new(OpCodes.Br_S, storeLocal),
            };
            List<CodeInstruction> codes = instructions.ToList();
            List<CodeInstruction> modified = new();
            int foundIndex = -1;
            for (int i = 0; i < codes.Count; ++i) {
                var instr = codes[i];
                if (foundIndex >= 0 || i+2 >= codes.Count ||
                        codes[i].opcode != OpCodes.Ldloca_S ||
                        codes[i+1].opcode != OpCodes.Call ||
                        codes[i+2].opcode != OpCodes.Ldfld) {
                    modified.Add(instr);
                    continue;
                }
                modified.AddRange(injection);
                instr.labels.Add(defaultStart);
                modified.Add(instr);
                foundIndex = i;
            }
            if (foundIndex >= 0) {
                for (int i = foundIndex+injection.Count+1; i < modified.Count; ++i) {
                    if (modified[i].opcode == OpCodes.Stloc_S) {
                        modified[i].labels.Add(storeLocal);
                        break;
                    }
                }
            }
            return modified;
        }
    }

}
