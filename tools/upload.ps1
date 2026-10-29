# Uploads dist\<mod id>\ to the Steam Workshop with SteamCMD (run tools\build.ps1 first).
#
#   tools\upload.ps1 -User <steam account> [-ChangeNote "..."] [-SteamCmd C:\steamcmd\steamcmd.exe]
#
# SteamCMD asks for the password and Steam Guard code itself. A new item is created private; set its
# visibility on the Workshop page once it checks out. Later uploads keep the visibility set there.
# The item ID is kept in workshop\publishedfileid.txt; commit it after the first upload.
param(
    [Parameter(Mandatory = $true)][string]$User,
    [string]$ChangeNote,
    [string]$SteamCmd = "C:\steamcmd\steamcmd.exe"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$manifest = ([xml](Get-Content "$root\mod\manifest.xml" -Encoding UTF8)).Manifest
$content = Join-Path $root "dist\$($manifest.ID)"
$idFile = Join-Path $root "workshop\publishedfileid.txt"

if (-not (Test-Path $SteamCmd)) {
    throw "找不到 SteamCMD：$SteamCmd。請從 https://developer.valvesoftware.com/wiki/SteamCMD 下載後解壓縮，或用 -SteamCmd 指定位置。"
}
if (-not (Test-Path "$content\manifest.xml")) { throw "找不到 $content，請先執行 tools\build.ps1" }

$id = if (Test-Path $idFile) { (Get-Content $idFile).Trim() } else { "0" }
if (-not $ChangeNote) { $ChangeNote = "v$($manifest.ModVersion)" }

function Quote([string]$s) { '"' + $s.Replace('\', '\\').Replace('"', '\"') + '"' }
$utf8 = New-Object System.Text.UTF8Encoding($false)
$fields = [ordered]@{
    appid           = "4348910"
    publishedfileid = $id
    contentfolder   = $content
    previewfile     = Join-Path $root "workshop\preview.png"
    title           = (Get-Content "$root\workshop\title.txt" -Encoding UTF8 -Raw).Trim()
    description     = (Get-Content "$root\workshop\description.txt" -Encoding UTF8 -Raw).Trim()
    changenote      = $ChangeNote
}
if ($id -eq "0") { $fields.visibility = "2" }   # private until checked
$vdf = "`"workshopitem`"`n{`n" + (($fields.GetEnumerator() | ForEach-Object { "`t`"$($_.Key)`" $(Quote $_.Value)" }) -join "`n") + "`n}`n"
$vdfPath = Join-Path $root "build\workshop_item.vdf"
New-Item -ItemType Directory -Force (Split-Path $vdfPath) | Out-Null
[IO.File]::WriteAllText($vdfPath, $vdf, $utf8)

& $SteamCmd +login $User +workshop_build_item $vdfPath +quit
if ($LASTEXITCODE -ne 0) { throw "SteamCMD failed ($LASTEXITCODE)" }

# SteamCMD writes the new item's ID back into the VDF.
$newId = [regex]::Match([IO.File]::ReadAllText($vdfPath, $utf8), '"publishedfileid"\s+"(\d+)"').Groups[1].Value
if ($newId -and $newId -ne "0" -and $newId -ne $id) {
    [IO.File]::WriteAllText($idFile, "$newId`n", $utf8)
    Write-Host "New Workshop item $newId saved to workshop\publishedfileid.txt"
}
Write-Host "https://steamcommunity.com/sharedfiles/filedetails/?id=$(if ($newId) { $newId } else { $id })"
