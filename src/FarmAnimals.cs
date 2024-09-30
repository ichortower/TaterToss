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
        private static NetMutex CurrentMutex = null;

        public static HashSet<long> EarnedTossFriendship = new();

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
            if (Main.Config.AnimalBlocklist.Contains(__instance.type.Value)) {
                Main.instance.Monitor.Log("Blocked toss of animal type" +
                        $" {__instance.type.Value}, according to block list.",
                        LogLevel.Info);
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
            if (!Main.Config.AnimalThrowKey.IsDown()) {
                return;
            }
            // skip the AnimalQueryMenu by exiting it immediately
            Game1.exitActiveMenu();
            RequestToss(__instance, who);
        }

        private static void RequestToss(FarmAnimal fa, Farmer who)
        {
            CurrentMutex = GuaranteeAnimalMutex(fa);
            if (who == Game1.player) {
                CurrentMutex.RequestLock(delegate {
                    PerformAnimalToss(fa, who);
                });
            }
            else {
                PerformAnimalToss(fa, who);
            }
        }

        private static void PerformAnimalToss(FarmAnimal fa, Farmer who)
        {
            who.forceTimePass = true;
            who.faceDirection(2);
            who.FarmerSprite.PauseForSingleAnimation = false;
            Vector2 SavedPosition = fa.Position;
            Vector2 pos = who.Position;
            pos.X -= (fa.Sprite.SpriteWidth - who.Sprite.SpriteWidth) * 2;
            pos.Y -= (who.Sprite.SpriteHeight * 4 + (fa.Sprite.SpriteHeight - who.Sprite.SpriteHeight) * 2);
            fa.Position = pos;
            float throwVelocity = 30f;
            int freezeTime = 2500;
            string throwSound = "crit";
            if (Game1.random.NextDouble() >= 0.01 || who.stats?.Get("timesTossedBaby") <= 3) {
                throwVelocity = Game1.random.Next(12, 19);
                throwSound = "dwop";
                freezeTime = 1500;
            }
            // delegate here for closure access to fa
            AnimatedSprite.endOfAnimationBehavior FinishAnimalToss = delegate (Farmer who) {
                who.forceTimePass = false;
                who.CanMove = true;
                who.forceCanMove();
                who.faceDirection(2);
                fa.drawOnTop = false;
                fa.doEmote(20);
                fa.Sprite.StopAnimation();
                fa.Position = SavedPosition;
                if (EarnedTossFriendship.Add(fa.myID.Value)) {
                    float fpoints = 10f / Game1.getOnlineFarmers().Count;
                    fa.friendshipTowardFarmer.Value = Math.Min(1000,
                            fa.friendshipTowardFarmer.Value + (int)fpoints);
                }
                Game1.playSound("tinyWhip");
                if (CurrentMutex.IsLockHeld()) {
                    CurrentMutex.ReleaseLock();
                }
                CurrentMutex = null;
            };

            who.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[1]{
                new(57, freezeTime, secondaryArm: false, flip: false,
                        FinishAnimalToss, behaviorAtEndOfFrame: true)
            });
            who.freezePause = freezeTime;
            who.CanMove = false;
            fa.yJumpVelocity = throwVelocity;
            fa.yJumpOffset = -1;
            fa.drawOnTop = true;
            fa.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> {
                    new(0, 100),
                    new(1, 100),
                    new(2, 100),
                    new(3, 100),
            });
            Game1.playSound(throwSound);
            /*
            string animalSound = fa.GetAnimalData()?.Sound;
            if (animalSound != null) {
                DelayedAction.functionAfterDelay(delegate {
                    Game1.playSound(animalSound);
                }, 220);
            }
            */
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
