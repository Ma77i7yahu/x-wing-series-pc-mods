# Round trips for each patch installer, on scratch copies only (never the real game folders).
#  1. install on a clean copy of the upstream release: every file must equal obj\<Game>\modded
#     (the release + rebuilt exe + resources that build.ps1 staged), a user file must survive,
#     the backup must equal the release, and the XvT files must be copied when an XvT folder is given
#  2. reinstall: no second backup, nothing changes
#  3. restore: the release comes back, the modded folder is kept aside
#  4. refusals: a release copy with one changed patched file, and the old full-build mod
#     (G:\Dev\Open*-test), must both be refused with nothing written
param([string[]]$Only)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$xvt = 'F:\Game Files\Steam\steamapps\common\STAR WARS X-Wing vs TIE Fighter'
$cases = @(
    @{ Key = 'XWing'; Port = 'OpenXW'; Folder = 'Openxw'; Xvt = $xvt }
    @{ Key = 'TIEFighter'; Port = 'OpenTIE'; Folder = 'Opentie'; Xvt = $xvt }
    @{ Key = 'XvT'; Port = 'OpenXvT'; Folder = 'Openxvt'; Xvt = '-' }
    @{ Key = 'XWA'; Port = 'OpenXWA'; Folder = 'openxwa'; Xvt = '-' }
)

function Hashes($dir) {
    $h = @{}
    Get-ChildItem $dir -Recurse -File | % { $h[$_.FullName.Substring($dir.Length + 1)] = (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
    $h
}
function Same($a, $b, $label) {
    $bad = @($a.Keys | ? { $b[$_] -ne $a[$_] }) + @($b.Keys | ? { -not $a.ContainsKey($_) })
    if ($bad.Count) { Write-Host "FAIL $label : $($bad.Count) differ, e.g. $($bad | select -First 5)"; $false } else { Write-Host "ok   $label"; $true }
}
function Harness($exe, $dir, $x, $mode) {
    $out = & "$root\test\harness.exe" $exe $dir $x $mode
    $out | % { "     $_" } | Out-Host
    $out -join "`n"
}
function Flat($h) { @($h.GetEnumerator() | % { "$($_.Key)=$($_.Value)" } | Sort-Object) -join "`n" }

$all = $true
foreach ($c in $cases) {
    if ($Only -and $Only -notcontains $c.Key) { continue }
    Write-Host "===== $($c.Key)"
    $obj = Join-Path $root "obj\$($c.Key)"
    $release = Get-ChildItem (Join-Path $obj 'release') -Directory | Select-Object -First 1 -ExpandProperty FullName
    $modded = Join-Path $obj 'modded'
    $exe = Join-Path $root "dist\$($c.Key)\$($c.Key)ModInstaller.exe"
    $scratch = Join-Path $root "test\$($c.Key)"
    if (Test-Path $scratch) { Remove-Item $scratch -Recurse -Force }
    $game = Join-Path $scratch 'game'
    $port = Join-Path $game $c.Folder
    robocopy $release $port /E /NFL /NDL /NJH /NJS /NP | Out-Null
    Set-Content (Join-Path $port 'my-soundfont.sf2') 'user file'
    $orig = Hashes $port

    # 1. install
    $null = Harness $exe $game $c.Xvt install
    $want = Hashes $modded
    $now = Hashes $port
    $sub = @{}; foreach ($k in $want.Keys) { $sub[$k] = $now[$k] }
    $ok = Same $want $sub 'patched release equals the modded build, file by file'
    $extra = @($now.Keys | ? { -not $want.ContainsKey($_) -and $_ -ne 'my-soundfont.sf2' -and $_ -ne 'mod-install.txt' -and $_ -notlike 'resources\xvt_*' })
    if ($extra.Count) { Write-Host "FAIL unexpected files: $($extra | select -First 5)"; $ok = $false } else { Write-Host 'ok   no unexpected files' }
    if (-not (Test-Path (Join-Path $port 'my-soundfont.sf2'))) { Write-Host 'FAIL user file lost'; $ok = $false }
    $ok = (Same $orig (Hashes "$port-original") 'backup equals the release') -and $ok
    if ($c.Xvt -ne '-') {
        $n = @(Get-ChildItem (Join-Path $port 'resources') -Recurse -File | ? { $_.FullName -match '\\resources\\xvt_' }).Count
        if ($n -gt 0) { Write-Host "ok   $n XvT files copied from the player's XvT" } else { Write-Host 'FAIL no XvT files copied'; $ok = $false }
    }
    Get-Content (Join-Path $port 'mod-install.txt') | % { "     $_" }

    # 2. reinstall
    $before = Hashes $port
    $null = Harness $exe $port $c.Xvt install
    if (Test-Path "$port-original-2") { Write-Host 'FAIL reinstall made a second backup'; $ok = $false } else { Write-Host 'ok   reinstall kept one backup' }
    $after = Hashes $port; $after.Remove('mod-install.txt'); $before.Remove('mod-install.txt')
    $ok = (Same $before $after 'reinstall changed nothing') -and $ok

    # 3. restore
    $null = Harness $exe $game $c.Xvt restore
    $ok = (Same $orig (Hashes $port) 'restore brings the release back') -and $ok
    if (-not (Test-Path "$port-mods-removed\mod-install.txt")) { Write-Host 'FAIL modded folder not kept aside'; $ok = $false }

    # 4. refusals
    foreach ($bad in @('tampered', 'oldmod')) {
        $g2 = Join-Path $scratch "refuse-$bad"
        $p2 = Join-Path $g2 $c.Folder
        if ($bad -eq 'tampered') {
            robocopy $release $p2 /E /NFL /NDL /NJH /NJS /NP | Out-Null
            Add-Content (Join-Path $p2 "$($c.Port).exe") 'x'
        } else {
            robocopy "G:\Dev\$($c.Port)-test" $p2 /E /NFL /NDL /NJH /NJS /NP /XD xvt_* | Out-Null
        }
        $h0 = Flat (Hashes $p2)
        $text = Harness $exe $g2 '-' install
        $refused = $text -match "isn't an unchanged"
        $untouched = (-not (Test-Path "$p2-original")) -and ((Flat (Hashes $p2)) -eq $h0)
        if ($refused -and $untouched) { Write-Host "ok   $bad folder refused, nothing written" } else { Write-Host "FAIL $bad folder: refused=$refused untouched=$untouched"; $ok = $false }
    }
    $mb = (Get-Item $exe).Length / 1MB
    Write-Host ("RESULT {0}: {1} (installer {2:N1} MB)" -f $c.Key, $(if ($ok) { 'PASS' } else { 'FAIL' }), $mb)
    $all = $all -and $ok
}
Write-Host $(if ($all) { 'ALL PASS' } else { 'SOME FAILED' })
