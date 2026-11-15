# Star Wars: TIE Fighter (OpenTIE) – Mod Installer

Installs a modified build of [OpenTIE](https://github.com/elyosh/OpenTIE), the open-source
TIE Fighter engine. It adds widescreen, an X-Wing vs TIE Fighter-style HUD and gameplay, and fixes.
Almost every change has its own switch in OpenTIE's settings, so you can keep only what you like.

Built from OpenTIE v0.0.7 with 18 changes on top.

This is a patch, not a full build: **it only works on an unchanged OpenTIE v0.0.7**, the official
Windows release. Other versions are refused, and nothing is changed.

## Requirements

- Star Wars: TIE Fighter (GOG or Steam), with **OpenTIE v0.0.7 installed, set up and running**.
  Download `opentie-0.0.7-windows-x86_64-release.zip` from
  <https://github.com/elyosh/OpenTIE/releases/tag/v0.0.7> and unzip it into the game folder.
  The installer patches that OpenTIE folder (for example `Opentie` inside the game folder).
- 64-bit Windows 10 or 11 with .NET Framework 4.5 or later (included in Windows 10 and 11).
- Optional: **Star Wars: X-Wing vs TIE Fighter** (GOG or Steam). The XvT message font, the engine
  wash sounds and the XvT control sounds are XvT's own files. They can't be shipped here, so the
  installer copies them from your XvT folder. Without XvT, TIE Fighter's font is used and those
  sounds stay silent. Everything else still works.

## How to install

1. Close OpenTIE.
2. Run `TIEFighterModInstaller.exe`. It looks for your game and XvT folders. If it doesn't find them, click **Browse...**.
3. Click **Install**.

The installer:
- copies your OpenTIE folder to `<folder>-original` (for example `Opentie-original`), once
- zips your OpenTIE settings and pilots (`%APPDATA%\TotallyOpen\OpenTIE`) to `OpenTIE-backup-<date>.zip` next to that folder
- checks that every file it patches is the OpenTIE v0.0.7 original, then patches `OpenTIE.exe` and
  `resources\config.yaml` and adds this README. Everything else is left as it is.
- copies the XvT files it needs into `resources\` inside your OpenTIE folder

Run it again to repair, or to add the XvT files later. **Restore original OpenTIE** puts the
backup back and renames the modded folder to `<folder>-mods-removed`.

## What's included

### Video settings
| Setting | What it does |
|---|---|
| **Widescreen Flight View** | The flight view fills a widescreen window instead of being pillarboxed to 4:3. |
| **Stretch Cockpit to Widescreen** | The 4:3 cockpit art fills the width. The 3D view isn't stretched. |
| **Widescreen Cockpit-Off HUD** | With the cockpit off, the side HUD elements move out to the screen edges. |
| **XvT Cockpit Readouts** | XvT's CMD text for starfighters (order word and target). Messages move to the top in XvT's font, status messages sit above the CMD, and your own key confirmations are hidden. The cockpit art fills the old message strip. Also adds XvT's four threat lights above the gunsight, XvT's craft lists (**F** / **Shift+F**) and XvT's damage pop-up (**D**: Up/Down to select, Enter to repair first). |

### Game settings
| Setting | What it does |
|---|---|
| **Laser Convergence** | X-Wing Alliance-style convergence. Lasers converge on the target ahead, or at the distance you pick. With no target they fire parallel. |
| **Laser Rate and Speed** | XvT's faster refire, and bolts twice as fast for half the time (same range). Applies to all craft. |
| **Acceleration and Braking** | XvT's per-craft acceleration. Big throttle cuts brake fast. |
| **AI Laser & Ion Linking** | AI craft fire lasers and ion cannons together. Choose Off, Friendly Only or All Craft. |
| **Shield Hits** | XvT: a hit draws energy from the other shield, then the shields balance. |
| **Turning** | XvT: tightest turns at 1/3 throttle, and the power settings change the rate. |
| **Engine Wash** | XvT: damage and a rumble sound behind capital ship engines. The sound needs XvT. |
| **Shields at Launch** | Start each mission with double shields. |
| **Recharge Rates** | XvT: faster TIE Fighter and TIE Bomber laser recharge, and faster Y-Wing shield recharge. |
| **Empty Warhead Launchers** | W skips empty launchers. After your last warhead you switch back to cannons. |
| **Capital Ship Missiles** | Capital ships fire missiles at you only after you've kept them targeted for 2 seconds, as in XvT. Scrolling past them with T no longer sets them off. |
| **Twist Roll** | X-Wing Alliance's roll rate for each craft on a joystick twist, and turns still bank while you twist. Keyboard roll is unchanged. |
| **Ion Hits** | XvT's ion model: sturdier craft soak up more ion hits before their systems drop, and ion hits on you hit your systems, not your hull. |
| **Ion Cannons vs TIEs** | Ion-disabled TIEs stay disabled instead of blowing up. |
| **Show Bonus Goals in Campaign** | The goals screen lists bonus goals in Tour of Duty missions, as it does in the combat simulator. |

### Audio settings
| Setting | What it does |
|---|---|
| **XvT Power and Throttle Sounds** | XvT's tones and clicks for power, throttle and weapon keys. *Needs XvT.* |

### Always on
- **Lasers and ion cannons fire linked**: press X again after "fire linked" (X-Wing Alliance style).
- **E** cycles through craft attacking you. **I** targets the nearest incoming warhead, and component tracking moves to **Alt+I**.
- **Shift+'**: all cannon energy to shields.
- Fixes: with CD music, the menu music no longer keeps looping under the flight music. Dying craft no longer flip 180° and ram you. Dying ships no longer keep firing their turrets. The craft list shows shield and hull percentages above 127 correctly.

## Source code

OpenTIE is free software (GPL-3). The `source` folder has these changes as git patches against
the upstream commit they were built on, with instructions in `SOURCE.txt`.

## Credits

OpenTIE is by elyosh and contributors. These changes were made for personal use and are shared as is.
Star Wars: TIE Fighter and X-Wing vs TIE Fighter are © Lucasfilm Ltd. No game files are included.
