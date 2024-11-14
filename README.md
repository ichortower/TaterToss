# Tater Toss - Throw Your Loved Ones

This is a small Stardew Valley mod which patches in the ability to toss
children after they have grown older and left the crib. Now you can enjoy
throwing them for much longer!

(Special thanks to Airyn for pointing out that I did not need to create new
animation frames, saving many potential headaches)

Starting in version 1.1.0, this mod now also allows you to throw farm animals.
To throw animals, you will have to hold a (configurable) hotkey: just
interacting with an animal after it has been pet opens a menu, so I needed
a way to determine the desired behavior.

Tossing farm animals will increase their happiness slightly (once per day
per farmer; each active farmer contributes a share of the total amount).

(Special thanks to Airyn for goading me into adding throwable chickens. I
decided not to limit it to chickens)

More loved ones may become throwable in the future: pets, perhaps, or maybe
even spouses.


## Requirements

You will need SMAPI 4.0 or later and Stardew Valley 1.6+. Unzip this mod into
your Mods folder as usual, and enjoy!


## Configuration

In 1.2.0 the config settings have changed. For best results, you may need to
delete your previous config if you are updating to this version.

1. `ThrowKey`: (default *LeftShift*) which key to hold down in order to toss
  farm animals. If the key is held, interacting with the animal will toss it
  (after it has already been pet). Otherwise, you will see the normal animal
  menu, in order to rename, sell, prevent births, etc.
2. `UseKeyForChildren`: (default false) if set to true, the ThrowKey will also
  be required in order to throw children. By default (false), no modifier key
  is needed.
3. `Blocklist`: (default empty) a list of strings containing animal types
  (e.g. "White Cow") and/or children types ("Crawler", "Toddler") which should
  not be tossed. Any type listed here will not permit tossing.

As before, these can be set using 
[Generic Mod Config Menu](https://github.com/spacechase0/StardewValleyMods/tree/develop/GenericModConfigMenu),
but the list option can be a bit cumbersome.


## Compatibility

This mod uses a set of Harmony patches to do its job, so the usual caveats
apply for that (other mods which patch the same methods may conflict). This mod
uses only one skipping prefix, which is to prevent children from trying to walk
around during a toss; if the child skips an update like this, it will attempt
to update after landing.

This should be compatible with any mod that changes the sprites of your
children or farm animals, since it uses vanilla animation frames. It should
also be compatible with all farm animals, since they all use the same code and
I did not make any part of this specific to chickens. However, it won't work
with any of the mods that turn your children into NPCs, since (I believe) they
stop being the Child type of NPC that this mod works on.

I don't yet know of any specific conflicts. Please let me know if you find any!


## Known Issues

* In multiplayer, when tossing a crawling child, farmhands may see
  swaddled-infant frames instead of crawling ones (after the toss, the child
  will quickly return to normal). I believe this is also due to inconsistent
  sync, but I haven't figured out a workaround yet.
* In multiplayer, farmhands may not be able to fully stop a walking toddler
  when tossing them. Like above, I haven't found a solution yet.
