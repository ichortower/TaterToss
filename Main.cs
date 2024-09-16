using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Characters;
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
    internal sealed class Main : Mod
    {
        public static Main instance = null;
        public static string ModId = null;

        private static Vector2 SavedChildPosition;
        private static int SavedChildFacingDirection;
        private static int SavedFarmerFacingDirection;
        private static bool SavedWalkingInSquare;
        private static PathFindController SavedChildController;

        private static string TossingChildName = "";
        private static int SkippedUpdates = 0;
        private static bool WasHoldingHat = false;

        public override void Entry(IModHelper helper)
        {
            instance = this;
            ModId = this.ModManifest.UniqueID;
            Harmony harmony = new(ModId);
            try {
                MethodInfo Child_checkAction = typeof(Child).GetMethod(
                        nameof(Child.checkAction),
                        BindingFlags.Public | BindingFlags.Instance);
                MethodInfo Child_performToss = typeof(Child).GetMethod(
                        nameof(Child.performToss),
                        BindingFlags.Public | BindingFlags.Instance);
                MethodInfo Child_doneTossing = typeof(Child).GetMethod(
                        nameof(Child.doneTossing),
                        BindingFlags.Public | BindingFlags.Instance);
                MethodInfo Child_tenMinuteUpdate = typeof(Child).GetMethod(
                        nameof(Child.tenMinuteUpdate),
                        BindingFlags.Public | BindingFlags.Instance);
                MethodInfo Child_draw1 = typeof(Child).GetMethod(
                        nameof(Child.draw),
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new Type[]{typeof(SpriteBatch)},
                        null);
                MethodInfo Child_draw2 = typeof(Child).GetMethod(
                        nameof(Child.draw),
                        BindingFlags.Public | BindingFlags.Instance,
                        null, new Type[]{typeof(SpriteBatch), typeof(float)},
                        null);
                harmony.Patch(Child_checkAction,
                        prefix: new HarmonyMethod(typeof(Main),
                            "Child_checkAction_Prefix"),
                        postfix: new HarmonyMethod(typeof(Main),
                            "Child_checkAction_Postfix"));
                harmony.Patch(Child_performToss,
                        postfix: new HarmonyMethod(typeof(Main),
                            "Child_performToss_Postfix"));
                harmony.Patch(Child_doneTossing,
                        postfix: new HarmonyMethod(typeof(Main),
                            "Child_doneTossing_Postfix"));
                harmony.Patch(Child_tenMinuteUpdate,
                        prefix: new HarmonyMethod(typeof(Main),
                            "Child_tenMinuteUpdate_Prefix"));
                harmony.Patch(Child_draw1,
                        transpiler: new HarmonyMethod(typeof(Main),
                            "Child_draw1_Transpiler"));
                harmony.Patch(Child_draw2,
                        transpiler: new HarmonyMethod(typeof(Main),
                            "Child_draw2_Transpiler"));
            }
            catch (Exception e) {
                Monitor.Log("Could not apply required Harmony patch: " +
                        $"{e}", LogLevel.Error);
            }
        }

        /*
         * Determine if the player was holding a hat when interacting with the
         * child. We use this to be able to abort the toss.
         */
        public static void Child_checkAction_Prefix(Child __instance,
                Farmer who, GameLocation l)
        {
            if (__instance.Age >= 3 && who.Items.Count > who.CurrentToolIndex &&
                    who.Items[who.CurrentToolIndex] != null &&
                    who.Items[who.CurrentToolIndex] is Hat) {
                WasHoldingHat = true;
            }
            else {
                WasHoldingHat = false;
            }
        }

        /*
         * Vanilla does the toss in GameLocation, since it's a tile action
         * called "Crib". Older kids move around, so attach to the NPC inter-
         * action instead.
         */
        public static void Child_checkAction_Postfix(ref bool __result,
                Child __instance, Farmer who, GameLocation l)
        {
            // original returns true when you interact/"talk" to the kid
            if (__result) {
                return;
            }
            if (__instance.Age < 2) {
                return;
            }
            if (WasHoldingHat) {
                return;
            }
            if (__instance.idOfParent.Value != who.UniqueMultiplayerID) {
                return;
            }
            // we are skipping Child.toss, so duplicate some of its guards
            if (__instance.IsInvisible || Game1.timeOfDay >= 1800) {
                return;
            }
            SavedChildPosition = __instance.Position;
            SavedChildFacingDirection = GetChildFacingDirection(__instance);
            SavedFarmerFacingDirection = who.FacingDirection;
            SavedChildController = __instance.controller ?? null;
            SavedWalkingInSquare = __instance.IsWalkingInSquare;
            __instance.Halt();
            __instance.controller = null;
            __instance.IsWalkingInSquare = false;
            TossingChildName = __instance.Name;
            SkippedUpdates = 0;
            // this is likewise copying what Child.toss does
            if (who == Game1.player) {
                __instance.mutex.RequestLock(delegate {
                        __instance.performToss(who);
                });
            }
            else {
                __instance.performToss(who);
            }
        }

        private static int GetChildFacingDirection(Child c)
        {
            if (c.Age >= 3) {
                return c.FacingDirection;
            }
            int row = (c.Sprite.CurrentFrame - 24) / 4;
            int[] map = new int[] {2, 1, 0, 3, 2, 2};
            return map[row];
        }

        /*
         * Pretty blunt change here: for children of the older ages, replace
         * their animation lists and adjust their starting positions.
         */
        public static void Child_performToss_Postfix(Child __instance, Farmer who)
        {
            if (__instance.Age == 2) {
                __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> {
                    new(44, 200), new(45, 200), new(44, 200), new(46, 200)
                });
                __instance.Position += new Vector2(8f, 0f);
            }
            else if (__instance.Age >= 3) {
                __instance.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> {
                    new(17, 100), new(18, 100), new(19, 100), new(18, 100)
                });
                __instance.Position += new Vector2(16f, 0f);
            }
        }

        /*
         * After tossing an older child, restore the state that was saved
         * beforehand.
         */
        public static void Child_doneTossing_Postfix(Child __instance, Farmer who)
        {
            if (__instance.Age < 2) {
                return;
            }
            who.faceDirection(SavedFarmerFacingDirection);
            TossingChildName = null;
            __instance.Sprite.StopAnimation();
            __instance.Position = SavedChildPosition;
            __instance.faceDirection(SavedChildFacingDirection);
            if (__instance.Age == 2) {
                __instance.Sprite.CurrentFrame += 24;
            }
            __instance.IsWalkingInSquare = SavedWalkingInSquare;
            if (SavedChildController != null) {
                __instance.controller = SavedChildController;
            }
            if (SkippedUpdates > 0) {
                __instance.tenMinuteUpdate();
            }
        }

        /*
         * Block updating a child if it is being tossed when the clock ticks
         * over to the next 10 minutes.
         */
        public static bool Child_tenMinuteUpdate_Prefix(Child __instance)
        {
            if (TossingChildName?.Equals(__instance.Name) == true) {
                ++SkippedUpdates;
                return false;
            }
            return true;
        }

        /*
         * Fix a bug in the one-argument Child.draw: when drawing worn hats,
         * the depth value does not honor drawOnTop (which is set when a child
         * is tossed).
         */
        public static IEnumerable<CodeInstruction> Child_draw1_Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase original)
        {
            Label defaultStart = generator.DefineLabel();
            Label storeLocal = generator.DefineLabel();
            FieldInfo drawOnTopField = typeof(Character).GetField(
                    nameof(Character.drawOnTop),
                    BindingFlags.Public | BindingFlags.Instance);
            List<CodeInstruction> injection = new() {
                new(OpCodes.Ldfld, drawOnTopField),
                new(OpCodes.Brfalse_S, defaultStart),
                new(OpCodes.Ldc_R4, 0.993f),
                new(OpCodes.Br_S, storeLocal),
                new(OpCodes.Ldarg_0), // <- defaultStart goes here
            };
            injection[injection.Count-1].labels.Add(defaultStart);

            List<CodeInstruction> codes = instructions.ToList();
            List<CodeInstruction> modified = new();
            bool found = false;
            for (int i = 0; i < codes.Count; ++i) {
                var instr = codes[i];
                if (found || i < 2 || i+6 >= codes.Count ||
                        codes[i-2].opcode != OpCodes.Ret ||
                        codes[i-1].opcode != OpCodes.Ldarg_0 ||
                        codes[i].opcode != OpCodes.Call ||
                        codes[i+1].opcode != OpCodes.Ldfld) {
                    modified.Add(instr);
                    continue;
                }
                modified.AddRange(injection);
                modified.Add(instr);
                codes[i+5].labels.Add(storeLocal);
                found = true;
            }
            return modified;
        }

        /*
         * Fix a bug in the two-argument Child.draw: a line reading
         *    base.yJumpOffset = height_difference;
         * should say
         *    base.yJumpOffset += height_difference;
         */
        public static IEnumerable<CodeInstruction> Child_draw2_Transpiler(
                IEnumerable<CodeInstruction> instructions,
                ILGenerator generator,
                MethodBase original)
        {
            FieldInfo yJumpOffsetField = typeof(Character).GetField(
                    nameof(Character.yJumpOffset),
                    BindingFlags.Public | BindingFlags.Instance);
            List<CodeInstruction> injection = new() {
                new(OpCodes.Dup),
                new(OpCodes.Ldfld, yJumpOffsetField),
                new(OpCodes.Ldloc_S, (short)5),
                new(OpCodes.Add),
            };

            List<CodeInstruction> codes = instructions.ToList();
            List<CodeInstruction> modified = new();
            bool found = false;
            for (int i = 0; i < codes.Count; ++i) {
                var instr = codes[i];
                if (found || i < 1 || i+1 >= codes.Count ||
                        codes[i-1].opcode != OpCodes.Ldarg_0 ||
                        codes[i].opcode != OpCodes.Ldloc_S ||
                        codes[i+1].opcode != OpCodes.Stfld) {
                    modified.Add(instr);
                    continue;
                }
                modified.AddRange(injection);
                found = true;
            }
            return modified;
        }

    }

}
