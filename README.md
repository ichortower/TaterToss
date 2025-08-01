# Tater Toss - Throw Your Loved Ones

In Stardew Valley, you can pick up your baby from the crib (once they're big
enough to be awake) and toss them into the air. This is fun for them, and
hopefully for you, but once they grow older and leave the crib, you can no
longer do it. Fortunately, this mod is here to help.

When installed, this mod patches in the ability to toss:

- Children of crawling and toddling ages
- Farm animals (of any type)
- Pets (of any type)
- Spouses

More loved ones may become tossable in the future, if I can think of any more.


## Requirements

You will need SMAPI 4.0 or later and Stardew Valley 1.6+. Unzip this mod into
your Mods folder as usual, and enjoy!


## Special Thanks

- Airyn, for saving me many headaches by pointing out I could use vanilla
  animation frames
- Airyn (again), for goading me into adding throwable chickens, which became
  throwable everything
- Claire, for having a passion for yeeting


## How to Toss

After exhausting normal interaction with your loved one, interact with them
again to toss. This works just like tossing a child from the crib, so you will
pause and throw them, then put them down (time will pass during the throw).
Loved ones can be tossed only if they are not sleeping, and depending on the
type of loved one, certain other restrictions apply:

- **Children**: if the `UseKeyForChildren` config setting (see below) is set
  to true, you will have to hold the throw key while interacting in order to
  toss.
- **Pets**: if the `UseKeyForPets` config setting (see below) is set to true,
  you will have to hold the throw key while interacting in order to toss.
- **Farm Animals**: you must use the throw key to toss a farm animal.
- **Spouses**: you must use the throw key to toss a spouse. In addition, you
  may only throw your own spouse (if you have multiple spouses due to a
  polyamory mod, it should work for all of them equally, but it won't work
  on another player's spouse). The spouse must also be an NPC (no throwing
  other players).

When you toss any kind of loved one, they will gain a small amount of bonus
friendship with you, if possible (once per day).

You can configure the throw key, and in addition you can specify types and/or
names of loved ones who should not be throwable using the blocklist option
(see below).


## Configuration

Tater Toss uses the following config values:

- `ThrowKey`: (default *LeftShift*) which key to hold down in order to toss
  loved ones who require it. For farm animals, this bypasses the animal menu
  (renaming, selling, etc.); for spouses, this bypasses kissing (in truth,
  both work by instantly aborting the action). Pets and children in vanilla
  do not have an always-available interaction that requires bypassing, but
  you can choose to require the key if you wish.
- `UseKeyForChildren`: (default false) if set to true, the ThrowKey will also
  be required in order to throw children. By default (false), no modifier key
  is needed.
- `UseKeyForPets`: (default false) if set to true, the ThrowKey will also
  be required in order to throw pets. By default (false), no modifier key is
  needed.
- `Blocklist`: (default empty) a list of strings specifying loved ones who
  should not be tossed. Each string can represent one of several properties,
  depending on the type of loved one:
    - Children: an age bracket ("Crawler" or "Toddler"), or a particular
      child's name.
    - Pets: a type (e.g. "Cat"), a breed (e.g. "ichortower.IchorsCat.Cat"),
      or a particular pet's name.
    - Farm Animals: a type (e.g. "White Cow") or a particular animal's name.
    - Spouses: an internal name (e.g. "ichortower.HatMouseLacey_Lacey") or
      a display name (e.g. "Lacey").

  Any match will prevent the toss and log the block to your SMAPI console. For
  example, "Brown Cow" will block tossing for all brown cows, but "Zelda" will
  block any loved one named Zelda (a child, a pet, a cow, etc.).

These can be set using 
[Generic Mod Config Menu](https://github.com/spacechase0/StardewValleyMods/tree/develop/GenericModConfigMenu),
but the blocklist option can be a bit cumbersome.


## Compatibility

This mod uses a set of Harmony patches to do its job, so the usual caveats
apply for that (other mods which patch the same methods may conflict). This mod
uses only two skipping prefixes, which are to prevent children and pets from
trying to walk around during a toss; if a child skips an update like this, it
will attempt to update after landing (pets just ignore it).

This should be compatible with any mod that changes the sprites of your
children or farm animals, since it uses vanilla animation frames. It should
also be compatible with all farm animals, all pets, and all custom NPC spouses,
since they all use the same code and I intentionally set it up to work as
generically as possible. However, it won't (yet?) work with any of the mods
that turn your children into NPCs, since (I believe) they stop being the Child
type of NPC, and the NPC toss code only permits spouses.

I am given to understand that this mod lets you toss animals created by
[the MEEP mod](https://github.com/AlanDavison/StardewValleyMods/tree/master/MappingExtensionsAndExtraProperties),
such as [Baron Munchington](https://www.nexusmods.com/stardewvalley/mods/5787).
This was not done on purpose, but it's a feature, not a bug.

I don't yet know of any specific conflicts. Please let me know if you find any!


## Known Issues

* In multiplayer, when tossing a crawling child, farmhands may see
  swaddled-infant frames instead of crawling ones (after the toss, the child
  will quickly return to normal). I believe this is also due to inconsistent
  sync, but I haven't figured out a workaround yet.
* In multiplayer, farmhands may not be able to fully stop a walking toddler
  when tossing them. Like above, I haven't found a solution yet.
* If an NPC spouse is at a spot with schedule dialogue (which repeats
  infinitely), there is no way to exhaust interaction with them, so tossing is
  not possible until they move.
