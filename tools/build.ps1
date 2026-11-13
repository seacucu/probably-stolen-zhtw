# Builds the mod and assembles the folder the game loads (and the Workshop item uploads):
#   dist\seacucu.zh-tw\  manifest.xml, ProbablyStolenZhTW.dll, data\, fonts\, LICENSE.txt
#   dist\seacucu.zh-tw.zip  the same folder, attached to GitHub releases
#   dist\release-notes.md   this version's changelog, the release notes (empty when it has none)
# The folder is scanned with the installed game's own mod scanner (tools\ModScan) before it is packed.
# -Install also copies it to <game>\Mods\ for local testing.
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen",
    [switch]$Install
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$manifest = ([xml](Get-Content "$root\mod\manifest.xml" -Encoding UTF8)).Manifest
$modId = $manifest.ID
$modVersion = $manifest.ModVersion
$out = Join-Path $root "dist\$modId"

# Version history (mod\data\changelog.txt) -> the Workshop description's version history section and
# dist\release-notes.md for the current version. Versions without items are left out.
$utf8 = New-Object System.Text.UTF8Encoding $false
$versions = @()
foreach ($line in [IO.File]::ReadAllLines("$root\mod\data\changelog.txt", $utf8)) {
    if ($line -eq "" -or $line.StartsWith("#")) { continue }
    if ($line.StartsWith("- ")) { $versions[-1].Items += $line.Substring(2) }
    else { $versions += [pscustomobject]@{ Heading = $line; Items = @() } }
}
$listed = @($versions | Where-Object { $_.Items.Count -gt 0 })
$bb = ($listed | ForEach-Object {
    "[*][b]v$($_.Heading)[/b]`n[list]`n" + (($_.Items | ForEach-Object { "[*]$_" }) -join "`n") + "`n[/list]"
}) -join "`n"
$descPath = "$root\workshop\description.txt"
$desc = [IO.File]::ReadAllText($descPath, $utf8)
# The section heading, spelled by code point: Windows PowerShell reads this script as ANSI.
$heading = -join [char[]](0x66F4, 0x65B0, 0x7D00, 0x9304)
$section = [regex]('(?s)(\[h2\]' + $heading + '\[/h2\]\r?\n\[list\]\r?\n).*?(\r?\n\[/list\]\r?\n\r?\n\[h2\])')
if (-not $section.IsMatch($desc)) { throw "workshop\description.txt has no version history section" }
[IO.File]::WriteAllText($descPath, $section.Replace($desc, { param($m) $m.Groups[1].Value + $bb + $m.Groups[2].Value }, 1), $utf8)
New-Item -ItemType Directory -Force "$root\dist" | Out-Null
$current = $versions | Where-Object { $_.Heading.Split(" ")[0] -eq $modVersion } | Select-Object -First 1
if (-not $current) { throw "mod\data\changelog.txt has no entry for $modVersion" }
[IO.File]::WriteAllText("$root\dist\release-notes.md", (($current.Items | ForEach-Object { "- $_" }) -join "`n") + "`n", $utf8)

dotnet build "$root\src\ProbablyStolenZhTW.csproj" -c Release -o "$root\build" -p:GameDir="$GameDir" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item "$root\mod\*" $out -Recurse
Copy-Item "$root\build\ProbablyStolenZhTW.dll" $out
Copy-Item "$root\LICENSE" "$out\LICENSE.txt"
Write-Host "Built $out"

# The game scans a mod before activating it; stop here rather than pack a build it would refuse or prompt for.
dotnet build "$root\tools\ModScan\ModScan.csproj" -c Release -o "$root\build\ModScan" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build of tools\ModScan failed" }
& "$root\build\ModScan\ModScan.exe" $GameDir $out
if ($LASTEXITCODE -ne 0) { throw "the game's mod scanner would refuse this build or ask players to approve it" }

# Release asset; entry names use "/" so the zip also unpacks on Linux (the Workshop upload runs there).
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$zipPath = "$out.zip"
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
$zip = [IO.Compression.ZipFile]::Open($zipPath, "Create")
try {
    Get-ChildItem $out -Recurse -File | ForEach-Object {
        $name = "$modId/" + $_.FullName.Substring($out.Length + 1).Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $name) | Out-Null
    }
} finally { $zip.Dispose() }
Write-Host "Packed $zipPath"

if ($Install) {
    $target = Join-Path $GameDir "Mods\$modId"
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    Copy-Item $out $target -Recurse
    Write-Host "Installed to $target"
}
