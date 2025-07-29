using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using System.Linq;

namespace ichortower.TaterToss
{
    internal sealed class TossSync
    {
        public static void SendToss(Character who, GameLocation location, float velocity)
        {
            TossStruct msg;
            msg.UniqueName = who.Name;
            msg.LocationName = location.NameOrUniqueName;
            msg.Velocity = velocity;
            if (who is Child) {
                msg.Type = LovedOneType.Child;
            }
            else if (who is FarmAnimal) {
                msg.Type = LovedOneType.FarmAnimal;
            }
            else if (who is Pet) {
                msg.Type = LovedOneType.Pet;
            }
            else {
                Main.instance.Monitor.Log("Blocked sending toss of " +
                        $"unsupported Character type {who.GetType().Name}",
                        LogLevel.Warn);
                return;
            }
            Main.instance.Helper.Multiplayer.SendMessage(msg, "Toss",
                    modIDs: new[] {Main.ModId});
        }

        public static void ReceiveToss(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID != Main.ModId) {
                return;
            }
            if (e.Type != "Toss") {
                return;
            }
            TossStruct msg = e.ReadAs<TossStruct>();
            GameLocation loc = Game1.getLocationFromName(msg.LocationName);
            if (loc is null) {
                return;
            }
            if (msg.Type == LovedOneType.Child) {
                foreach (NPC who in loc.characters) {
                    if (who is Child ch && ch.Name == msg.UniqueName) {
                        ch.yJumpVelocity = msg.Velocity;
                        ch.yJumpOffset = -1;
                    }
                }
            }
            else if (msg.Type == LovedOneType.FarmAnimal) {
                foreach (FarmAnimal who in loc.animals.Values) {
                    if (who.Name == msg.UniqueName) {
                        who.yJumpVelocity = msg.Velocity;
                        who.yJumpOffset = -1;
                    }
                }
            }
            else if (msg.Type == LovedOneType.Pet) {
                foreach (Pet who in loc.characters) {
                    if (who is Pet p && p.Name == msg.UniqueName) {
                        p.yJumpVelocity = msg.Velocity;
                        p.yJumpOffset = -1;
                    }
                }
            }
        }
    }

    internal struct TossStruct
    {
        public LovedOneType Type;
        public string UniqueName;
        public string LocationName;
        public float Velocity;

        public TossStruct(LovedOneType type, string name, string locationName, float velocity)
        {
            Type = type;
            UniqueName = name;
            LocationName = locationName;
            Velocity = velocity;
        }
    }

    internal enum LovedOneType {
        Child,
        FarmAnimal,
        Pet,
    }
}
