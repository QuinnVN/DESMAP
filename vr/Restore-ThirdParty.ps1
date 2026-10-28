[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [string]$Source
)

$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $Source).Path
$assetsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Assets'))
$targetRoot = [IO.Path]::GetFullPath((Join-Path $assetsRoot 'ThirdParty'))
$plan = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'ThirdPartyAssets.json') | ConvertFrom-Json

foreach ($asset in $plan.assets) {
    $oldRelative = $asset.oldPath.Substring('Assets/'.Length)
    $newRelative = $asset.path.Substring('Assets/ThirdParty/'.Length)
    $candidates = @(
        (Join-Path $sourceRoot $asset.name),
        (Join-Path $sourceRoot $oldRelative),
        (Join-Path $sourceRoot $newRelative),
        (Join-Path $sourceRoot (Join-Path 'Assets' $asset.name)),
        (Join-Path $sourceRoot (Join-Path 'Assets\ThirdParty' $asset.name)),
        (Join-Path $sourceRoot (Join-Path 'vr\Assets' $asset.name))
    )
    $folder = $candidates | Where-Object {
        (Test-Path -LiteralPath $_ -PathType Container) -and (Test-Path -LiteralPath ($_ + '.meta'))
    } | Select-Object -First 1
    if (-not $folder) { throw "Missing source folder and .meta for $($asset.name)." }
    $guidLine = Get-Content -LiteralPath ($folder + '.meta') | Where-Object { $_ -match '^guid:' }
    if ($guidLine -ne ('guid: ' + $asset.folderGuid)) {
        throw "Folder GUID differs from the project manifest: $folder"
    }
    $destination = [IO.Path]::GetFullPath((Join-Path $targetRoot $newRelative))
    if (-not $destination.StartsWith($targetRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Destination escapes the third-party asset directory: $destination"
    }
    if (Test-Path -LiteralPath $destination) {
        $existingGuid = Get-Content -LiteralPath ($destination + '.meta') | Where-Object { $_ -match '^guid:' }
        if ($existingGuid -ne $guidLine) { throw "Existing destination has a different GUID: $destination" }
        Write-Host "Already restored: $($asset.name)"
        continue
    }
    if ($PSCmdlet.ShouldProcess($destination, 'Restore asset folder with original metadata')) {
        New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null
        & robocopy $folder $destination /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /XJ /NFL /NDL /NP /NJH /NJS
        if ($LASTEXITCODE -ge 8) { throw "Asset restore failed: $folder" }
        Copy-Item -LiteralPath ($folder + '.meta') -Destination ($destination + '.meta')
    }
}

# Imported editor/demo scripts sometimes contain paths to the original folders.
if (-not $WhatIfPreference -and (Test-Path -LiteralPath $targetRoot)) {
    foreach ($script in Get-ChildItem -LiteralPath $targetRoot -Recurse -Filter '*.cs' -File) {
        $content = [IO.File]::ReadAllText($script.FullName)
        $updated = $content
        foreach ($asset in $plan.assets) {
            $updated = $updated.Replace($asset.oldPath + '/', $asset.path + '/')
        }
        if ($updated -ne $content) { [IO.File]::WriteAllText($script.FullName, $updated) }
    }
}
Write-Host 'Third-party assets are local only; Git ignores Assets/ThirdParty/.'
