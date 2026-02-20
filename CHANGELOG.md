`1.3.12`
- attributes

`1.3.11`
- decreased update interval for more responsive UI elements (1s -> 0.1s), added config option if you prefer the old behavior; for noticeable effect requires Bloodcraft v1.11.14 or higher with fast updates enabled (Eclipsed = true)

`1.3.10`
- gamepad mode (position of quest windows and profession bars will change based on detected input)
- ability slot click-toggle for profession bars functional again

`1.3.9`
- preventing duplicate coroutine instances from being created if one is already present

`1.3.8`
- removing interact progress bar when unlocking familiars from buff entity on client
- changed abbreviation for PrimaryLifeLeech to PAL

`1.3.7`
- adjusted name trimming to target only vBloods for fam bar/quest targets
- handling for movementSpeed to show correctly on expertise bar and weapon tooltips

`1.3.6`
- small fix for legacy bar errors in console
- weapon expertise stats (most, not all) appear on tooltips

`1.3.5`
- Updated for VRising 1.1 compatibility

`1.3.3`
- small changes to accomodate minor version increases without needing to add more boilerplate logic to client/server
- minor recipe adjustments to match server-side changes for the client visually

`1.3.2`
- backwards compatible with Bloodcraft 1.5.3 (will show NPC spell cooldown's on shift when using 1.6.4 #soonYM, other changes below do not require additional information from Bloodcraft)
- weapon expertise stats show on tooltips accordingly (not all stats will show there for some reason but works for most)
- new recipe/salvage additions and changes added in 1.6.4 with ExtraRecipes enabled are best experienced with Eclipse
- profession bars no longer an eyesore (thin gold bar represents progress to max level, icon indicates profession, reordered to crafting profs then gathering profs)

`1.2.2`
- added bar for basic familiar info
- added bargraph of sorts for professions
- can toggle individual parts by clicking on ability slots 1-7

`1.1.2`
- removed unneeded dependency like I meant to for 1.1.1, oopsie

`1.1.1`
- class text under experience bar formatted more aesthetically
- improved positioning for UI elements at various resolutions (probably >_>)

`1.0.0`
- versioning for Thunderstore/sanity
- requires Bloodcraft 1.4.0

`0.2.1`
- fixed loop update if more than 3 bonus stats were chosen
- added quest icons for crafting and gathering

`0.2.0`
- added icons for quests based on normal/vblood target
- handled displaying stats in different locales

`0.1.4`
- making an attempt at handling scaling for UI elements at various resolutions, will need feedback on this although seems decent so far

`0.1.3`
- changed click detection to work off the same blood object that shows blood information when hovered over (this should fix any issues with errant clicks as if the blood object is not present it cannot be interacted with)
- quests should reliably be on the bottom right of the screen now

`0.1.2`
- clicking blood orb area if UI not active in-game will not do anything

`0.1.1`
- fixed extra bar at top of screen if only experience is enabled

`0.1.0`
- initial test release
- config values for experience, prestige, legacy, expertise, and quests (should be okay to mix and match but probably works best with all atm)
- progress bars for experience, legacies, expertise with bonus stats displayed beneath and prestige in bar header if enabled with current level on the left
- quest daily and weekly windows beneath bars
- click blood orb to turn UI on/off
