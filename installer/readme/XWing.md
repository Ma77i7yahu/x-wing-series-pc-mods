# Star Wars: X-Wing (OpenXW) – Mod Installer

Installs a modified build of [OpenXW](https://github.com/elyosh/OpenXW), the open-source
X-Wing engine. It adds widescreen, X-Wing vs TIE Fighter cockpits, HUD and gameplay, and fixes.
Almost every change has its own switch in OpenXW's settings, so you can keep only what you like.

Built from OpenXW v0.0.3 (plus three later upstream commits) with 59 changes on top.

This is a patch, not a full build: **it only works on an unchanged OpenXW v0.0.3**, the official
Windows release. Other versions are refused, and nothing is changed.

## Requirements

- Star Wars: X-Wing (GOG or Steam), with **OpenXW v0.0.3 installed, set up and running**.
  Download `openxw-0.0.3-windows-x86_64-release.zip` from
  <https://github.com/elyosh/OpenXW/releases/tag/v0.0.3> and unzip it into the game folder.
  The installer patches that OpenXW folder (for example `Openxw` inside the game folder).
- 64-bit Windows 10 or 11 with .NET Framework 4.5 or later (included in Windows 10 and 11).
- Optional: **Star Wars: X-Wing vs TIE Fighter** (GOG or Steam; both include Balance of Power).
  The XvT cockpits, threat display, XvT laser sounds, engine wash sound and control sounds
  use XvT's own art and sounds. They can't be shipped here, so the installer copies them from
  your XvT folder. Without XvT those features stay off, and everything else still works.

## How to install

1. Close OpenXW.
2. Run `XWingModInstaller.exe`. It looks for your game and XvT folders. If it doesn't find them, click **Browse...**.
3. Click **Install**.

The installer:
- copies your OpenXW folder to `<folder>-original` (for example `Openxw-original`), once
- zips your OpenXW settings and pilots (`%APPDATA%\TotallyOpen\OpenXW`) to `OpenXW-backup-<date>.zip` next to that folder
- checks that every file it patches is the OpenXW v0.0.3 original, then patches `OpenXW.exe` and
  `resources\config.yaml` and adds this README. Everything else, including files you added
  there like ROMs or SoundFonts, is left as it is.
- copies the XvT files it needs into `resources\` inside your OpenXW folder

Run it again to repair, or to add the XvT files later. **Restore original OpenXW** puts the
backup back and renames the modded folder to `<folder>-mods-removed`.

Note: the modified build upgrades your pilot file. If you restore an older OpenXW and it
can't read your pilot, unzip the settings backup into `%APPDATA%\TotallyOpen\OpenXW`.

## What's included

### Video settings
| Setting | What it does |
|---|---|
| **Stretch Cockpit to Widescreen** | The 4:3 cockpit art fills a widescreen window. The 3D view isn't stretched. |
| **Widescreen Cockpit-Off HUD** | With the cockpit off, the HUD panels move out to the screen edges. |
| **XvT Targeting Computer** | XvT's CMD: a live 3D view of the target with SHD/HULL/SYS, its orders and its target, cargo for starships, and DIST. |
| **XvT Cockpit and HUD** | XvT's Balance of Power cockpits for the X-, Y-, A- and B-Wing in every view, with XvT's panel text, gauges, gunsight, threat lights and fonts. Messages are centred in XvT's font, and your own key confirmations are hidden. *Needs XvT.* |

### Audio settings
| Setting | What it does |
|---|---|
| **XvT Laser and Ion Sounds** | XvT's laser, ion and turbolaser sounds. *Needs XvT.* |
| **XvT Power and Throttle Sounds** | XvT's tones and clicks for recharge rates, energy transfers, shields, throttle and weapon keys. *Needs XvT.* |

### Game settings
| Setting | What it does |
|---|---|
| **Default Convergence Distance** | X-Wing Alliance-style laser convergence. Lasers converge on the target ahead, or at the distance you pick. With no target they fire parallel. |
| **Laser Rate and Speed** | XvT's refire rate, bolt speed, range, launch point and gun positions. |
| **Acceleration and Braking** | XvT's per-craft acceleration. Big throttle cuts brake fast. |
| **Turning** | XvT's throttle-dependent turning: tightest at 1/3 throttle, and the power settings change the rate. |
| **Recharge Rates** | XvT's extras: Y-Wing shields recharge twice as fast, and TIE lasers half again as fast. |
| **Shield Hits** | XvT's self-balancing shields. |
| **Shields at Launch** | Start missions with double (overcharged) shields, as in XvT. |
| **Engine Wash** | XvT's engine wash: damage and a rumble sound behind capital ship and freighter engines. The sound needs XvT. |
| **Ion Cannons vs TIEs** | Ion-disabled TIEs stay disabled instead of blowing up. |
| **Ion Hits** | XvT's ion model: sturdier craft soak up more ion hits before their systems drop (corvettes, frigates and Star Destroyers take many), and disabled craft lose their shields. |
| **Starship Components** | XvT's targetable components on starships: turrets, laser guns, shield generators and more. **,** / **Shift+,** cycles them, the CMD shows the component and a marker, and warheads home on it. Hits reach a component even with the shields up, as in TIE Fighter and X-Wing Alliance. Destroyed guns stop firing. |
| **Destroyed Craft** | Destroyed craft always break up as in XvT (wings come off, the wreck tumbles for a few seconds) instead of X-Wing's instant explosion. |
| **Twist Roll** | X-Wing Alliance's roll rate for each craft on a joystick twist, and turns still bank while you twist, with XWA's 8% twist dead zone. Keyboard roll is unchanged. |
| **AI Laser & Ion Linking** | AI craft fire lasers and ion cannons together. Choose Off, Friendly Only or All Craft. |
| **Empty Warhead Launchers** | W skips empty launchers. After your last warhead you switch back to cannons. |
| **Damage Report (D)** | XvT's damage pop-up: Up/Down selects a system and Enter makes it the first to repair. Or keep X-Wing's screen. |
| **Message Time** | Messages stay up half as long. |
| **Killed or Captured on Tour** | Keep your pre-mission pilot instead of losing the tour, as in TIE Fighter. |

### Always on
- **Lasers and ion cannons fire linked**: press X again after "fire linked" (X-Wing Alliance style).
- **Z** opens XvT's threat display (target camera, orders, destination, distance, boarding time).
- XvT targeting keys: **U** newest craft, **I** nearest incoming warhead, **A** craft attacking your target, and **R** also picks shuttles, transports and tugs. **E** cycles through craft attacking you, exactly as in XvT (skipping dying and departing craft), and **R** skips disabled craft.
- **F** / **Shift+F**: XvT's friendly and enemy craft lists, scrolled with Up/Down.
- **Shift+'**: all cannon energy to shields.
- **Alt+T**: TIE Fighter's time acceleration (1x/2x/4x).
- Fixes: dying craft no longer flip 180° and ram you. Dying ships no longer keep firing their turrets. Messages no longer repeat. Destroyed craft no longer turn toward you. The craft list shows shield and hull percentages above 127 correctly.

### Key changes
S-foils moved to **V** (replay view to **Shift+V**). Identify moved to **Alt+I**, and crosshair targeting to **Alt+U**.

## Source code

OpenXW is free software. The `source` folder has these changes as git patches against the
upstream commit they were built on, with instructions in `SOURCE.txt`.

**Licence note:** the OpenXW repository has no LICENSE file of its own. It links the GPL-3
Aeron and Landru libraries, so these changes are offered under GPL-3, but OpenXW's own licence
hasn't been stated by its author. Check with upstream before redistributing OpenXW itself.

## Credits

OpenXW is by elyosh and contributors. These changes were made for personal use and are shared as is.
Star Wars: X-Wing and X-Wing vs TIE Fighter are © Lucasfilm Ltd. No game files are included.
