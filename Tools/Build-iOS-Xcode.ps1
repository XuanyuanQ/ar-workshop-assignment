[CmdletBinding()]
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.com',
    [string]$ProjectPath = (Resolve-Path -LiteralPath "$PSScriptRoot\..").Path,
    [string]$OutputPath = (Join-Path (Resolve-Path -LiteralPath "$PSScriptRoot\..").Path 'Builds\iOS-Xcode')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity.exe was not found at: $UnityPath"
}

$runningUnity = Get-Process Unity -ErrorAction SilentlyContinue
if ($runningUnity) {
    throw "Unity is already running. Save your work, close all Unity windows, then run this script again."
}

$projectLock = Join-Path $ProjectPath 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $projectLock) {
    throw "Unity appears to have this project open. Save your work, close Unity, then run this script again."
}

$logPath = Join-Path $ProjectPath 'Logs\ios-xcode-export.log'
$unityArgs = @(
    '-batchmode',
    '-quit',
    '-projectPath', $ProjectPath,
    '-executeMethod', 'IOSBuildSetup.BuildIOSXcode',
    '-outputPath', $OutputPath,
    '-logFile', $logPath
)

& $UnityPath @unityArgs
$exitCode = if ($null -eq $LASTEXITCODE) { 1 } else { $LASTEXITCODE }

if ($exitCode -ne 0) {
    throw "Unity iOS export failed with exit code $exitCode. See Logs\ios-xcode-export.log."
}

$projectFile = Join-Path $OutputPath 'Unity-iPhone.xcodeproj\project.pbxproj'
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "Export finished, but Unity-iPhone.xcodeproj was not found at: $OutputPath"
}

Write-Host "iOS Xcode export ready: $OutputPath"
