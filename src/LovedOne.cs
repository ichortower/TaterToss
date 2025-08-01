using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Network;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;

namespace ichortower.TaterToss;

internal sealed class LovedOne
{
    internal static HashSet<Character> BeingTossed = new();
    internal static HashSet<Character> EarnedTossFriendship = new();
    internal const int frameTime = 125;

    internal static SavedState GetSave(Character love)
    {
        return new SavedState {
            Position = love.Position,
            FacingDirection = love.FacingDirection,
            WalkingInSquare = (love as NPC)?.IsWalkingInSquare ?? false,
            PetBehavior = (love as Pet)?.CurrentBehavior ?? null,
            Controller = love.controller,
        };
    }

    internal static void RestoreSave(Character love, SavedState save)
    {
        love.Position = save.Position;
        love.faceDirection(save.FacingDirection);
        if (love is NPC n) {
            n.IsWalkingInSquare = save.WalkingInSquare;
        }
        if (love is Pet p) {
            p.CurrentBehavior = save.PetBehavior;
        }
        // some loved ones (NPC spouses, in particular) may have gotten a new
        // controller during a toss. if they have, just leave it.
        if (save.Controller is not null && love.controller is null) {
            love.controller = save.Controller;
        }
    }

    internal static void TryGiveFriendship(Character love, Farmer who)
    {
        if (!EarnedTossFriendship.Add(love)) {
            return;
        }
        float fpoints = 10f / Game1.getOnlineFarmers().Count;
        if (love is Pet p) {
            p.friendshipTowardFarmer.Value = Math.Min(1000,
                    p.friendshipTowardFarmer.Value + (int)fpoints);
        }
        else if (love is FarmAnimal fa) {
            fa.friendshipTowardFarmer.Value = Math.Min(1000,
                    fa.friendshipTowardFarmer.Value + (int)fpoints);
        }
        else if (love is NPC n) {
            who.changeFriendship(10, n);
        }
    }

    /*
     * An implementation of toss for characters who don't already have one
     * (i.e. everyone except Child).
     */
    internal static void PerformToss(Character love, Farmer who, NetMutex mutex)
    {
        Action call = delegate {
            ReallyPerformToss(love, who, mutex);
        };
        if (who == Game1.player) {
            mutex.RequestLock(call);
        }
        else {
            call();
        }
    }

    internal static void ReallyPerformToss(Character love, Farmer who, NetMutex mutex)
    {
        if (!BeingTossed.Add(love)) {
            Main.instance.Monitor.Log($"{love.Name} is already being tossed", LogLevel.Info);
            return;
        }

        int yOffset = 0;
        who.forceTimePass = true;
        who.faceDirection(2);
        who.FarmerSprite.PauseForSingleAnimation = false;
        SavedState save = GetSave(love);
        if (love is NPC n) {
            n.IsWalkingInSquare = false;
            yOffset = 20;
        }
        if (love is Pet p2) {
            p2.CurrentBehavior = null;
            yOffset = 20;
        }
        if (love is FarmAnimal) {
            yOffset = -8;
        }
        love.controller = null;
        love.Halt();
        love.FacingDirection = -1;
        Vector2 pos = who.Position;
        pos.X -= (love.Sprite.SpriteWidth - who.Sprite.SpriteWidth) * 2;
        pos.Y -= (who.Sprite.SpriteHeight * 4 +
                (love.Sprite.SpriteHeight - who.Sprite.SpriteHeight) * 2);
        pos.Y += yOffset;
        love.Position = pos;

        float throwVelocity = 30f;
        int freezeTime = 2500;
        string throwSound = "crit";
        if (Game1.random.NextDouble() >= 0.01 || who.stats?.Get("timesTossedBaby") <= 3) {
            throwVelocity = Game1.random.Next(12, 19);
            throwSound = "dwop";
            freezeTime = 1500;
        }
        // using the heck out of this closure, thanks delegates
        AnimatedSprite.endOfAnimationBehavior FinishToss = delegate (Farmer who) {
            who.forceTimePass = false;
            who.CanMove = true;
            who.forceCanMove();
            who.faceDirection(2);
            BeingTossed.Remove(love);
            love.Sprite.StopAnimation();
            RestoreSave(love, save);
            love.drawOnTop = false;
            love.doEmote(20);
            TryGiveFriendship(love, who);
            Game1.playSound("tinyWhip");
            if (mutex.IsLockHeld()) {
                mutex.ReleaseLock();
            }
        };

        who.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[1]{
            new(57, freezeTime, secondaryArm: false, flip: false,
                    FinishToss, behaviorAtEndOfFrame: true)
        });
        who.freezePause = freezeTime;
        who.CanMove = false;
        love.yJumpVelocity = throwVelocity;
        love.yJumpOffset = -1;
        love.drawOnTop = true;
        love.Sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> {
            new(0, frameTime),
            new(1, frameTime),
            new(2, frameTime),
            new(3, frameTime),
        });
        love.Sprite.loop = true;
        TossSync.SendToss(love, who.currentLocation, throwVelocity);
        Game1.playSound(throwSound);
    }
}

internal struct SavedState
{
    public Vector2 Position;
    public int FacingDirection;
    public bool WalkingInSquare;
    public string PetBehavior;
    public PathFindController Controller;
}
