# Builds the four mod installers into ..\dist\<Game>\. Each installer is a patch against one
# upstream OpenX release, not a whole build.
# For each game:
#  1. unzips the release zip from ..\upstream (checked against its SHA-256) into obj\<Game>\release
#  2. stages the modded result in obj\<Game>\modded: the release, plus the exe and resources from
#     G:\Dev\<Repo>\build-rel (built by G:\Dev\reldeps\build_rel.sh against the release's own DLLs),
#     the license texts and MODS-README.md. Shaders stay the release's (the mods don't change them).
#  3. makepatch.exe writes manifest + deltas + new files, zipped as payload.zip
#  4. generates GameInfo.cs and compiles Installer.cs + Delta.cs with the zip embedded
# It also writes the source patches (git format-patch from the upstream base) for the GPL.
# obj\<Game>\modded is what test\run.ps1 expects an install to produce.
# Usage: & build.ps1 -Only XWing,TIEFighter   (or no -Only for all four)
param([string[]]$Only)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Split-Path $PSScriptRoot
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bash = 'G:\msys64\usr\bin\bash.exe'

$tools = Join-Path $root 'obj\tools'
New-Item -ItemType Directory -Force $tools | Out-Null
& $csc /nologo /optimize "/out:$tools\makepatch.exe" (Join-Path $PSScriptRoot 'MakePatch.cs') (Join-Path $PSScriptRoot 'Delta.cs')
if ($LASTEXITCODE -ne 0) { throw 'csc failed for makepatch' }

# XvT sources are relative to the X-Wing vs TIE Fighter folder; a "*" source copies every match into the
# destination folder. These match package_xw.sh / package_tie.sh.
$wave = { param($dir, $names) $names | % { ,@("wave\$_", "resources\$dir\$_") } }
$washSounds = & $wave 'xvt_engine_wash' @('sd-lp-1.wav', 'cal-flyb.wav')
$controlSounds = & $wave 'xvt_control_sounds' @('BEEP-01.WAV', 'SMCLICK1.WAV', 'SET_OFF2.WAV', 'SET_VLO3.WAV', 'SET_LO2.WAV', 'SET_MED2.WAV', 'SET_HI2.WAV')

$games = @(
    @{ Key = 'XWing'; Title = 'Star Wars: X-Wing'; Port = 'OpenXW'; Repo = 'OpenXW'; AppData = 'OpenXW'; Tag = 'v0.0.3'; ReleaseName = 'OpenXW v0.0.3'
       Zip = 'openxw-0.0.3-windows-x86_64-release.zip'; ZipSha = '14f70cfc6611d037dd77db4383f11daad7daa1e2a904c6251c0a58e7d222955d'
       Url = 'https://github.com/elyosh/OpenXW'
       XvtNote = 'The XvT cockpits, threat display (Z), XvT laser/ion sounds, engine wash sound and control sounds use art and sounds from Star Wars: X-Wing vs TIE Fighter (with Balance of Power), which can''t be shipped here. If you own it, the installer copies them from your XvT folder.'
       XvtHint = 'the cockpits need the BalanceOfPower folder, which the GOG and Steam releases include'
       After = 'The new options are on the Video, Audio and Game pages of OpenXW''s settings. S-foils moved to V; see the README for the new keys.'
       Xvt = @(& $wave 'xvt_weapon_sounds' @('REB-LS-2.WAV', 'REB-LS-4.WAV', 'TL-SHT-1.WAV', 'TL-BIG-B.WAV', 'W-LASR-3.WAV', 'CANNON-1.WAV')) + $washSounds + $controlSounds + @(
           ,@('cp640\TIEFTRTD.LFD', 'resources\xvt_threat_display\TIEFTRTD.LFD')
           ,@('BalanceOfPower\CP640\xwing.int', 'resources\xvt_cockpit\xwing.int')
           ,@('BalanceOfPower\CP640\ywing.int', 'resources\xvt_cockpit\ywing.int')
           ,@('BalanceOfPower\CP640\awing.int', 'resources\xvt_cockpit\awing.int')
           ,@('BalanceOfPower\CP640\bwing.int', 'resources\xvt_cockpit\bwing.int')
           ,@('BalanceOfPower\CP640\*.lfd', 'resources\xvt_cockpit')
           ,@('BalanceOfPower\CP640\*.pnl', 'resources\xvt_cockpit')
           ,@('BalanceOfPower\micro64.fnt', 'resources\xvt_cockpit\MICRO64.FNT')) }
    @{ Key = 'TIEFighter'; Title = 'Star Wars: TIE Fighter'; Port = 'OpenTIE'; Repo = 'OpenTIE'; AppData = 'OpenTIE'; Tag = 'v0.0.7'; ReleaseName = 'OpenTIE v0.0.7'
       Zip = 'opentie-0.0.7-windows-x86_64-release.zip'; ZipSha = '1943eeb5192b70d032c1298386862ad159f2e2643071fb4b998a72bd93e1138f'
       Url = 'https://github.com/elyosh/OpenTIE'
       XvtNote = 'The XvT message font, engine wash sounds and XvT control sounds come from Star Wars: X-Wing vs TIE Fighter, which can''t be shipped here. If you own it, the installer copies them from your XvT folder; without them TIE Fighter''s own font and sounds are used.'
       XvtHint = 'they are MICRO64.FNT and files in the "wave" folder'
       After = 'The new options are on the Video, Audio and Game pages of OpenTIE''s settings. Component tracking moved to Alt+I; see the README for the new keys.'
       Xvt = @(,@('MICRO64.FNT', 'resources\xvt_fonts\MICRO64.FNT')) + $washSounds + $controlSounds }
    @{ Key = 'XvT'; Title = 'Star Wars: X-Wing vs TIE Fighter'; Port = 'OpenXvT'; Repo = 'OpenXvT'; AppData = 'OpenXvT'; Tag = 'v0.0.4'; ReleaseName = 'OpenXvT v0.0.4'
       Zip = 'openxvt-0.0.4-windows-x86_64-release.zip'; ZipSha = 'afc9514435d42e40d19c3e0e66c5ad72fc558b531e51c1ae2886755cda4eef8d'
       Url = 'https://github.com/elyosh/OpenXvT'; XvtNote = ''; XvtHint = ''
       After = 'The new options are on the Video and Game pages of OpenXvT''s settings.'
       Xvt = @() }
    @{ Key = 'XWA'; Title = 'Star Wars: X-Wing Alliance'; Port = 'OpenXWA'; Repo = 'OpenXWA'; AppData = 'OpenXWA'; Tag = 'v0.0.17'; ReleaseName = 'OpenXWA v0.0.17'
       Zip = 'openxwa-0.0.17-windows-x86_64-release.zip'; ZipSha = 'b53340c06f66ba1d96e697f070c3c461ff8022d706812abdf34c68c979b6c1d7'
       Url = 'https://github.com/elyosh/OpenXWA'; XvtNote = ''; XvtHint = ''
       After = 'Starship Point Defence is on the General Options page.'
       Xvt = @() }
)

function CsStr([string]$s) { '"' + $s.Replace('\', '\\').Replace('"', '\"') + '"' }

function Git([string]$cmd) {
    $env:MSYSTEM = 'MINGW64'
    $out = & $bash -lc $cmd
    if ($LASTEXITCODE -ne 0) { throw "git failed: $cmd" }
    $out
}

foreach ($g in $games) {
    if ($Only -and $Only -notcontains $g.Key) { continue }
    $k = $g.Key
    Write-Host "=== $k"
    $obj = Join-Path $root "obj\$k"
    $dist = Join-Path $root "dist\$k"
    if (Test-Path $obj) { Remove-Item $obj -Recurse -Force }
    New-Item -ItemType Directory -Force $obj, $dist | Out-Null

    # build id from the repo the test build came from
    $repo = "/g/Dev/$($g.Repo)"
    $head = (Git "cd $repo && git rev-parse --short HEAD") | Select-Object -Last 1
    $base = (Git "cd $repo && git merge-base HEAD origin/main") | Select-Object -Last 1
    $tag = (Git "cd $repo && git describe --tags --abbrev=0 $base") | Select-Object -Last 1
    $count = (Git "cd $repo && git rev-list --count $base..HEAD") | Select-Object -Last 1
    $buildId = "$($g.Port) $tag + $count changes, $head"
    if ($tag -ne $g.Tag) { throw "$k is based on $tag, but the installer targets $($g.Tag)" }

    # 1. the upstream release
    $zipPath = Join-Path $root "upstream\$($g.Zip)"
    if ((Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLower() -ne $g.ZipSha) { throw "$zipPath doesn't match its SHA-256" }
    $relDir = Join-Path $obj 'release'
    [IO.Compression.ZipFile]::ExtractToDirectory($zipPath, $relDir)
    $relDir = Join-Path $relDir ([IO.Path]::GetFileNameWithoutExtension($g.Zip))

    # 2. the modded result: release + rebuilt exe and resources + licenses + MODS-README.md
    $modded = Join-Path $obj 'modded'
    robocopy $relDir $modded /E /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $k" }
    $build = "G:\Dev\$($g.Repo)\build-rel"
    $exeSrc = "G:\Dev\$($g.Repo)-rel\$($g.Port).exe"
    if ((Get-Item $exeSrc).LastWriteTime -lt (Get-Item "$build\$($g.Port).exe").LastWriteTime) { throw "$exeSrc is older than the build; rerun build_rel.sh" }
    Copy-Item $exeSrc $modded -Force
    robocopy "$build\resources" "$modded\resources" /E /NFL /NDL /NJH /NJS /NP /XD xvt_* | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $k resources" }
    Copy-Item (Join-Path $root "licenses\$k\*") (Join-Path $modded 'licenses') -Recurse -Force
    if (Test-Path (Join-Path $root "licenses\$k-share")) { Copy-Item (Join-Path $root "licenses\$k-share\*") (Join-Path $modded 'share') -Recurse -Force }
    Copy-Item (Join-Path $root "readme\$k.md") (Join-Path $modded 'MODS-README.md')
    $bad = @(Get-ChildItem $modded -Recurse -File -Include *.wav, *.lfd, *.pnl, *.int) + @(Get-ChildItem (Join-Path $modded 'resources') -Directory -Filter 'xvt_*')
    if ($bad) { throw "XvT files in the $k build: $($bad.FullName -join ', ')" }

    # 3. the patch
    $patch = Join-Path $obj 'patch'
    & (Join-Path $root 'obj\tools\makepatch.exe') $relDir $modded $patch $g.ReleaseName | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "makepatch failed for $k" }
    # entry names with "/" (CreateFromDirectory on .NET Framework writes "\")
    $zip = Join-Path $obj 'payload.zip'
    $za = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Get-ChildItem $patch -Recurse -File | % {
            $name = $_.FullName.Substring($patch.Length + 1).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, $_.FullName, $name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $za.Dispose() }

    # GameInfo.cs
    $files = ($g.Xvt | % { "        new[] { $(CsStr $_[0]), $(CsStr $_[1]) }," }) -join "`r`n"
    $info = @"
// Generated by build.ps1 for $k.
static class GameInfo {
    public const string Title = $(CsStr $g.Title);
    public const string PortName = $(CsStr $g.Port);
    public const string PortExe = $(CsStr ($g.Port + '.exe'));
    public const string PortUrl = $(CsStr $g.Url);
    public const string AppDataName = $(CsStr $g.AppData);
    public const string BuildId = $(CsStr $buildId);
    public const string ReleaseName = $(CsStr $g.ReleaseName);
    public const string ReleaseUrl = $(CsStr "$($g.Url)/releases/tag/$($g.Tag)");
    public const string XvtNote = $(CsStr $g.XvtNote);
    public const string XvtMissingHint = $(CsStr $g.XvtHint);
    public const string AfterInstall = $(CsStr $g.After);
    public static readonly string[][] XvtFiles = {
$files
    };
}
"@
    Set-Content (Join-Path $obj 'GameInfo.cs') $info -Encoding UTF8

    $exe = Join-Path $dist "$($k)ModInstaller.exe"
    & $csc /nologo /target:winexe /optimize "/out:$exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll `
        /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "/resource:$zip,payload.zip" `
        (Join-Path $PSScriptRoot 'Installer.cs') (Join-Path $PSScriptRoot 'Delta.cs') (Join-Path $obj 'GameInfo.cs')
    if ($LASTEXITCODE -ne 0) { throw "csc failed for $k" }

    Copy-Item (Join-Path $root "readme\$k.md") (Join-Path $dist 'README.md')

    # GPL: our changes as patches against the upstream commit, plus the aeron submodule's if it changed
    $src = Join-Path $dist 'source'
    if (Test-Path $src) { Remove-Item $src -Recurse -Force }
    New-Item -ItemType Directory $src | Out-Null
    $srcU = '/' + $src.Replace(':', '').Replace('\', '/').Substring(0, 1).ToLower() + $src.Replace('\', '/').Substring(2)
    Git "cd $repo && git format-patch -q --no-signature -o '$srcU/$($g.Repo)' $base..HEAD" | Out-Null
    $subBase = (Git "cd $repo && git ls-tree $base aeron | awk '{print `$3}'") | Select-Object -Last 1
    $subHead = (Git "cd $repo && git ls-tree HEAD aeron | awk '{print `$3}'") | Select-Object -Last 1
    $aeron = ''
    if ($subBase -ne $subHead) {
        Git "cd $repo/aeron && git format-patch -q --no-signature -o '$srcU/aeron' $subBase..$subHead" | Out-Null
        $aeron = "`r`nThe aeron submodule also changed: apply the patches in aeron\ inside aeron/ (upstream submodule commit $subBase).`r`n"
    }
    $fullBase = (Git "cd $repo && git rev-parse $base") | Select-Object -Last 1
    $buildDir = Join-Path $src 'build'
    New-Item -ItemType Directory $buildDir | Out-Null
    foreach ($f in 'build_rel.sh', 'build_ffmpeg.sh', 'mkimplib.sh', 'checkimports.sh', 'pc\speexdsp.pc') { Copy-Item "G:\Dev\reldeps\$f" $buildDir }
    @"
Source code for the modified $($g.Port) in $($k)ModInstaller.exe ($buildId).

$($g.Port) is free software ($($g.Url)); these changes are released under the same terms
(GNU GPL version 3, see licenses\ in the installed folder).

To rebuild: clone $($g.Url), check out commit $fullBase
(with its submodules), then apply the patches in $($g.Repo)\ in order:
    git am $($g.Repo)/*.patch
$aeron
The installer patches the official $($g.ReleaseName) Windows release
($($g.Url)/releases/tag/$($g.Tag), $($g.Zip),
SHA-256 $($g.ZipSha)), so the exe is built to run on that release's own DLLs. It was built
with MSYS2 (MINGW64, GCC 16) using the scripts in build\:
    build_ffmpeg.sh   FFmpeg 8.0.1 headers and import libraries, configured as upstream's
                      packaging/windows/Dockerfile (the DLLs themselves come from the release)
    mkimplib.sh       import libraries for the release's libspeexdsp.dll (and libcurl.dll)
    speexdsp.pc       pkg-config file pointing SpeexDSP at that import library
    build_rel.sh      configures and builds with libstdc++/libgcc/winpthread linked statically, strips
                      the exe and runs checkimports.sh against the release folder
Shaders and every other file are the release's own. The X-Wing vs TIE Fighter files the
installer copies come from your own XvT install and are not part of the source.
"@ | Set-Content (Join-Path $src 'SOURCE.txt') -Encoding UTF8

    $mb = (Get-Item $exe).Length / 1MB
    Write-Host ("{0}: {1} ({2:N1} MB), {3} XvT entries" -f $k, $buildId, $mb, $g.Xvt.Count)
}
