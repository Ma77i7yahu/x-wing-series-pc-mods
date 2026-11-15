# Star Wars: X-Wing vs TIE Fighter (OpenXvT) – Mod Installer

Installs a modified build of [OpenXvT](https://github.com/elyosh/OpenXvT), the open-source
X-Wing vs TIE Fighter engine. It adds a widescreen cockpit and HUD, X-Wing Alliance-style
gunnery, single-player AI and rating fixes, and crash logs.

Built from OpenXvT v0.0.4 (plus one later upstream fix) with 25 changes on top.

This is a patch, not a full build: **it only works on an unchanged OpenXvT v0.0.4**, the official
Windows release. Older and newer versions are refused, and nothing is changed.

## Requirements

- Star Wars: X-Wing vs TIE Fighter (GOG or Steam), with **OpenXvT v0.0.4 installed, set up and running**.
  Download `openxvt-0.0.4-windows-x86_64-release.zip` from
  <https://github.com/elyosh/OpenXvT/releases/tag/v0.0.4> and unzip it into the game folder.
  The installer patches that OpenXvT folder (for example `Openxvt` inside the game folder).
- 64-bit Windows 10 or 11 with .NET Framework 4.5 or later (included in Windows 10 and 11).

## How to install

1. Close OpenXvT.
2. Run `XvTModInstaller.exe`. It looks for your game folder. If it doesn't find it, click **Browse...**.
3. Click **Install**.

The installer copies your OpenXvT folder to `<folder>-original` (once). It zips your settings and
pilots (`%APPDATA%\TotallyOpen\OpenXvT`) to `OpenXvT-backup-<date>.zip` next to that folder.
After checking that every file it patches is the v0.0.4 original, it patches `OpenXvT.exe` and
`resources\config.yaml` and adds this README. **Restore original OpenXvT** puts the backup
back and renames the modded folder to `<folder>-mods-removed`.

## What's included

### Video settings
| Setting | What it does |
|---|---|
| **Widescreen Cockpit-Off HUD** | With the cockpit off, the HUD spreads to the screen edges. A compact top strip shows the laser charge, countermeasures and clock. Shields use the cockpit's arcs and hull icon, and mid-screen messages stack above the console. |
| **Stretch Cockpit to Widescreen** | The 4:3 cockpit art (and its target display) fills a widescreen window. The 3D view isn't stretched. |

### Game settings
| Setting | What it does |
|---|---|
| **Hard Difficulty** | *Balanced*: in single player on Hard, your wingmen keep their mission skill. The stock game lowers them, which is meant to balance multiplayer. Enemies stay tougher. *Classic* is the original. |
| **AI Laser & Ion Linking** | AI craft fire lasers and ion cannons together. Choose Off, Friendly Only or All Craft. Disable orders still use ions only. |
| **Capital Ship Missiles** | *After 2 Seconds*: capital ships fire missiles at you only after you've kept them targeted for 2 seconds, so scrolling past them with T no longer sets them off. |
| **Twist Roll** | *X-Wing Alliance*: XWA's 8% twist dead zone on the roll axis (the roll rates are already the same as XWA's). |

### Always on
- **Laser convergence** (X-Wing Alliance style). Your cannons converge on the target ahead when it's within about 30° and 0.1–2.5 km, otherwise at 550 m. With no target they fire parallel. To change the distance, or set it to 0 for off, edit `flight: laser_convergence:` in `%APPDATA%\TotallyOpen\OpenXvT\config.yaml`.
- **Lasers and ion cannons fire linked**: press X again after "fire linked".
- **Single-player ratings**: AI kills give full rating points at every rank, so every rating can be reached in single player. In the stock game, progress stalls at Top Ace.
- **Turret gunners** use the Hard skill on every difficulty, so they lead maneuvering fighters less perfectly.
- **Crash logs**: each session writes a log to `%APPDATA%\TotallyOpen\OpenXvT\logs` (the last 10 are kept), with a stack trace if the game crashes.

## Source code

OpenXvT is free software (GPL-3). The `source` folder has these changes as git patches against
the upstream commit they were built on, with instructions in `SOURCE.txt`.

## Credits

OpenXvT is by elyosh and contributors. These changes were made for personal use and are shared as is.
Star Wars: X-Wing vs TIE Fighter is © Lucasfilm Ltd. No game files are included.
