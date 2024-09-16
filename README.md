# Tater Toss - Throw Your Toddlers

This is a small Stardew Valley mod which patches in the ability to toss
children after they have grown older and left the crib. Now you can enjoy
throwing them for much longer!

Special thanks to Airyn, who pointed out to me that I could use vanilla frames
for the toss animations and therefore would not have to 1. create new art
or 2. worry about compatibility with the new art.

## Requirements

You will need SMAPI 4.0 or later and Stardew Valley 1.6+. Unzip this mod into
your Mods folder as usual, and enjoy!

## Compatibility

This mod uses a set of Harmony patches to do its job, so the usual caveats
apply for that (other mods which patch the same methods may conflict). This mod
uses only one skipping prefix, which is to prevent children from trying to walk
around during a toss; if the child skips an update like this, it will attempt
to update after landing.

I don't yet know of any specific conflicts. Please let me know if you find any!

## Known Issues

* In multiplayer, when you toss a child, the other players will see you pick up
    the child, but they won't see it fly into the air. This is vanilla behavior,
    since the child's jump status does not fully sync between players.
* In multiplayer, when tossing a crawling child, farmhands may see
    swaddled-infant frames instead of crawling ones (after the toss, the child
    will quickly return to normal). I believe this is also due to inconsistent
    sync, but I haven't figured out a workaround yet.
* In multiplayer, farmhands may not be able to fully stop a walking toddler
    when tossing them. Like above, I haven't found a solution yet.
