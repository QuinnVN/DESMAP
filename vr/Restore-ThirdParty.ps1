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

$metadataByGuid = $null
$restoreEntries = foreach ($asset in $plan.assets) {
    $isFile = $asset.kind -eq 'file'
    $sourceType = if ($isFile) { 'Leaf' } else { 'Container' }
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
        (Test-Path -LiteralPath $_ -PathType $sourceType) -and (Test-Path -LiteralPath ($_ + '.meta'))
    } | Select-Object -First 1
    if (-not $folder) {
        # Older checkouts may have moved an imported folder without changing its GUID.
        if ($null -eq $metadataByGuid) {
            $metadataByGuid = @{}
            foreach ($metadata in Get-ChildItem -LiteralPath $sourceRoot -Recurse -Filter '*.meta' -File) {
                $sourceGuid = Get-Content -LiteralPath $metadata.FullName |
                    Where-Object { $_ -match '^guid:' } | Select-Object -First 1
                if ($sourceGuid) {
                    $metadataByGuid[$sourceGuid] = @($metadataByGuid[$sourceGuid]) + $metadata.FullName
                }
            }
        }
        $matchesByGuid = @($metadataByGuid['guid: ' + $asset.guid] | Where-Object { $_ })
        if ($matchesByGuid.Count -gt 1) { throw "Multiple source assets have GUID $($asset.guid)." }
        if ($matchesByGuid.Count -eq 1) {
            $candidate = $matchesByGuid[0].Substring(0, $matchesByGuid[0].Length - '.meta'.Length)
            if (Test-Path -LiteralPath $candidate -PathType $sourceType) { $folder = $candidate }
        }
    }
    if (-not $folder) { throw "Missing source asset and .meta for $($asset.name)." }
    $guidLine = Get-Content -LiteralPath ($folder + '.meta') | Where-Object { $_ -match '^guid:' }
    if ($guidLine -ne ('guid: ' + $asset.guid)) {
        throw "Asset GUID differs from the project manifest: $folder"
    }
    $destination = [IO.Path]::GetFullPath((Join-Path $targetRoot $newRelative))
    if (-not $destination.StartsWith($targetRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Destination escapes the third-party asset directory: $destination"
    }
    [pscustomobject]@{
        Asset = $asset
        Source = $folder
        Destination = $destination
        GuidLine = $guidLine
        IsFile = $isFile
    }
}

# Resolve and validate every source before copying any assets.
foreach ($entry in $restoreEntries) {
    $asset = $entry.Asset
    $folder = $entry.Source
    $destination = $entry.Destination
    $guidLine = $entry.GuidLine
    if (Test-Path -LiteralPath $destination) {
        $existingGuid = Get-Content -LiteralPath ($destination + '.meta') | Where-Object { $_ -match '^guid:' }
        if ($existingGuid -ne $guidLine) { throw "Existing destination has a different GUID: $destination" }
        Write-Host "Already restored: $($asset.name)"
        continue
    }
    if ($PSCmdlet.ShouldProcess($destination, 'Restore local asset with original metadata')) {
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
        if ($entry.IsFile) {
            Copy-Item -LiteralPath $folder -Destination $destination
        }
        else {
            # Nested manifest entries are restored separately, including their metadata.
            $nested = @($restoreEntries | Where-Object {
                $_.Source.StartsWith($folder + '\', [StringComparison]::OrdinalIgnoreCase)
            })
            $copyArguments = @($folder, $destination, '/E', '/COPY:DAT', '/DCOPY:DAT', '/R:1', '/W:1', '/XJ', '/NFL', '/NDL', '/NP', '/NJH', '/NJS')
            $nestedDirectories = @($nested | Where-Object { -not $_.IsFile })
            if ($nestedDirectories.Count -gt 0) { $copyArguments += @('/XD') + @($nestedDirectories.Source) }
            if ($nested.Count -gt 0) {
                $copyArguments += @('/XF') + @($nested | ForEach-Object { $_.Source + '.meta' })
                $copyArguments += @($nested | Where-Object { $_.IsFile } | ForEach-Object { $_.Source })
            }
            & robocopy @copyArguments
            if ($LASTEXITCODE -ge 8) { throw "Asset restore failed: $folder" }
        }
        Copy-Item -LiteralPath ($folder + '.meta') -Destination ($destination + '.meta')
    }
}

# Imported editor/demo scripts sometimes contain paths to the original folders.
if (-not $WhatIfPreference -and (Test-Path -LiteralPath $targetRoot)) {
    foreach ($script in Get-ChildItem -LiteralPath $targetRoot -Recurse -Filter '*.cs' -File) {
        $content = [IO.File]::ReadAllText($script.FullName)
        $updated = $content
        foreach ($entry in ($restoreEntries | Sort-Object { $_.Source.Length } -Descending)) {
            $relativeSource = $entry.Source.Substring($sourceRoot.Length).TrimStart('\', '/').Replace('\', '/')
            $updated = $updated.Replace('Assets/' + $relativeSource + '/', $entry.Asset.path + '/')
        }
        foreach ($asset in ($plan.assets | Sort-Object { $_.oldPath.Length } -Descending)) {
            $updated = $updated.Replace($asset.oldPath + '/', $asset.path + '/')
        }
        if ($updated -ne $content) { [IO.File]::WriteAllText($script.FullName, $updated) }
    }
}
Write-Host 'Third-party assets are local only; Git ignores Assets/ThirdParty/.'
