[CmdletBinding()]
param(
    [ValidateSet('Validate', 'Windows', 'Android', 'EditModeTests', 'PlayModeTests')]
    [string]$Target = 'Validate',

    [string]$UnityPath,

    [string]$Output,

    [switch]$Development,

    [switch]$ConnectProfiler,

    [switch]$AutoRun
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectVersionFile = Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt'

if (-not (Test-Path -LiteralPath $projectVersionFile)) {
    throw "ProjectVersion.txt was not found under '$projectRoot'."
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $UnityPath = $env:UNITY_EDITOR_PATH
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $versionLine = Get-Content -LiteralPath $projectVersionFile |
        Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } |
        Select-Object -First 1

    if (-not $versionLine -or $versionLine -notmatch '^m_EditorVersion:\s*(.+)$') {
        throw "Could not determine the Unity version from '$projectVersionFile'."
    }

    $unityVersion = $Matches[1].Trim()
    $UnityPath = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$unityVersion\Editor\Unity.exe"
}

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor was not found at '$UnityPath'. Pass -UnityPath or set UNITY_EDITOR_PATH."
}

$automationRoot = Join-Path $projectRoot 'Logs\Codex'
$resultRoot = Join-Path $projectRoot 'TestResults'
New-Item -ItemType Directory -Path $automationRoot -Force | Out-Null
New-Item -ItemType Directory -Path $resultRoot -Force | Out-Null

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logPath = Join-Path $automationRoot "$Target-$timestamp.log"
$unityArguments = @(
    '-batchmode'
    '-nographics'
    '-projectPath', $projectRoot
    '-logFile', $logPath
)

switch ($Target) {
    'Validate' {
        # Opening the project in batch mode imports assets and fails on compiler errors.
    }
    'Windows' {
        $unityArguments += @('-buildTarget', 'Win64', '-executeMethod', 'Project.Editor.BuildCommand.BuildWindows64')
    }
    'Android' {
        $unityArguments += @('-buildTarget', 'Android', '-executeMethod', 'Project.Editor.BuildCommand.BuildAndroid')
    }
    'EditModeTests' {
        $resultPath = Join-Path $resultRoot "EditMode-$timestamp.xml"
        $unityArguments += @('-runTests', '-testPlatform', 'EditMode', '-testResults', $resultPath)
    }
    'PlayModeTests' {
        $resultPath = Join-Path $resultRoot "PlayMode-$timestamp.xml"
        $unityArguments += @('-runTests', '-testPlatform', 'PlayMode', '-testResults', $resultPath)
    }
}

if (-not [string]::IsNullOrWhiteSpace($Output)) {
    $resolvedOutput = if ([System.IO.Path]::IsPathRooted($Output)) {
        $Output
    }
    else {
        Join-Path $projectRoot $Output
    }
    $unityArguments += @('-buildOutput', $resolvedOutput)
}

if ($Development) {
    $unityArguments += '-developmentBuild'
}

if ($ConnectProfiler) {
    $unityArguments += '-connectProfiler'
}

if ($AutoRun) {
    $unityArguments += '-autoRunPlayer'
}

$unityArguments += '-quit'

Write-Host "Unity:   $UnityPath"
Write-Host "Project: $projectRoot"
Write-Host "Target:  $Target"
Write-Host "Log:     $logPath"

$quotedArguments = $unityArguments | ForEach-Object {
    if ($_ -match '[\s"]') {
        '"' + ($_ -replace '"', '\"') + '"'
    }
    else {
        $_
    }
}

$unityProcess = Start-Process `
    -FilePath $UnityPath `
    -ArgumentList ($quotedArguments -join ' ') `
    -NoNewWindow `
    -Wait `
    -PassThru
$unityExitCode = $unityProcess.ExitCode

if ($unityExitCode -ne 0) {
    throw "Unity exited with code $unityExitCode. See '$logPath'."
}

Write-Host "Completed successfully. Log: $logPath"
if ($resultPath) {
    Write-Host "Results: $resultPath"
}
