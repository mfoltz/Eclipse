## Table of Contents

- [Sponsors](#sponsors)
- [Features](#features)
- [Configuration](#configuration)

## Sponsor this project

[![patreon](https://i.imgur.com/u6aAqeL.png)](https://www.patreon.com/join/4865914)  [![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/zfolmt)

## Sponsors

Jairon O.; Odjit; Jera; Kokuren TCG and Gaming Shop; Rexxn; Eduardo G.; DirtyMike; Imperivm Draconis; Geoffrey D.; SirSaia; Robin C.; K Simp; Colin F.; Jade K.;

## Features

- Currently supports progress bars and info for leveling, legacies, expertise, familiars, professions, and quest progress. Also shows shift slot ability when being used with cooldown. Blood orb can be clicked to enable/disable UI.
- Can disable individual UI elements via config or by clicking on your abilities (1-7, each disables and enables one UI element when clicked (clicking on the shift slot disables itself until reactivated by blood orb since can't click it after it's not there to toggle back).
 
## Configuration

### UIOptions

- **Experience Bar**: `ExperienceBar` (bool, default: true)  
  Enable or disable the experience bar.
- **Show Prestige**: `ShowPrestige` (bool, default: true)  
  Enable or disable showing prestige levels.
- **Legacy Bar**: `LegacyBar` (bool, default: true)  
  Enable or disable the legacy bar.
- **Expertise Bar**: `ExpertiseBar` (bool, default: true)  
  Enable or disable the expertise bar.
- **Familiars**: `Familiars` (bool, default: true)  
  Enable or disable the familiar bar.
- **Quest Tracker**: `QuestTracker` (bool, default: true)  
  Enable or disable the quest windows.
- **Professions**: `Professions` (bool, default: true)  
  Enable or disable the profession bars.
- **ShiftSlot**: `ShiftSlot` (bool, default: true)  
  Enable or disable the shift slot appearing when applicable.
