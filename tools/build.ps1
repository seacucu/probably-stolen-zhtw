# Builds the mod and assembles the folder the game loads (and the Workshop item uploads):
#   dist\seacucu.zh-tw\  manifest.xml, ProbablyStolenZhTW.dll, data\, fonts\, LICENSE.txt
# -Install also copies it to <game>\Mods\ for local testing.
param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Probably Stolen",
    [switch]$Install
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$modId = ([xml](Get-Content "$root\mod\manifest.xml" -Encoding UTF8)).Manifest.ID
$out = Join-Path $root "dist\$modId"

dotnet build "$root\src\ProbablyStolenZhTW.csproj" -c Release -o "$root\build" -p:GameDir="$GameDir" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

if (Test-Path $out) { Remove-Item -Recurse -Force $out }
New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item "$root\mod\*" $out -Recurse
Copy-Item "$root\build\ProbablyStolenZhTW.dll" $out
Copy-Item "$root\LICENSE" "$out\LICENSE.txt"
Write-Host "Built $out"

if ($Install) {
    $target = Join-Path $GameDir "Mods\$modId"
    if (Test-Path $target) { Remove-Item -Recurse -Force $target }
    Copy-Item $out $target -Recurse
    Write-Host "Installed to $target"
}
