<#
.SYNOPSIS
    Insère un en-tête de licence SPDX (GPL v3) en tête de tous les fichiers
    source C# (.cs) éligibles du dépôt AudioHexViewer.

.DESCRIPTION
    - Idempotent : un fichier contenant déjà "SPDX-License-Identifier" est ignoré.
    - Préserve le BOM UTF-8 (l'en-tête est inséré APRÈS le BOM, pas avant).
    - Préserve les fins de ligne (lecture/écriture en octets bruts, aucune ré-encode).
    - Exclut : dossiers obj/ et bin/, fichiers générés (BuildInfo.cs, *.g.cs,
      *.AssemblyInfo.cs, *.AssemblyAttributes.cs) et les 3 fichiers morts Histogram.
    - Les .Designer.cs sont INCLUS (choix retenu).

.PARAMETER Root
    Racine du dépôt à parcourir. Par défaut : le dossier courant.

.PARAMETER Author
    Titulaire du copyright. Par défaut : "Frex - OnE Audio Projects".

.PARAMETER Year
    Année du copyright. Par défaut : l'année courante.

.PARAMETER RemoveDeadFiles
    Si présent, supprime aussi les 3 fichiers morts Histogram (sinon ils sont
    seulement signalés). Recommandé : utiliser 'git rm' à la place pour garder l'historique.

.PARAMETER DryRun
    Simulation : affiche ce qui serait fait sans rien modifier.

.EXAMPLE
    # Aperçu sans rien modifier, depuis la racine du dépôt :
    .\Add-LicenseHeaders.ps1 -DryRun

.EXAMPLE
    # Application réelle :
    .\Add-LicenseHeaders.ps1

.NOTES
    À lancer une fois, depuis la racine du dépôt, AVANT le premier commit public.
    Vérifier ensuite le résultat avec :  git diff
#>

[CmdletBinding()]
param(
    [string] $Root   = (Get-Location).Path,
    [string] $Author = "Frex - OnE Audio Projects",
    [int]    $Year   = (Get-Date).Year,
    [switch] $RemoveDeadFiles,
    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

# ── En-tête à insérer (3 lignes + ligne vide), terminé par LF ──────────────────
$headerText =
    "// SPDX-License-Identifier: GPL-3.0-or-later`n" +
    "// Copyright (C) $Year $Author`n" +
    "// Part of AudioHexViewer - see LICENSE for details.`n" +
    "`n"

$bom        = [byte[]](0xEF, 0xBB, 0xBF)
$headerBytes = [System.Text.Encoding]::UTF8.GetBytes($headerText)  # UTF8.GetBytes ne préfixe PAS de BOM

$deadFiles = @("HistogramForm.cs", "HistogramWindow.cs", "HistogramWindow.Designer.cs")

function Test-Eligible {
    param([System.IO.FileInfo] $File)
    # Exclut obj/ et bin/ n'importe où dans le chemin
    $rel = $File.FullName
    if ($rel -match "[\\/](obj|bin)[\\/]") { return $false }
    $name = $File.Name
    if ($deadFiles -contains $name)                { return $false }
    if ($name -eq "BuildInfo.cs")                  { return $false }
    if ($name -like "*.g.cs")                      { return $false }
    if ($name -like "*.AssemblyInfo.cs")           { return $false }
    if ($name -like "*.AssemblyAttributes.cs")     { return $false }
    return $true
}

Write-Host "Racine        : $Root"
Write-Host "Copyright     : (C) $Year $Author"
if ($DryRun) { Write-Host "Mode          : DRY-RUN (aucune écriture)" -ForegroundColor Yellow }
Write-Host ""

# ── Étape 1 : fichiers morts ───────────────────────────────────────────────────
$foundDead = Get-ChildItem -Path $Root -Recurse -File -Filter *.cs |
             Where-Object { $deadFiles -contains $_.Name }
foreach ($d in $foundDead) {
    if ($RemoveDeadFiles) {
        if ($DryRun) { Write-Host "[dead] supprimerait : $($d.FullName)" -ForegroundColor Magenta }
        else { Remove-Item $d.FullName -Force; Write-Host "[dead] supprimé : $($d.FullName)" -ForegroundColor Magenta }
    } else {
        Write-Host "[dead] présent (non supprimé, voir -RemoveDeadFiles ou 'git rm') : $($d.Name)" -ForegroundColor DarkYellow
    }
}
if ($foundDead) { Write-Host "" }

# ── Étape 2 : insertion des en-têtes ───────────────────────────────────────────
$modified = 0; $skipped = 0; $eligible = 0; $bomCount = 0

Get-ChildItem -Path $Root -Recurse -File -Filter *.cs |
    Where-Object { Test-Eligible $_ } |
    Sort-Object FullName |
    ForEach-Object {
        $eligible++
        $bytes = [System.IO.File]::ReadAllBytes($_.FullName)

        # Détection BOM
        $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        $start  = if ($hasBom) { 3 } else { 0 }
        if ($hasBom) { $bomCount++ }

        # Idempotence : cherche le marqueur dans les ~300 premiers octets après BOM
        $probeLen = [Math]::Min(300, $bytes.Length - $start)
        $probe    = [System.Text.Encoding]::UTF8.GetString($bytes, $start, $probeLen)
        if ($probe.Contains("SPDX-License-Identifier")) { $skipped++; return }

        if ($DryRun) {
            Write-Host "[head] ajouterait : $($_.FullName)"
            $modified++; return
        }

        # Reconstruction : [BOM] + header + corps (sans BOM)
        $out = New-Object System.Collections.Generic.List[byte]
        if ($hasBom) { $out.AddRange($bom) }
        $out.AddRange($headerBytes)
        $out.AddRange([byte[]]($bytes[$start..($bytes.Length-1)]))
        [System.IO.File]::WriteAllBytes($_.FullName, $out.ToArray())
        $modified++
    }

Write-Host ""
Write-Host ("Éligibles : {0} | Modifiés : {1} | Ignorés (déjà taggés) : {2} | avec BOM : {3}" -f `
            $eligible, $modified, $skipped, $bomCount) -ForegroundColor Cyan
if ($DryRun) { Write-Host "DRY-RUN : relance sans -DryRun pour appliquer." -ForegroundColor Yellow }
else { Write-Host "Terminé. Vérifie avec : git diff" -ForegroundColor Green }
