[code][h1]Overview[/h1]
[list][h2]A Cities: Skylines mod that adjusts in-game lighting to look more natural.[/h2][/list]
[/code]

[img]https://i.imgur.com/0O9w9Ta.png[/img]

[HR][/HR]

[h1]Natural Lighting[/h1]
This mod is a "no-frills", "no performance impact", "no configuration needed" attempt to provide an instant visual improvement in the game by making the in-game lighting look more natural and the shadows on the buildings less harsh.

It is a replacement for Daylight Classic and Softer Shadows but made for the current version of the game (1.17.1-f4) and having some of the features removed that targeted the version of the game available in their day and age that work a bit janky in the current version or look out of place nowadays (in my opinion).

The mod provides translations for all it's settings menu items in all the languages supported by the game. If you find an incorrect translation, please leave a comment below or open an issue on GitHub, linked below.

[h2]Natural Lighting LUT[/h2]
The current version 1.1.0 of this mod includes the option to use this mod's own LUT for the best experience. You can enable/disable that option anytime in-game. But you can of course also use any other LUT you like.

[h2]Compatibility[/h2]
[b]Game Version[/b]: 1.17.1-f4
[b]Uses Harmony[/b]: no
[b]Changes Save Files[/b]: no

This mod has a built-in check for the presence of the following mods that provide similar functionality or would break it:
[list]
    [*][b]Daylight Classic[/b] - replaced by this mod
    [*][b]Softer Shadows[/b] - replaced by this mod
    [*][b]Render It![/b] - much more options and scope
    [*][b]Lumina[/b] - much more options and scope
[/list]
If one of these is detected, Natural Lighting will deactivate itself and show a message in it's settings panel in the game's options menu. You can then either disable these mods to use Natural Lighting or check the "Enable anyway" checkbox. In the latter case, Natural Lighting will apply its settings and possibly override the settings from the incompatible mods. This will not result in crashes but may result in the game's lighting becoming a mix between Natural Lighting and the other mod(s). However this might give you an interesting starting point: You can for example activate Natural Lighting in conjunction with Render It! or Lumina and use these mods to then fine tune the game's visuals to your liking. Recommended only for players who have more experience with the aforementioned mods.

[h2]Why not just use Render It! or Lumina?[/h2]
I like both mods. I used them for quite some time as Daylight Classic and Softer Shadows are very old and have their quirks with the current version of the game. But I could never get exactly the look I wanted and both mods gave me option paralysis. I just wanted a no-config, instant improvement for the game's very yellow tinted lighting. So I created Natural Lighting for myself and decided to share it here.

The second reason is that both mods use the [b]Harmony[/b] patching library which might cause [url=https://github.com/boformer/CitiesHarmony/issues/24]issues[/url] for people who play the game on Apple Silicon Hardware as it has some outstanding issues with platform support. Natural Lighting does not make use of [b]Harmony[/b].

[HR][/HR]

[h2]Source Code[/h2]
The source code of all my mods is available on [url=https://github.com/Bomret/CitiesSkylinesMods/tree/main/mods/NaturalLighting]GitHub[/url]. Feel free to report any issues and feedback there or here on Steam.