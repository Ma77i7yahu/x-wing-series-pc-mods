# Star Wars X-Wing series (PC) – OpenX engine mods

Modified builds of the open-source **OpenXW**, **OpenTIE**, **OpenXvT** and **OpenXWA** engines (by elyosh and contributors) for **X-Wing**, **TIE Fighter**, **X-Wing vs TIE Fighter** and **X-Wing Alliance**. They add widescreen, X-Wing vs TIE Fighter-style cockpits, HUD and gameplay, X-Wing Alliance laser convergence and twist roll, and many fixes. Almost every change has its own switch in the port's settings, so you can keep only what you like.

**Each installer is a patch, not a full build.** It patches one official OpenX release and refuses anything else, so install that release first:

| Game | Needs | Download | Changes |
|---|---|---|---|
| X-Wing | [OpenXW v0.0.3](https://github.com/elyosh/OpenXW/releases/tag/v0.0.3) | `XWingMods-v1.0.zip` | 59 ([list](installer/readme/XWing.md)) |
| TIE Fighter | [OpenTIE v0.0.7](https://github.com/elyosh/OpenTIE/releases/tag/v0.0.7) | `TIEFighterMods-v1.0.zip` | 18 ([list](installer/readme/TIEFighter.md)) |
| X-Wing vs TIE Fighter | [OpenXvT v0.0.4](https://github.com/elyosh/OpenXvT/releases/tag/v0.0.4) | `XvTMods-v1.0.zip` | 25 ([list](installer/readme/XvT.md)) |
| X-Wing Alliance | [OpenXWA v0.0.17](https://github.com/elyosh/OpenXWA/releases/tag/v0.0.17) | `XWAMods-v1.0.zip` | 4 ([list](installer/readme/XWA.md)) |

Get the zips from the [**Releases**](https://github.com/Ma77i7yahu/x-wing-series-pc-mods/releases/latest) page (`SHA256SUMS.txt` there lets you check them). Each holds the installer, its README and the source patches. The installer copies your OpenX folder to `<folder>-original` and backs up your settings and pilots first, and can restore the original.

Some X-Wing and TIE Fighter features use art and sounds from **X-Wing vs TIE Fighter**. Those can't be shipped, so the installer copies them from your own XvT install if you have one; without it those features stay off.

## Status

The changes were played in my own builds of each port. The installers contain the **same source rebuilt to run on the official releases' DLLs**, and those rebuilt exes haven't been flown yet. Every installer passes its test harness: install on a clean release equals the build file by file, reinstall changes nothing, restore brings the release back, and a changed or older folder is refused with nothing written.

## What's in this repo

- `patches/<Game>/`: the changes as git patches against the upstream commit they were built on, with the build scripts and `SOURCE.txt` (how to rebuild).
- `installer/`: the patch installer (`src/Installer.cs`, a bsdiff port in `src/Delta.cs`, `src/MakePatch.cs`, `src/build.ps1`), the per-game READMEs, licence texts and the test harness.

## Licence

OpenTIE, OpenXvT and OpenXWA are GPL-3, and these changes are released under the same terms ([LICENSE](LICENSE)). OpenXW has no LICENSE file of its own; it links the GPL-3 Aeron and Landru libraries, so its changes are offered under GPL-3 too. The installed folders get the full licence texts of every library.

## Disclaimer

Unofficial fan-made mods, not affiliated with or endorsed by Lucasfilm, LucasArts, Disney, Totally Games or the OpenX authors. Star Wars is a trademark of its owners. No game files are distributed here.
