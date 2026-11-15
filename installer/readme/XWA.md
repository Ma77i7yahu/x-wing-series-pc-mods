# Star Wars: X-Wing Alliance (OpenXWA) – Mod Installer

Installs a modified build of [OpenXWA](https://github.com/elyosh/OpenXWA), the open-source
X-Wing Alliance engine. It adds automatic laser convergence on more craft, removes the
single-player rating locks, and adds a starship point defence option.

Built from OpenXWA v0.0.17 with 4 changes on top.

This is a patch, not a full build: **it only works on an unchanged OpenXWA v0.0.17**, the official
Windows release. Other versions are refused, and nothing is changed.

## Requirements

- Star Wars: X-Wing Alliance (GOG or Steam), with **OpenXWA v0.0.17 installed, set up and running**.
  Download `openxwa-0.0.17-windows-x86_64-release.zip` from
  <https://github.com/elyosh/OpenXWA/releases/tag/v0.0.17> and unzip it into the game folder.
  The installer patches that OpenXWA folder (for example `openxwa` inside the game folder).
- 64-bit Windows 10 or 11 with .NET Framework 4.5 or later (included in Windows 10 and 11).

## How to install

1. Close OpenXWA.
2. Run `XWAModInstaller.exe`. It looks for your game folder. If it doesn't find it, click **Browse...**.
3. Click **Install**.

The installer copies your OpenXWA folder to `<folder>-original` (once). It zips your settings and
pilots (`%APPDATA%\TotallyOpen\OpenXWA`) to `OpenXWA-backup-<date>.zip` next to that folder.
After checking that every file it patches is the v0.0.17 original, it patches `OpenXWA.exe` and
adds this README. **Restore original OpenXWA** puts the backup
back and renames the modded folder to `<folder>-mods-removed`.

## What's included

- **Automatic convergence for manual-convergence craft.** On craft such as the Z-95, **Z** cycled
  through fixed convergence distances only (off / 0.25 / 0.75 / 1.25 km). It now also cycles to
  *Auto*, which follows your current target's range, as craft with automatic convergence already do.
- **No single-player rating locks.** Campaign, Death Star and Junkyard missions capped your rating
  at 7, so only the simulators and skirmishes could go higher. In single player you can now reach
  the top rating anywhere. AI kills also give full rating points at your rank.
- **Starship Point Defence** (General Options: *Original* / *Reduced* / *Off*; default *Reduced*).
  Capital ships, freighters and platforms shoot down warheads less often. With *Reduced*, rockets
  are stopped 75% of the time, advanced torpedoes 15%, space bombs 10% and other warheads 33%.
  Dumb-firing (no lock) still works as before. YT transports keep their full defence.

## Source code

OpenXWA is free software (GPL-3). The `source` folder has these changes as git patches against
the upstream commit they were built on, with instructions in `SOURCE.txt`.

## Credits

OpenXWA is by elyosh and contributors. These changes were made for personal use and are shared as is.
Star Wars: X-Wing Alliance is © Lucasfilm Ltd. No game files are included.
